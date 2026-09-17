namespace Aegis.Payroll.Domain.Payroll;

public enum PayrollRunMode
{
    Simulation = 0,
    Production = 1
}

public enum PayrollRunStatus
{
    Draft = 0,
    Calculating = 1,
    Calculated = 2,
    Reviewed = 3,
    Finalized = 4,
    Failed = 5,
    Discarded = 6
}

public sealed class PayrollRun
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PayrollPeriodId { get; init; }
    public PayrollRunMode Mode { get; init; }
    public PayrollRunStatus Status { get; private set; } = PayrollRunStatus.Draft;
    public string? ScenarioName { get; init; }
    public string? RuleSetVersion { get; init; }
    public Guid? BasedOnRunId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public bool CanProduceExternalEffects => Mode == PayrollRunMode.Production && Status == PayrollRunStatus.Finalized;

    public void MarkCalculating() => Status = PayrollRunStatus.Calculating;

    public void MarkCalculated()
    {
        Status = PayrollRunStatus.Calculated;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkReviewed() => Status = PayrollRunStatus.Reviewed;

    public void FinalizeRun()
    {
        if (Mode == PayrollRunMode.Simulation)
            throw new InvalidOperationException("Simulation payroll runs cannot be finalized for payment, statutory submission, or accounting export.");

        if (Status != PayrollRunStatus.Reviewed)
            throw new InvalidOperationException("Only reviewed production payroll runs can be finalized.");

        Status = PayrollRunStatus.Finalized;
        CompletedAtUtc ??= DateTimeOffset.UtcNow;
    }

    public void Discard()
    {
        if (Mode != PayrollRunMode.Simulation)
            throw new InvalidOperationException("Only simulation payroll runs can be discarded.");

        Status = PayrollRunStatus.Discarded;
    }
}

public sealed record PayrollSimulationScenario(
    string Name,
    Guid PayrollPeriodId,
    string? RuleSetVersion,
    Guid? BasedOnRunId,
    IReadOnlyDictionary<string, string> Overrides);
