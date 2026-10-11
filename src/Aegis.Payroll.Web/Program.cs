var builder = WebApplication.CreateBuilder(args);
string api = builder.Configuration["PayrollApi:BaseUrl"] ?? "http://127.0.0.1:5088";
if (!Uri.TryCreate(api, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && !uri.IsLoopback))
    throw new InvalidOperationException("PayrollApi:BaseUrl requires HTTPS except loopback development.");
builder.Services.AddReverseProxy().LoadFromMemory(
    new[] {
        new Yarp.ReverseProxy.Configuration.RouteConfig { RouteId = "api", ClusterId = "payroll", Match = new() { Path = "/api/{**rest}" } },
        new Yarp.ReverseProxy.Configuration.RouteConfig { RouteId = "auth", ClusterId = "payroll", Match = new() { Path = "/auth/{**rest}" } }
    },
    new[] { new Yarp.ReverseProxy.Configuration.ClusterConfig { ClusterId = "payroll", Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig> {
        ["api"] = new() { Address = api.TrimEnd('/') + "/" } } } });
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapReverseProxy();
app.Run();
