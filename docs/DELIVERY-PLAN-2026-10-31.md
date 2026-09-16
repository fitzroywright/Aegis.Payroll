# Aegis.Payroll delivery plan — 31 October 2026

## Definition of done
A payroll can be prepared from employee/master data, approved leave, attendance and external deductions; calculated using verified effective-dated Jamaican statutory rules; reviewed and approved; closed without silent mutation; produce payslips/statutory outputs and accounting output; expose employee/manager self-service; and retain/migrate required VPAY history.

## Phase 1 — Foundation and truth model (Sep 16–20)
- Solution/project skeleton and Common dependencies.
- Employee, employment, pay period, earning, deduction, statutory ledger, leave ledger and attendance evidence models.
- PostgreSQL persistence and migrations.
- Registration/configuration contract and diagnostics.
- Security roles/permissions baseline.
- Statutory rule source register.

Exit: application starts, registers truthfully, persists core model, and calculation tests can run independently of UI.

## Phase 2 — Jamaican calculation engine (Sep 21–30)
- PAYE, NIS, NHT and Education Tax effective-dated rules.
- Pension/superannuation and configurable earnings/deductions.
- Calculation explanation/audit trace.
- Period workflow: Draft -> Calculated -> Reviewed -> Approved -> Closed.
- Known-answer tests against official rules and sanitized VPAY 2026 cases.

Exit: representative employees reconcile to independently verified expected statutory results.

## Phase 3 — VPAY migration + statutory outputs (Oct 1–8)
- DBF reader/staging importer.
- Employee/code/department/cost-centre mappings.
- Historical payroll/statutory import.
- Import provenance, idempotency and reconciliation.
- SO1/SO2/C7 generation and monthly/YTD reconciliation.

Exit: sanitized VPAY import can be repeated without duplicates and reconciles to source totals.

## Phase 4 — Leave, attendance, holidays, cafeteria (Oct 9–15)
- Leave ledger, entitlement/policy, request/approval workflow.
- Employee and manager self-service flows.
- IAttendanceProvider with SensorNetwork and FFP Manager adapters.
- IHolidayProvider with Jamaican government baseline and operator override/audit.
- IDeductionProvider with Cafeteria deduction batch integration.

Exit: approved upstream events flow into payroll without direct database coupling or automatic punitive deductions from raw attendance.

## Phase 5 — Dynamics + banking/accounting boundary (Oct 16–21)
- IAccountingIntegration contract.
- Dynamics adapter selected/configured for the organization's actual Dynamics product/API.
- Payroll journal mapping by earning/deduction/cost centre/department.
- Export/post preview, reconciliation, idempotency and retry semantics.
- No payroll close is represented as successfully posted unless Dynamics acknowledges the transaction.

Exit: closed test payroll produces a balanced journal and completes a test Dynamics round trip or an agreed production-equivalent acceptance path.

## Phase 6 — Web/PWA + Avalonia desktop (Oct 22–26)
- Payroll operations screens.
- Employee self-service: payslips, YTD, leave, attendance visibility and requests.
- Manager approvals and exception review.
- Tablet-responsive PWA.
- Avalonia Windows/Linux operational client using the same application/API services.

Exit: critical payroll workflows work from Web and desktop without duplicated calculation logic.

## Phase 7 — Parallel payroll, hardening and cutover (Oct 27–31)
- Parallel run against VPAY.
- Gross-to-net employee-level variance report.
- Statutory and GL/Dynamics reconciliation.
- Permission/audit review.
- Failure-mode tests: Configuration down, Diagnostics down, secret provider unavailable, Dynamics unavailable, duplicate imports, interrupted close/post.
- Backup/restore and deployment runbook.
- Production readiness assessment.

Exit: no unexplained material variances; recovery and rollback tested; acceptance recorded.

## Scope guard
Anything not required to calculate, review, approve, report, migrate, integrate, self-serve or safely operate the October payroll is post-1.0 unless it removes a release-blocking risk.

## Dynamics clarification required before Phase 5
Confirm the target: Dynamics 365 Finance, Dynamics 365 Business Central, Dynamics GP, or another Dynamics product; identify the company/legal entity, target journal type, dimensions/cost-centre mapping and available test environment/API credentials. These are integration configuration, not payroll-domain dependencies.
