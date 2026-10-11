namespace Aegis.Payroll.Domain;

// Contracts only: amounts, statutory rules and state transitions require human review.
public sealed record PayrollCalculationRequest(Guid PeriodId);
public sealed record PayrollCalculationResult(Guid PeriodId, string AuditReference);
public interface IPayrollCalculator
{
    Task<PayrollCalculationResult> CalculateAsync(PayrollCalculationRequest request, CancellationToken cancellationToken = default);
}
public sealed class PayrollCapabilityUnavailableException(string capability)
    : InvalidOperationException($"{capability} is unavailable until reviewed implementation and acceptance.")
{
}

public sealed record AttendanceEvidence(string Provider, string ExternalId, string EmployeeReference, DateTimeOffset OccurredAt);
public sealed record HolidayEvidence(string Provider, string ExternalId, DateOnly Date, string Name, string SourceReference);
public sealed record DeductionEvidence(string Provider, string BatchId, string EmployeeReference, decimal Amount, string Currency);
public sealed record AccountingExportRequest(Guid PeriodId, string IdempotencyKey);
public sealed record AccountingExportReceipt(string ExternalReference, bool Acknowledged);
public sealed record LegacyImportRequest(string SourceReference, string IdempotencyKey);
public interface IAttendanceProvider { Task<IReadOnlyList<AttendanceEvidence>> ReadAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default); }
public interface IHolidayProvider { Task<IReadOnlyList<HolidayEvidence>> ReadAsync(int year, CancellationToken cancellationToken = default); }
public interface IDeductionProvider { Task<IReadOnlyList<DeductionEvidence>> ReadAsync(string batchId, CancellationToken cancellationToken = default); }
public interface IAccountingIntegration { Task<AccountingExportReceipt> ExportAsync(AccountingExportRequest request, CancellationToken cancellationToken = default); }
public interface ILegacyImport { Task StageAsync(LegacyImportRequest request, CancellationToken cancellationToken = default); }
