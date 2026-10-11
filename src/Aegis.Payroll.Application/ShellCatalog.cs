namespace Aegis.Payroll.Application;

public sealed record ShellCapability(string Key, string Name, string State, string Detail);
public static class ShellCatalog
{
    public const string ViewShell = "Payroll.Shell.View";
    public const string ViewDiagnostics = "Payroll.Diagnostics.View";
    public static IReadOnlyList<ShellCapability> Capabilities { get; } = new[]
    {
        ("employees", "Employees"), ("payroll", "Payroll processing"), ("deductions", "Deductions"),
        ("statutory", "Statutory rules and reporting"), ("banking", "Banking and payments"),
        ("leave", "Leave management"), ("attendance", "Attendance evidence"),
        ("holidays", "Jamaica holidays"), ("self-service", "Employee and manager self-service"),
        ("cafeteria", "Cafeteria deductions"), ("sensor-network", "SensorNetwork"),
        ("ffp-manager", "FFP Manager"), ("legacy", "VPAY import"),
        ("accounting", "Business Central export"), ("audit", "Audit trail")
    }.Select(x => new ShellCapability(x.Item1, x.Item2, "NotImplemented", "Requires reviewed implementation and acceptance.")).ToArray();
}
public sealed record ShellDependency(string Name, string State, DateTimeOffset? VerifiedAtUtc = null);
public sealed record ShellStatus(string Application, string Stage, bool PayrollReady, IReadOnlyList<ShellCapability> Capabilities,
    IReadOnlyList<ShellDependency> Dependencies);
