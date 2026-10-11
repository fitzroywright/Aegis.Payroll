using Microsoft.EntityFrameworkCore;
namespace Aegis.Payroll.Infrastructure;

// Schema scaffolding only. No automatic schema creation or domain write endpoints.
public sealed class PayrollDbContext(DbContextOptions<PayrollDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeRecord> Employees => Set<EmployeeRecord>();
    public DbSet<PayPeriodRecord> PayPeriods => Set<PayPeriodRecord>();
    public DbSet<SourceRecord> Sources => Set<SourceRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeRecord>().HasIndex(x => x.ExternalReference).IsUnique();
        modelBuilder.Entity<EmployeeRecord>().Property(x => x.ExternalReference).HasMaxLength(100);
        modelBuilder.Entity<SourceRecord>().HasIndex(x => new { x.Provider, x.ExternalReference }).IsUnique();
    }
}
public sealed class EmployeeRecord
{
    public Guid Id { get; set; }
    public string ExternalReference { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
public sealed class PayPeriodRecord
{
    public Guid Id { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
}
public sealed class SourceRecord
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = "";
    public string ExternalReference { get; set; } = "";
    public string SourceHash { get; set; } = "";
    public DateTimeOffset ObservedAtUtc { get; set; }
}
