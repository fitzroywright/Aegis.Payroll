using Aegis.Payroll.Application;
using Common.Security.Abstractions;
using Common.Security.Authorization;
using Microsoft.Extensions.Configuration;
namespace Aegis.Payroll.Infrastructure;

// Explicit allow-list. AD authentication and directory administration do not grant payroll access.
public sealed class ShellAuthorizationStore(IConfiguration configuration) : IAuthorizationStore
{
    private static readonly IReadOnlyList<RoleDefinition> Roles = [
        new("ShellViewer", "Shell viewer", [ShellCatalog.ViewShell]),
        new("ShellEngineer", "Shell engineer", [ShellCatalog.ViewShell, ShellCatalog.ViewDiagnostics])];
    public Task<IReadOnlyCollection<string>> GetDirectRoleKeysAsync(string subjectId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<string>>(configuration.GetSection("Security:UserRoles").GetChildren()
            .Where(x => string.Equals(x.Key, subjectId, StringComparison.OrdinalIgnoreCase))
            .SelectMany(x => x.Get<string[]>() ?? []).ToArray());
    public Task<IReadOnlyCollection<string>> GetRoleKeysForGroupsAsync(IReadOnlyCollection<string> groups, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<string>>(configuration.GetSection("Security:GroupRoles").GetChildren()
            .Where(x => groups.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
            .SelectMany(x => x.Get<string[]>() ?? []).ToArray());
    public Task<IReadOnlyCollection<RoleDefinition>> GetRolesAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<RoleDefinition>>(Roles.Where(x => keys.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToArray());
}
public static class ShellStartupPolicy
{
    public static void ValidateAuthentication(bool development, string mode, string? mockPassword)
    {
        if (mode != "ActiveDirectory" && mode != "Mock") throw new InvalidOperationException("Unknown authentication mode.");
        if (mode == "Mock" && (!development || string.IsNullOrWhiteSpace(mockPassword)))
            throw new InvalidOperationException("Mock authentication requires Development and an explicit secret password.");
    }
}
