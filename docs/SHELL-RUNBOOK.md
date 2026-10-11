# Payroll foundation shell

Target repository: `fitzroywright/Aegis.Payroll`. The shell contains no statutory formulas, gross-to-net calculations, payroll transitions, close, bank export, accounting posting or legacy import implementation.

## Projects

| Project | Shell responsibility |
| --- | --- |
| Domain | Provider-neutral calculation, attendance, holiday, deduction, accounting and import contracts |
| Application | Capability catalog and unavailable calculator boundary |
| Infrastructure | Common library composition, AD/mock authentication, explicit role allow-list, EF Core schema scaffold, status checks and registration worker |
| Api | Shared runtime, cookie sign-in, antiforgery, rate limit, protected status/diagnostics and health |
| Web | Responsive shell, sign-in and dependency status through a same-origin API proxy; PWA manifest |
| Desktop | Avalonia Windows/Linux shell and shared capability catalog; operational API/authentication still pending |
| LegacyImport | Explicit unavailable command, exit code 2; does not read or write VPAY data |
| Tests | Calculation boundary, fail-closed authentication, permission isolation, metadata secrecy and HTTP boundaries |

The API is the sole registration owner for Aegis.Payroll. Browser and desktop clients do not create duplicate application registrations. Web is not an independently registered service in this shell.

## Local setup

Use .NET 10 SDK, Git and PowerShell 7. These commands are run locally; no cloud restore/build/test/deployment is required. From the repository root:

```powershell
pwsh ./scripts/Initialize-Common.ps1 -Pinned
dotnet restore Aegis.Payroll.slnx
dotnet build Aegis.Payroll.slnx --no-restore
dotnet test tests/Aegis.Payroll.Tests/Aegis.Payroll.Tests.csproj --no-build
```

Default layout:

```text
Projects/Aegis.Payroll
Projects/Common/Common.Diagnostics
Projects/Common/Common.Registration
Projects/Common/Common.Secrets
Projects/Common/Common.Security
Projects/Common/Common.Storage
Projects/Common/Common.Messaging
```

The setup script preserves existing checkouts, warns about baseline differences and never resets local changes. `common-baselines.json` records the Common main commits whose APIs were inspected. For a different layout pass `-CommonRoot` to the script and `-p:CommonRoot=/absolute/path/Common` to each dotnet command. Build all projects before using `--no-build` for tests. Shared library restore/build failures must be resolved locally before this shell is considered validated.

### Development sign-in

The default uses AD and LDAPS port 636 against JMAD01/JMAD02. Install/trust the issuing directory certificate chain locally. Authentication does not imply authorization. `Security:UserRoles` maps the exact trimmed lowercase login identifier; `Security:GroupRoles` maps directory group names. Supported shell roles are `ShellViewer` and `ShellEngineer`. These roles grant no payroll calculation, approval or payment permissions.

For an explicit development-only mock:

```powershell
dotnet user-secrets set "Security:Mode" "Mock" --project src/Aegis.Payroll.Api
dotnet user-secrets set "payroll/mock-password" "<choose-a-local-test-password>" --project src/Aegis.Payroll.Api
dotnet user-secrets set "Security:UserRoles:payroll-dev:0" "ShellEngineer" --project src/Aegis.Payroll.Api
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Aegis.Payroll.Api --no-build --urls http://127.0.0.1:5088
```

In a second terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Aegis.Payroll.Web --no-build --urls http://127.0.0.1:5089
```

Open `http://127.0.0.1:5089`, sign in as `payroll-dev`, and inspect status. An unset mock password fails startup. Mock mode fails startup outside Development. On macOS/bash use `ASPNETCORE_ENVIRONMENT=Development dotnet run ...` in place of the PowerShell environment assignment.

Desktop preview:

```powershell
dotnet run --project src/Aegis.Payroll.Desktop --no-build
```

This is a capability shell, not visual acceptance parity with the approved dashboard. Desktop operational workflows and authentication remain unavailable. The browser manifest does not add offline payroll support; authenticated content is not cached by a service worker.

## Configuration and secrets

All provider credentials are resolved through Common.Secrets. Production uses its production policy: enabled HTTPS OpenBao. Supply the OpenBao address and external AppRole bootstrap material through deployment configuration; do not commit them. Production fails closed with the intentionally incomplete default OpenBao configuration. Development uses a named Configuration provider for local user-secrets. Secret values are not sent to Aegis.Configuration.

| Setting | Purpose |
| --- | --- |
| `Database:ConnectionSecretName` | Common.Secrets key for PostgreSQL connection string, default `payroll/database` |
| `Storage:Provider` | Common.Storage target: LocalFolder, NetworkFolder, SharePoint, AzureBlob, S3 or AmazonS3 |
| `Storage:Local:RootPath` | Isolated local Payroll document root, default `data/documents` relative to API content root |
| `Storage:NetworkFolder:RootPath` | Mounted network storage root |
| `Storage:CredentialSecretName` | SharePoint client secret or Azure Blob connection secret reference |
| `Storage:AccessKeySecretName`, `Storage:SecretKeySecretName` | S3 credential references; target metadata follows Common.Storage's configuration contract |
| `Messaging:QueuePath` | Durable Common.Messaging queue root, default `data/messaging` |
| `Messaging:Microsoft365:Enabled` | Explicitly register the shared Graph email provider |
| `Messaging:Microsoft365:TenantId`, `ClientId`, `SenderUpn` | Graph provider metadata |
| `Messaging:Microsoft365:ClientSecretName` | Common.Secrets key for Graph credential |
| `Aegis:Configuration:Url` | HTTPS configuration registration endpoint; loopback HTTP allowed for development |
| `Aegis:PublicUrl` | Published API health URL |
| `Aegis:Registration:InstanceId` | Stable instance name, defaults to environment if blank |
| `Aegis:Registration:IdentityFile` | Protected Common.Registration identity document; keep across restarts |
| `PayrollApi:BaseUrl` (Web) | API proxy destination; default loopback 5088 |
| `AllowedHosts` | Configure separately on API/Web for the deployed hostnames |

Common.Messaging queue, credential resolver and optional Graph channel are wired, but the application recipient directory, persisted inbox and notification workflows are pending. No send endpoint is exposed. Queue availability is not proof of email delivery. Common.Storage is wired without exposing document upload/download endpoints; employee authorization and audit must be implemented first.

Database persistence is optional at shell startup. If a database secret is supplied, status verifies all three shell tables. There is no automatic database creation, migration, employee import or record mutation. Generate and review EF migrations locally before applying any schema, and keep the payroll database separate from legacy systems. A missing table is reported as unavailable/schema missing rather than healthy.

## HTTP contracts and validation

- `/live`: process liveness only, not payroll readiness.
- `/health`: suite Common.Diagnostics health contract, explicitly Degraded while implementation is pending.
- `/api/shell/capabilities`: public static catalog with no employee or financial data.
- `/auth/antiforgery`, `/auth/login`, `/auth/logout`: same-origin cookie authentication; login/logout require antiforgery.
- `/api/shell/status`: assigned shell role required; dependency observations and PayrollReady=false.
- `/api/shell/diagnostics`: ShellEngineer required; returns check names/states/durations without provider exceptions or paths.

Payroll transaction routes do not exist. Registration success, a working queue and storage checks never make payroll ready. AD remains NotVerified in the dashboard until a dedicated probe is implemented; successful sign-in is not a periodic health probe. Remote DiagnosticLevel execution and Operations telemetry are explicitly advertised unsupported in this shell; local Common.Diagnostics runner remains available to authorized engineers.

Local acceptance: restore/build/test, start both hosts, confirm unauthenticated status is 401, authenticate a mapped user, reject an unmapped user, inspect dependency failures, verify missing schema is not shown green, confirm mock fails outside Development, and exercise registration pending/approval/restart against the test Configuration service. Verify Graph/AD/OpenBao/storage using test credentials before enabling real integrations. No automatic deployment is included.

## Review boundary

Payroll calculation contracts and provider evidence are scaffolding, not accepted domain schemas or rules. Human implementation/review is required for effective-dated statutory rules, immutable close, attendance consequences, approval separation, financial reconciliation, audit persistence, employee privacy, VPAY migration and Business Central export. Existing charter and delivery-plan milestones are historical planning targets; this shell does not certify those phases complete.
