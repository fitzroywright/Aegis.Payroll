using Aegis.Payroll.Api;
using Common.Diagnostics;

namespace Aegis.Payroll.Tests;

public sealed class PayrollLifecycleTelemetryTests
{
    [Fact]
    public void CreateEvent_UsesMetadataOnlyIdentifiers()
    {
        LifecycleEvent evt = PayrollLifecycleTelemetry.CreateEvent(
            "payroll-01",
            "PayrollCrud",
            "Employee.Updated",
            LifecycleEventOutcome.Succeeded,
            "corr-1",
            "11111111-2222-3333-4444-555555555555");

        Assert.Equal("Aegis.Payroll", evt.ApplicationId);
        Assert.Equal("PayrollCrud", evt.Flow);
        Assert.Equal("Employee.Updated", evt.Stage);
        Assert.Equal("corr-1", evt.CorrelationId);
        Assert.Equal("11111111-2222-3333-4444-555555555555", evt.RelatedBusinessId);
        Assert.Null(evt.Detail);
        Assert.Null(evt.Properties);
    }

    [Fact]
    public void CreateEvent_RejectsSensitiveCodeNamesThroughSharedContract()
    {
        Assert.Throws<ArgumentException>(() => LifecycleEvent.Create(
            "Aegis.Payroll",
            "payroll-01",
            "PayrollCrud",
            "Employee.Updated",
            LifecycleEventOutcome.Failed,
            "corr-2",
            properties: new Dictionary<string, string>
            {
                ["EmployeePassword"] = "must-not-leak"
            }));
    }
}
