using Aegis.Payroll.Domain.Entities;
using Aegis.Payroll.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PayrollDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Payroll") ?? "Data Source=aegis-payroll.db"));
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<PayrollDbContext>().Database.EnsureCreatedAsync();

MapCrud<Employee>(app, "employees", db => db.Employees, ValidateEmployee);
MapCrud<PayrollPeriod>(app, "payroll-periods", db => db.PayrollPeriods, ValidatePayrollPeriod);
MapCrud<EarningCode>(app, "earning-codes", db => db.EarningCodes, x => ValidateCode(x.Code, x.Name));
MapCrud<DeductionCode>(app, "deduction-codes", db => db.DeductionCodes, x => ValidateCode(x.Code, x.Name));
MapCrud<LeaveType>(app, "leave-types", db => db.LeaveTypes, x => ValidateCode(x.Code, x.Name));
MapCrud<Holiday>(app, "holidays", db => db.Holidays, x => string.IsNullOrWhiteSpace(x.Name) ? "Holiday name is required." : null);

app.Run();

static void MapCrud<T>(WebApplication app, string route, Func<PayrollDbContext, DbSet<T>> set, Func<T, string?> validate) where T : Entity
{
    var group = app.MapGroup($"/api/{route}");
    group.MapGet("/", async (PayrollDbContext db, CancellationToken ct) => await set(db).AsNoTracking().ToListAsync(ct));
    group.MapGet("/{id:guid}", async (Guid id, PayrollDbContext db, CancellationToken ct) =>
        await set(db).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) is { } item ? Results.Ok(item) : Results.NotFound());
    group.MapPost("/", async (T item, PayrollDbContext db, CancellationToken ct) =>
    {
        if (validate(item) is { } error) return Results.ValidationProblem(new Dictionary<string, string[]> { ["record"] = [error] });
        set(db).Add(item);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/{route}/{item.Id}", item);
    });
    group.MapPut("/{id:guid}", async (Guid id, T input, PayrollDbContext db, CancellationToken ct) =>
    {
        if (id != input.Id) return Results.BadRequest(new { error = "Route id and record id must match." });
        if (validate(input) is { } error) return Results.ValidationProblem(new Dictionary<string, string[]> { ["record"] = [error] });
        if (!await set(db).AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
        input.Touch();
        db.Entry(input).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
        return Results.Ok(input);
    });
    group.MapDelete("/{id:guid}", async (Guid id, PayrollDbContext db, CancellationToken ct) =>
    {
        var item = await set(db).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();
        set(db).Remove(item);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    });
}

static string? ValidateEmployee(Employee x)
{
    if (string.IsNullOrWhiteSpace(x.EmployeeNumber)) return "Employee number is required.";
    if (string.IsNullOrWhiteSpace(x.FirstName) || string.IsNullOrWhiteSpace(x.LastName)) return "Employee first and last names are required.";
    if (x.TerminationDate is { } termination && termination < x.EmploymentDate) return "Termination date cannot precede employment date.";
    return null;
}

static string? ValidatePayrollPeriod(PayrollPeriod x)
{
    if (string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name)) return "Payroll period code and name are required.";
    if (x.EndDate < x.StartDate) return "Payroll period end date cannot precede start date.";
    if (x.PayDate < x.StartDate) return "Pay date cannot precede the period start date.";
    return null;
}

static string? ValidateCode(string code, string name) =>
    string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) ? "Code and name are required." : null;

public partial class Program;
