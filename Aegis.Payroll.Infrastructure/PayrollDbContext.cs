using Aegis.Payroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Payroll.Infrastructure;

public sealed class PayrollDbContext(DbContextOptions<PayrollDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<EarningCode> EarningCodes => Set<EarningCode>();
    public DbSet<DeductionCode> DeductionCodes => Set<DeductionCode>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().HasIndex(x => x.EmployeeNumber).IsUnique();
        modelBuilder.Entity<PayrollPeriod>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<EarningCode>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<DeductionCode>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<LeaveType>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Holiday>().HasIndex(x => new { x.Date, x.Name }).IsUnique();
    }
}
