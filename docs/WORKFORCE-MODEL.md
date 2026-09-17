# Workforce and Engagement Model

Aegis.Payroll must not assume that every person is a standard employee. A person may participate in the organization under one or more effective-dated engagements, including employee, intern, volunteer and consultant arrangements.

## Core model

Use a stable Person record plus effective-dated Engagement records.

```text
Person
  └── Engagement
      ├── EngagementType
      ├── EffectiveFrom / EffectiveTo
      ├── Department / Position / CostCentre / Location
      ├── CompensationArrangement
      ├── StatutoryProfile
      ├── AttendancePolicy
      ├── LeavePolicy
      ├── PaymentInstruction
      └── AccountingProfile
```

A person's identity is distinct from their engagement. This allows someone to move from volunteer to intern to employee without losing history or creating duplicate people.

## Engagement types

### Employee
Typical salaried/hourly employee relationship.

Potential capabilities:
- Regular payroll
- PAYE/NIS/NHT/Education Tax according to applicable rules
- Leave entitlement
- Attendance integration
- Recurring earnings/deductions
- Banking/payment instructions
- Employer contributions
- Business Central posting

### Intern
Interns may be paid or unpaid and may have different attendance, leave, stipend and statutory treatment.

Required characteristics:
- Paid, stipend, allowance-only or unpaid modes
- Start/end dates are normally mandatory
- Optional school/programme/sponsor reference
- Dedicated eligibility/statutory rule profile
- Attendance may be required even when no payroll payment occurs
- Leave entitlement may differ from employees

### Volunteer
Volunteers are not assumed to be payroll-paid employees.

Required characteristics:
- Usually unpaid, but may receive reimbursements, stipends or allowances
- Attendance/time may still be tracked
- Reimbursements must remain distinct from taxable earnings unless a rule explicitly classifies them otherwise
- May have programme/project/cost-centre allocations
- Can participate in self-service and approval workflows without being included in ordinary employee payroll

### Consultant
Consultants are engagements rather than employees unless explicitly classified otherwise by authorized policy.

Required characteristics:
- Contract start/end dates
- Contract/reference number
- Fixed fee, milestone, day-rate, hour-rate or retainer compensation models
- Invoice/payment-cycle support where required
- Separate statutory/tax treatment rule profile
- Normally excluded from employee leave and employee recurring-deduction behavior unless specifically enabled
- Accounting distributions may target professional-service/contract accounts instead of payroll salary accounts
- Payment may be routed through payroll payment infrastructure or a separate accounts-payable/provider flow according to policy

## Rule-driven behavior

Application code must not contain assumptions such as `if EmployeeType == Consultant` scattered throughout the system. Engagement behavior should be resolved through typed rules/policies:

- Payroll eligibility
- Statutory eligibility/treatment
- Leave eligibility
- Attendance requirements
- Compensation calculation
- Deduction eligibility
- Approval workflow
- Payment method
- Accounting mapping
- Self-service capabilities

Example:

```text
Rule: INTERN-STIPEND-ELIGIBILITY
When Engagement.Type = Intern
And Compensation.Mode = Stipend
And Engagement is active for payroll period
Then include stipend pay component
```

## Payroll inclusion

A payroll run should operate over eligible engagements, not simply `Employees.Where(IsActive)`.

A person may therefore exist in the system but have:
- no payroll-eligible engagement,
- one payroll-eligible engagement,
- multiple concurrent engagements where policy permits.

The engine must prevent accidental duplicate pay where multiple engagements overlap unless that outcome is explicitly allowed.

## Historical audit

Finalized payroll snapshots must capture the engagement type and effective engagement context used for that run so later changes do not rewrite history.

Historical audit must support filtering/grouping by engagement type, including:
- Employees
- Interns
- Volunteers
- Consultants

Examples:
- Total consultant payments by department and month
- Intern stipends by programme
- Volunteer reimbursements by project
- Employee payroll totals excluding non-employees

## Migration

Legacy VPAY employee records should import through a staging/mapping layer. The importer must not automatically assume every historical record represents a standard Employee engagement. Mapping rules and operator review should identify non-standard engagements where evidence exists.

## Design principle

`Person` answers **who is this?**

`Engagement` answers **in what capacity are they participating?**

`PayrollEligibilityRule` answers **should this engagement produce payroll transactions for this period?**
