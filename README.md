# Aegis.Payroll

Aegis.Payroll is the Aegis payroll, leave, attendance and employee self-service platform.

## October 31, 2026 delivery scope

- Jamaican payroll calculation engine with effective-dated statutory rules
- PAYE, NIS, NHT and Education Tax calculation and audit explanation
- Payroll periods, earnings, deductions, adjustments, approvals, close and immutable history
- Leave management with employee/manager self-service
- Attendance ingestion through provider contracts (Sensor Network and FFP Manager)
- Jamaica public holiday calendar with government baseline plus operator overrides
- Cafeteria payroll-deduction integration
- SO1, SO2 and C7 statutory reporting/reconciliation
- VPAY legacy import with staging, validation, provenance, idempotency and reconciliation
- Dynamics 365 Business Central integration behind a provider-neutral accounting adapter
- Responsive Web/PWA interface for desktop, Linux and tablets
- Avalonia desktop client for Windows and Linux
- Common.Security, Common.Secrets, Common.Messaging, Common.Diagnostics, Common.Storage and Common.Registration used for cross-cutting concerns

## Architectural rules

1. Payroll calculation logic lives in the domain/application layers, never in a UI.
2. Closed payroll periods are immutable; corrections are explicit adjustments/reversals.
3. Statutory rules are effective-dated and source-attributed. Historical payroll is never recalculated with current rules.
4. Attendance is evidence. HR/authorized approval establishes payroll consequence.
5. Aegis.Configuration describes and monitors required configuration; it does not own configuration values or secrets.
6. Applications access secret providers only through Common.Secrets.
7. Applications register exactly once through Common.Registration with Aegis.Configuration.
8. Business Central, attendance, holiday, deduction and legacy systems are integrations behind contracts; the payroll domain does not depend on provider-specific SDKs.
9. Government holiday data is a baseline. Authorized operators can add, override, deactivate or supersede calendar entries without losing provenance.
10. Legacy imports must reconcile before commit and must be safe to repeat without duplicates.

## Business Central integration

The initial accounting target is **Microsoft Dynamics 365 Business Central**.

- Authentication: Microsoft Entra OAuth 2.0 service-to-service/client credentials for unattended posting.
- API: Business Central REST API v2.0 where standard endpoints are sufficient.
- Initial posting target: payroll general journal batches and journal lines.
- Payroll maps internal posting accounts/cost centres/dimensions through an accounting adapter; domain code never calls Business Central directly.
- Every export gets a stable external reference, batch identity, request/result audit record and reconciliation status so retries cannot create duplicate payroll journals.
- Business Central credentials are obtained only through Common.Secrets.
- Company/environment IDs, journal selection and mappings are declared/monitored through the application's configuration contract; Aegis.Configuration does not store secrets.
- If standard Business Central APIs cannot represent a required payroll field or dimension, use a narrowly scoped custom AL API rather than database access.

## Solution shape

- `Aegis.Payroll.Domain` - payroll/leave/attendance domain and calculation contracts
- `Aegis.Payroll.Application` - use cases, workflows and integration orchestration
- `Aegis.Payroll.Infrastructure` - persistence and provider adapters, including Business Central
- `Aegis.Payroll.Api` - shared service/API surface
- `Aegis.Payroll.Web` - responsive Web/PWA and self-service
- `Aegis.Payroll.Desktop` - Avalonia payroll operations client
- `Aegis.Payroll.LegacyImport` - VPAY DBF staging/migration tools
- `Aegis.Payroll.Tests` - statutory, regression, migration and reconciliation tests

Target: production-capable first release by **2026-10-31**.
