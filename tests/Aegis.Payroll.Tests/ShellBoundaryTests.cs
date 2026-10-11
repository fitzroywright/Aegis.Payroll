using Aegis.Payroll.Application;
using Aegis.Payroll.Domain;
using Aegis.Payroll.Infrastructure;
using Common.Security.Authorization;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Aegis.Payroll.Tests;
public sealed class ShellBoundaryTests
{
    [Fact]
    public async Task CalculationCannotProduceAnUnreviewedPayroll()
    {
        await Assert.ThrowsAsync<PayrollCapabilityUnavailableException>(() => new UnavailablePayrollCalculator()
            .CalculateAsync(new PayrollCalculationRequest(Guid.NewGuid())));
    }
    [Theory]
    [InlineData(false, "Mock", "secret")]
    [InlineData(true, "Mock", "")]
    [InlineData(true, "Unknown", "secret")]
    public void InvalidAuthenticationConfigurationFailsClosed(bool development, string mode, string password)
        => Assert.Throws<InvalidOperationException>(() => ShellStartupPolicy.ValidateAuthentication(development, mode, password));
    [Fact]
    public async Task UnmappedDirectoryAdministratorDoesNotGetPayrollPermission()
    {
        var configuration = new ConfigurationBuilder().Build();
        var authorizer = new PermissionAuthorizer(new ShellAuthorizationStore(configuration));
        Assert.False(await authorizer.CanAsync(AuthorizationSubject.Create("admin", ["Domain Admins"]), ShellCatalog.ViewShell));
    }
    [Fact]
    public async Task ViewerCannotInspectDiagnostics()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Security:GroupRoles:Payroll Readers:0"] = "ShellViewer" }).Build();
        var authorizer = new PermissionAuthorizer(new ShellAuthorizationStore(config));
        var subject = AuthorizationSubject.Create("reader", ["Payroll Readers"]);
        Assert.True(await authorizer.CanAsync(subject, ShellCatalog.ViewShell));
        Assert.False(await authorizer.CanAsync(subject, ShellCatalog.ViewDiagnostics));
    }
    [Fact]
    public void RegistrationContractCannotExposeConfigurationValues()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
            { ["payroll/database"] = "sensitive connection", ["payroll/mock-password"] = "sensitive password" }).Build();
        string json = PayrollRegistrationWorker.BuildContract(config, "test").ToJsonString();
        Assert.DoesNotContain("sensitive", json);
        Assert.Contains("supportsRemoteDiagnostics", json);
    }
}
