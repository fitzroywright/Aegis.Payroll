using Aegis.Payroll.Application;
using Aegis.Payroll.Domain;
using Common.Diagnostics;
using Common.Messaging;
using Common.Messaging.Hosting;
using Common.Messaging.Channels.MicrosoftGraph;
using Common.Secrets;
using Common.Security.Abstractions;
using Common.Security.Authenticators;
using Common.Security.Integration;
using Common.Security.Options;
using Common.Security.Plugin.ActiveDirectory;
using Common.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Aegis.Payroll.Infrastructure;

public static class CommonFoundation
{
    public static async Task<CommonSecretsBootstrapRuntime> AddPayrollFoundationAsync(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() && !string.Equals(configuration["CommonSecrets:Mode"], "Production", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Non-development hosts require Common.Secrets Production policy.");
        services.AddCommonDiagnostics();
        services.AddCommonSecretsDiagnostics();
        services.AddCommonSecurity();
        services.AddCommonSecurityDiagnostics();
        services.AddCommonAuthorization<ShellAuthorizationStore>();
        services.AddSingleton<IPayrollCalculator, UnavailablePayrollCalculator>();
        // One provider graph for bootstrap and runtime. The API owns its lifetime through shutdown.
        var bootstrap = CommonSecretsBootstrapRuntime.Create(configuration);
        bootstrap.Register(services);
        try
        {
            string mode = configuration["Security:Mode"] ?? "ActiveDirectory";
            string? password = mode == "Mock"
                ? await bootstrap.Provider.GetAsync(configuration["Security:Mock:PasswordSecretName"] ?? "payroll/mock-password", cancellationToken)
                : null;
            ShellStartupPolicy.ValidateAuthentication(environment.IsDevelopment(), mode, password);
            if (mode == "Mock")
            {
                services.AddSingleton<IAuthenticator>(_ => new MockAuthenticator(new MockAuthenticationOptions
                {
                    Password = password, AllowedUsers = configuration["Security:Mock:AllowedUsers"], AllowAnyUser = false
                }));
            }
            else
            {
                new ActiveDirectoryAuthenticationPlugin().Register(services, configuration.GetSection("Security:ActiveDirectory"));
            }
            // Keep the shared authentication event sink in the path without replacing the inner authenticator recursively.
            services.AddScoped(provider => new PlatformAuthenticator(provider.GetRequiredService<IAuthenticator>(),
                provider.GetRequiredService<ISecurityEventSink>()));

            string? database = await bootstrap.Provider.GetAsync(
                configuration["Database:ConnectionSecretName"] ?? "payroll/database", cancellationToken);
            services.AddSingleton(new DatabaseAvailability(!string.IsNullOrWhiteSpace(database)));
            if (!string.IsNullOrWhiteSpace(database)) services.AddDbContext<PayrollDbContext>(options => options.UseNpgsql(database));

            foreach (string key in new[] { "Storage:SharePoint:ClientSecret", "Storage:AzureBlob:ConnectionString",
                "Storage:S3:AccessKeyId", "Storage:S3:SecretAccessKey", "Storage:AmazonS3:AccessKeyId", "Storage:AmazonS3:SecretAccessKey" })
                if (!string.IsNullOrWhiteSpace(configuration[key])) throw new InvalidOperationException("Storage credentials require Common.Secrets references.");
            async Task<string?> StorageSecretAsync(string key) => string.IsNullOrWhiteSpace(configuration[key])
                ? null : await bootstrap.Provider.GetRequiredAsync(configuration[key]!, cancellationToken);
            StorageTargetOptions storage = StorageTargetOptions.FromConfiguration(configuration,
                sharePointClientSecret: await StorageSecretAsync("Storage:CredentialSecretName"),
                s3AccessKeyId: await StorageSecretAsync("Storage:AccessKeySecretName"),
                s3SecretAccessKey: await StorageSecretAsync("Storage:SecretKeySecretName"));
            if (storage.Kind is StorageTargetKind.LocalFolder or StorageTargetKind.NetworkFolder)
            {
                if (string.IsNullOrWhiteSpace(storage.RootPath)) throw new InvalidOperationException("Storage:RootPath is required.");
                storage = storage with { RootPath = Path.GetFullPath(storage.RootPath, environment.ContentRootPath) };
            }
            services.AddCommonStorage(storage);
            services.AddCommonMessagingSecrets();
            services.AddCommonMessagingQueuedDelivery(new ExternalDeliveryOptions
            {
                DurableQueuePath = Path.GetFullPath(configuration["Messaging:QueuePath"] ?? "data/messaging", environment.ContentRootPath)
            });
            services.AddCommonMessagingDiagnostics();
            if (configuration.GetValue<bool>("Messaging:Microsoft365:Enabled"))
            {
                var options = configuration.GetSection("Messaging:Microsoft365").Get<MicrosoftGraphEmailOptions>() ?? new();
                if (!string.IsNullOrEmpty(options.ClientSecret)) throw new InvalidOperationException("Use a Common.Secrets name for Microsoft 365 credentials.");
                services.AddMicrosoftGraphEmailMessagingChannel(options);
            }
            services.AddSingleton<RegistrationSnapshot>();
            services.AddHostedService<PayrollRegistrationWorker>();
            services.AddScoped<ShellStatusService>();
            services.AddScoped<IDiagnosticCheck, ShellReadinessCheck>();
            return bootstrap;
        }
        catch
        {
            await bootstrap.DisposeAsync();
            throw;
        }
    }
}
public sealed record DatabaseAvailability(bool Configured);
