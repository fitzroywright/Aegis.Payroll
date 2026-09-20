using System.Net.Http.Headers;
using Common.Diagnostics;
using Common.Registration;

namespace Aegis.Payroll.Api;

public sealed class PayrollLifecycleTelemetry(
    IHttpClientFactory clients,
    IConfiguration configuration,
    ILogger<PayrollLifecycleTelemetry> logger)
{
    public const string ApplicationId = "Aegis.Payroll";

    public string InstanceId =>
        configuration["Aegis:Registration:InstanceId"]?.Trim()
        ?? Environment.MachineName;

    public string IdentityFile =>
        configuration["Aegis:Registration:IdentityFile"]?.Trim()
        ?? (OperatingSystem.IsWindows()
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Aegis",
                "Payroll",
                "registration-identity.json")
            : "/var/lib/aegis/payroll/registration-identity.json");

    public static LifecycleEvent CreateEvent(
        string instanceId,
        string flow,
        string stage,
        LifecycleEventOutcome outcome,
        string correlationId,
        string? businessId = null,
        string? code = null) =>
        LifecycleEvent.Create(
            ApplicationId,
            instanceId,
            flow,
            stage,
            outcome,
            correlationId,
            relatedBusinessId: businessId,
            code: code);

    public async Task EmitAsync(
        string flow,
        string stage,
        LifecycleEventOutcome outcome,
        string correlationId,
        string? businessId = null,
        string? code = null,
        CancellationToken cancellationToken = default)
    {
        string? operationsUrl = configuration["Aegis:Operations:Url"]?.Trim();
        if (string.IsNullOrWhiteSpace(operationsUrl)) return;

        try
        {
            var identityStore = new FileRegistrationIdentityStore(IdentityFile);
            RegistrationIdentityDocument identity =
                await identityStore.LoadOrCreateAsync(ApplicationId, InstanceId, cancellationToken);
            if (string.IsNullOrWhiteSpace(identity.Credential)) return;

            HttpClient client = clients.CreateClient("operations-lifecycle");
            var sink = new HttpLifecycleEventSink(
                client,
                new HttpLifecycleEventSinkOptions(
                    new Uri(operationsUrl.TrimEnd('/') + "/api/operations/lifecycle/events"),
                    TimeSpan.FromSeconds(5)),
                (request, lifecycleEvent, _) =>
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", identity.Credential);
                    request.Headers.TryAddWithoutValidation("X-Aegis-Application-Id", identity.ApplicationId);
                    request.Headers.TryAddWithoutValidation("X-Aegis-Instance-Id", identity.InstanceId);
                    request.Headers.TryAddWithoutValidation("X-Aegis-Installation-Id", identity.InstallationId);
                    return Task.CompletedTask;
                });

            await new LifecycleTelemetry(sink).EmitAsync(
                CreateEvent(
                    InstanceId,
                    flow,
                    stage,
                    outcome,
                    correlationId,
                    businessId,
                    code),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Payroll lifecycle telemetry publication failed; payroll operation remains committed.");
        }
    }
}
