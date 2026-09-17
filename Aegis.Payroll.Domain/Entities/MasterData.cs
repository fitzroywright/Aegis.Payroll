namespace Aegis.Payroll.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

public sealed class Employee : Entity
{
    public required string EmployeeNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Trn { get; set; }
    public string? NisNumber { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public DateOnly EmploymentDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum PayrollPeriodStatus { Draft, Open, Calculated, Reviewed, Finalized }

public sealed class PayrollPeriod : Entity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly PayDate { get; set; }
    public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Draft;
}

public sealed class EarningCode : Entity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsPensionable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class DeductionCode : Entity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsStatutory { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class LeaveType : Entity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool RequiresApproval { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public enum HolidaySource { GovernmentBaseline, Operator }

public sealed class Holiday : Entity
{
    public DateOnly Date { get; set; }
    public required string Name { get; set; }
    public HolidaySource Source { get; set; }
    public string? SourceReference { get; set; }
    public bool IsActive { get; set; } = true;
}
