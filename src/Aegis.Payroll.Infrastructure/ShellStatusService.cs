using Aegis.Payroll.Application;
using Common.Diagnostics;
using Common.Messaging;
using Common.Secrets;
using Common.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Aegis.Payroll.Infrastructure;

public sealed class ShellStatusService(IServiceProvider services, DatabaseAvailability database, RegistrationSnapshot registration)
{
    public async Task<ShellStatus> ReadAsync(CancellationToken ct = default)
    {
        var dependencies = new List<ShellDependency>();
        var now = DateTimeOffset.UtcNow;
        var secretHealth = await services.GetRequiredService<ICommonSecretsHealthCheck>().CheckAsync(ct);
        dependencies.Add(new("Common.Secrets", secretHealth.IsAvailable ? "Verified" : "Unavailable", now));
        var storageHealth = await services.GetRequiredService<IFileStorage>().CheckHealthAsync(ct);
        dependencies.Add(new("Common.Storage", storageHealth.Available && storageHealth.Writable ? "Verified" : "Unavailable", now));
        var queueHealth = await services.GetRequiredService<IExternalDeliveryQueueHealth>().CheckHealthAsync(ct);
        dependencies.Add(new("Common.Messaging queue", queueHealth.IsAvailable ? "Verified" : "Unavailable", now));
        dependencies.Add(new("Notification delivery", "NotImplemented"));
        string dbState = "NotConfigured";
        if (database.Configured)
        {
            try
            {
                var db = services.GetRequiredService<PayrollDbContext>();
                // Check actual shell tables, not merely whether a database accepts TCP connections.
                _ = await db.Employees.AsNoTracking().Take(1).Select(x => x.Id).ToListAsync(ct);
                _ = await db.PayPeriods.AsNoTracking().Take(1).Select(x => x.Id).ToListAsync(ct);
                _ = await db.Sources.AsNoTracking().Take(1).Select(x => x.Id).ToListAsync(ct);
                dbState = "Verified";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch { dbState = "UnavailableOrSchemaMissing"; }
        }
        dependencies.Add(new("PostgreSQL schema", dbState, database.Configured ? now : null));
        var observed = registration.Read();
        dependencies.Add(new("Common.Registration", observed.State, observed.At));
        dependencies.Add(new("Configuration contract", observed.ContractState, observed.At));
        dependencies.Add(new("Active Directory", "NotVerified"));
        dependencies.AddRange(new[] { "Business Central", "Cafeteria", "SensorNetwork", "FFP Manager" }
            .Select(x => new ShellDependency(x, "NotImplemented")));
        return new("Aegis.Payroll", "Foundation shell", false, ShellCatalog.Capabilities, dependencies);
    }
}
public sealed class ShellReadinessCheck : IDiagnosticCheck
{
    public string Name => "Payroll implementation readiness";
    public Task<DiagnosticResult> RunAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new DiagnosticResult(Name, DiagnosticStatus.Warning,
            "Foundation shell only. Calculation, finalization and exports are unavailable.", TimeSpan.Zero));
}
