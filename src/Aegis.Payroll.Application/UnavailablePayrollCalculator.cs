using Aegis.Payroll.Domain;
namespace Aegis.Payroll.Application;

public sealed class UnavailablePayrollCalculator : IPayrollCalculator
{
    public Task<PayrollCalculationResult> CalculateAsync(PayrollCalculationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new PayrollCapabilityUnavailableException("Payroll calculation");
    }
}
