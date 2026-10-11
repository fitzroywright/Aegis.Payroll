using System.Security.Claims;
using System.Threading.RateLimiting;
using Aegis.Payroll.Application;
using Aegis.Payroll.Infrastructure;
using Common.Diagnostics;
using Common.Security.Abstractions;
using Common.Security.Authorization;
using Common.Security.Integration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
await using var secrets = await builder.Services.AddPayrollFoundationAsync(builder.Configuration, builder.Environment);
builder.Services.AddAuthentication("PayrollCookie").AddCookie("PayrollCookie", options =>
{
    options.Cookie.Name = "Aegis.Payroll.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("login", limiter => { limiter.PermitLimit = 10; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
});
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    try { await next(context); }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
    catch (Exception ex)
    {
        app.Logger.LogError("Payroll request failed: {ErrorType}", ex.GetType().Name);
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new { error = "Dependency unavailable. Consult server diagnostics." });
        }
    }
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/live", () => Results.Ok(new { application = "Aegis.Payroll", processRunning = true }));
app.MapAegisHealth("Aegis.Payroll", (_, _) => Task.FromResult(
    AegisHealthAssessment.Degraded("Foundation shell only; payroll is not ready.")));
app.MapGet("/api/shell/capabilities", () => Results.Ok(ShellCatalog.Capabilities));
app.MapGet("/auth/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
    Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
app.MapPost("/auth/login", async Task<IResult> (HttpContext context, LoginRequest request, IAntiforgery antiforgery,
    PlatformAuthenticator authentication, IPermissionAuthorizer permissions, CancellationToken ct) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(new { error = "Invalid request token." }); }
    if (string.IsNullOrWhiteSpace(request.UserName) || request.UserName.Length > 200 || string.IsNullOrEmpty(request.Password) || request.Password.Length > 1024)
        return Results.BadRequest();
    var result = authentication.Authenticate(request.UserName, request.Password);
    if (!result.IsAuthenticated) return Results.Unauthorized();
    string subjectId = request.UserName.Trim().ToLowerInvariant();
    var subject = AuthorizationSubject.Create(subjectId, result.GroupNames);
    if (!await permissions.CanAsync(subject, ShellCatalog.ViewShell, ct)) return Results.StatusCode(403);
    var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, subjectId), new(ClaimTypes.Name, result.UserInfo?.DisplayName ?? subjectId) };
    claims.AddRange(result.GroupNames.Select(x => new Claim("directory-group", x)));
    await context.SignInAsync("PayrollCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "PayrollCookie")));
    return Results.Ok(new { displayName = result.UserInfo?.DisplayName ?? subjectId });
}).RequireRateLimiting("login");
app.MapPost("/auth/logout", async Task<IResult> (HttpContext context, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    await context.SignOutAsync("PayrollCookie");
    return Results.NoContent();
}).RequireAuthorization();
app.MapGet("/api/shell/status", async Task<IResult> (HttpContext context, IPermissionAuthorizer permissions, ShellStatusService status, CancellationToken ct) =>
{
    if (!await CanAsync(context, permissions, ShellCatalog.ViewShell, ct)) return Results.StatusCode(403);
    return Results.Ok(await status.ReadAsync(ct));
}).RequireAuthorization();
app.MapGet("/api/shell/diagnostics", async Task<IResult> (HttpContext context, IPermissionAuthorizer permissions, IDiagnosticRunner runner, CancellationToken ct) =>
{
    if (!await CanAsync(context, permissions, ShellCatalog.ViewDiagnostics, ct)) return Results.StatusCode(403);
    var results = await runner.RunAsync(ct);
    // Deliberately omit exceptions, provider paths, hostnames and detailed diagnostic text.
    return Results.Ok(results.Select(x => new { x.Name, status = x.Status.ToString(), durationMs = x.Duration.TotalMilliseconds }));
}).RequireAuthorization();
app.Run();

static Task<bool> CanAsync(HttpContext context, IPermissionAuthorizer authorizer, string permission, CancellationToken ct)
    => authorizer.CanAsync(AuthorizationSubject.Create(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        context.User.FindAll("directory-group").Select(x => x.Value)), permission, ct);
public sealed record LoginRequest(string UserName, string Password);
public partial class Program { }
