using System.Text.Json.Nodes;
using Common.Diagnostics;
using Common.Registration;

namespace Aegis.Payroll.Api;

public sealed class PayrollRegistrationHostedService(
    IHttpClientFactory clients,
    IConfiguration configuration,
    PayrollLifecycleTelemetry lifecycle,
    ILogger<PayrollRegistrationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string? configurationUrl = configuration["Aegis:Configuration:Url"]?.Trim();
        if (string.IsNullOrWhiteSpace(configurationUrl))
        {
            logger.LogInformation("Payroll registration is disabled because Aegis:Configuration:Url is not configured.");
            return;
        }

        if (!Uri.TryCreate(configurationUrl.TrimEnd('/') + "/", UriKind.Absolute, out Uri? baseUri))
        {
            logger.LogWarning("Payroll registration is disabled because Aegis:Configuration:Url is invalid.");
            return;
        }

        var registration = new RegistrationLifecycleClient(
            clients.CreateClient("configuration-registration"),
            new RegistrationLifecycleOptions(
                baseUri,
                PayrollLifecycleTelemetry.ApplicationId,
                lifecycle.InstanceId,
                lifecycle.IdentityFile,
                TimeSpan.FromSeconds(10),
                RunbookReference: "docs/RUNBOOK.md"),
            logger: logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                string correlation = Guid.NewGuid().ToString("N");
                await lifecycle.EmitAsync(
                    "ApplicationLifecycle",
                    "RegistrationStep",
                    LifecycleEventOutcome.Started,
                    correlation,
                    cancellationToken: stoppingToken);

                RegistrationLifecycleStatus status = await registration.StepAsync(
                    new JsonObject
                    {
                        ["displayName"] = "Aegis Payroll",
                        ["environment"] = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production",
                        ["capabilities"] = new JsonArray("payroll-crud", "structured-lifecycle-telemetry")
                    },
                    stoppingToken);

                await lifecycle.EmitAsync(
                    "ApplicationLifecycle",
                    status.State.ToString(),
                    status.IsRegistered ? LifecycleEventOutcome.Succeeded : LifecycleEventOutcome.Warning,
                    status.CorrelationId ?? correlation,
                    businessId: status.RegistrationId,
                    cancellationToken: stoppingToken);

                TimeSpan delay = status.State switch
                {
                    RegistrationLifecycleState.Registered => TimeSpan.FromMinutes(5),
                    RegistrationLifecycleState.Pending or RegistrationLifecycleState.RecoveryPending => TimeSpan.FromSeconds(15),
                    _ => TimeSpan.FromMinutes(1)
                };
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Payroll registration lifecycle step failed.");
                try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            }
        }
    }
}
