using System.Text.Json.Nodes;
using Common.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Aegis.Payroll.Infrastructure;

public sealed record RegistrationObservation(string State, string ContractState, DateTimeOffset? At);
public sealed class RegistrationSnapshot
{
    private RegistrationObservation current = new("NotConfigured", "NotPublished", null);
    public RegistrationObservation Read() => Volatile.Read(ref current);
    public void Set(RegistrationObservation value) => Volatile.Write(ref current, value);
}
// Exactly one registration owner: the API. Web/Desktop are clients, not duplicate Payroll registrations.
public sealed class PayrollRegistrationWorker(IConfiguration configuration, IHostEnvironment environment,
    RegistrationSnapshot snapshot, ILogger<PayrollRegistrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string? url = configuration["Aegis:Configuration:Url"];
        if (string.IsNullOrWhiteSpace(url)) return;
        var uri = new Uri(url, UriKind.Absolute);
        string instance = string.IsNullOrWhiteSpace(configuration["Aegis:Registration:InstanceId"])
            ? environment.EnvironmentName : configuration["Aegis:Registration:InstanceId"]!.Trim();
        string path = Path.GetFullPath(configuration["Aegis:Registration:IdentityFile"] ?? "data/registration-identity.json", environment.ContentRootPath);
        using var http = new HttpClient();
        var client = new RegistrationLifecycleClient(http, new RegistrationLifecycleOptions(uri, "Aegis.Payroll", instance, path,
            RequestTimeout: TimeSpan.FromSeconds(10), RunbookReference: "docs/SHELL-RUNBOOK.md"));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var state = await client.StepAsync(new JsonObject { ["displayName"] = "Aegis Payroll", ["description"] = "Foundation shell; payroll processing unavailable." }, stoppingToken);
                string contract = "NotPublished";
                if (state.IsRegistered)
                {
                    var response = await client.PublishConfigurationContractAsync(BuildContract(configuration, instance), stoppingToken);
                    contract = (int)response is >= 200 and < 300 ? "Published" : "Rejected";
                }
                snapshot.Set(new(state.State.ToString(), contract, state.ObservedAtUtc));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                snapshot.Set(new("Unavailable", "NotPublished", DateTimeOffset.UtcNow));
                logger.LogWarning("Payroll registration failed: {ErrorType}", ex.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
    public static JsonObject BuildContract(IConfiguration configuration, string instance) => new()
    {
        ["applicationId"] = "Aegis.Payroll", ["displayName"] = "Aegis Payroll", ["instanceId"] = instance,
        ["version"] = "0.1.0", ["description"] = "Foundation shell. Payroll processing not ready.",
        ["runbookReference"] = "docs/SHELL-RUNBOOK.md",
        ["diagnostics"] = new JsonObject { ["baseUrl"] = configuration["Aegis:PublicUrl"], ["healthPath"] = "/health",
            ["supportsRemoteDiagnostics"] = false, ["supportsOperationalTelemetry"] = false },
        ["requirements"] = new JsonArray(
            Requirement("database", "Secret", "Database:ConnectionSecretName", true),
            Requirement("storage", "Configuration", "Storage:Provider", true),
            Requirement("directory", "Configuration", "Security:ActiveDirectory:Servers", true),
            Requirement("public-url", "Configuration", "Aegis:PublicUrl", true),
            Requirement("m365", "Secret", "Messaging:Microsoft365:ClientSecretName", false))
    };
    private static JsonObject Requirement(string id, string kind, string key, bool required) => new()
    {
        ["id"] = id, ["displayName"] = id, ["kind"] = kind, ["configurationKey"] = key, ["required"] = required,
        ["purpose"] = "Shell foundation dependency; declared metadata does not prove connectivity."
    };
}
