using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Aegis.Payroll.Tests;
public sealed class HttpBoundaryTests
{
    [Fact]
    public async Task ShellProtectsStatusAndNeverReportsPayrollReady()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?>
            {
                ["Security:Mode"] = "Mock", ["payroll/mock-password"] = "test-only-password",
                ["Storage:Local:RootPath"] = Path.Combine(Path.GetTempPath(), "payroll-shell-test-" + Guid.NewGuid()),
                ["Messaging:QueuePath"] = Path.Combine(Path.GetTempPath(), "payroll-shell-test-" + Guid.NewGuid()),
                ["Aegis:Configuration:Url"] = ""
            }));
        });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/shell/status")).StatusCode);
        string health = await client.GetStringAsync("/health");
        Assert.Contains("Degraded", health);
        Assert.Contains("not ready", health);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/payroll/close", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/auth/login", new { userName = "payroll-dev", password = "test-only-password" })).StatusCode);
    }
}
