# Aegis.Payroll Project Charter

## Project objective
Deliver a production-capable Jamaican payroll, leave, attendance, employee self-service and accounting-integration platform by **October 31, 2026**.

## UX acceptance targets
The project has two approved visual targets stored in the ChatGPT Library under `/Aegis Payroll/Mockups`:

1. `Aegis-Payroll-Architecture-and-Roadmap.png` — overall architecture, integrations and delivery roadmap.
2. `Aegis-Payroll-Desktop-UX-Target.png` — desktop/Avalonia operational dashboard and navigation target.

The desktop mockup is an acceptance target, not merely inspiration. The implemented Avalonia desktop client should converge on its information architecture and operational visibility while remaining truthful to real system state.

## Legacy source material
Persistent project source archives are stored under `/Aegis Payroll/Legacy Sources`:

- `VPAY7-Legacy-Application.zip` — legacy VPAY application, manual, DBF/CDX structures and supporting files.
- `VPAY-August-2026-Payroll-Data.zip` — August 2026 payroll snapshot and historical/current payroll data.

These archives are reference/migration sources, not authoritative legal sources for statutory rules.

## Hard requirements

- Jamaican payroll calculation engine with effective-dated statutory rules.
- PAYE, NIS, NHT and Education Tax support with explainable calculations.
- SO1, SO2 and C7 reporting/reconciliation.
- Payroll periods, earnings, deductions, adjustments, approvals, close and immutable history.
- Leave management.
- Employee and manager self-service.
- Attendance integration from Aegis.SensorNetwork and FFP Manager.
- Jamaican public holiday baseline plus authorized operator overrides.
- Cafeteria payroll deductions integration.
- VPAY import with staging, mapping, provenance, idempotency and reconciliation.
- Microsoft Dynamics 365 Business Central integration.
- Responsive Web/PWA for browsers, Linux and tablets.
- Avalonia desktop client for Windows and Linux.
- Common.Security, Common.Secrets, Common.Messaging, Common.Diagnostics, Common.Storage and Common.Registration used wherever appropriate.
- Aegis.Configuration describes and monitors required configuration but does not own configuration values or secrets.

## Core design rules

1. Payroll calculation logic must not live in UI projects.
2. Both Web/PWA and Desktop consume the same domain/application behavior.
3. Closed payroll periods are immutable; corrections use explicit adjustments/reversals.
4. Statutory rules are effective-dated and source-attributed.
5. Historical payroll is never recalculated using current statutory rules.
6. Attendance is evidence; authorized HR/payroll approval establishes payroll consequence.
7. Government holiday information is the baseline; the approved organizational calendar is operator-controlled and auditable.
8. External systems are behind provider contracts. Payroll must not directly depend on SensorNetwork internals, FFP Manager tables, Cafeteria tables, OpenBao, or Business Central SDK-specific domain logic.
9. Applications use Common.Secrets rather than provider-specific secret-store code.
10. Applications register once through Common.Registration with Aegis.Configuration.
11. Integration and health indicators must reflect verified state; never show green/healthy without evidence.
12. Business Central posting is separate from payroll finalization and must support safe retry/idempotency.
13. Legacy imports must reconcile before commit and be safe to repeat.
14. Dashboard totals should support drill-down to source records and exceptions.

## Desktop UX target
The desktop client should include, at minimum:

- Dashboard with employee count, payroll totals, leave approvals, Cafeteria deductions, statutory readiness and Business Central export state.
- Current payroll period workflow: Setup -> Attendance -> Deductions -> Calculate -> Review -> Finalize.
- Employees.
- Payroll Processing.
- Deductions.
- Statutory area.
- Reports.
- Banking & Payments.
- Export to Dynamics / Business Central.
- Leave Management.
- Attendance.
- Jamaican Holidays.
- Employee Portal / Manager Portal access.
- Cafeteria integration.
- SensorNetwork integration.
- FFP Manager integration.
- VPAY data import.
- Business Central integration.
- Configuration.
- Security & Roles.
- Audit Trail.
- System Health.
- Recent activities and upcoming payroll/statutory dates.

## Accounting target
Initial accounting target is **Microsoft Dynamics 365 Business Central** using supported REST APIs and Microsoft Entra OAuth service-to-service authentication. Standard APIs are preferred; a narrowly scoped custom AL API may be used only when standard APIs are insufficient.

## Delivery target
Production-capable first release: **October 31, 2026**.

Parallel-run acceptance should compare Aegis.Payroll results with VPAY using real/sanitized historical payroll inputs and independently validate statutory rules against current Jamaican government sources.
