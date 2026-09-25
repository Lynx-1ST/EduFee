# EduFee UI V2 — Incremental Implementation

This document adopts the approved **EduFee UI V2 — Redesign Plan** as the UI source of truth.
The redesign remains native .NET 10 WinForms and reuses the existing services, SQLite database,
payment workflows, receipt snapshots, PDF engine, and recovery logic.

## Guardrails

- Do not change schema v6 or financial/payment behavior as part of UI work.
- Do not falsify operational bank beneficiary data while removing decorative university branding.
- Keep every checkpoint buildable and regression-tested.
- Prefer existing controls and services; add only reusable UI controls with at least two consumers.
- Preserve keyboard access, visible focus, 1160×680 usability, and PerMonitorV2 scaling.

## Delivery order

### Checkpoint A — Foundation (P0)

- [x] A1. Remove decorative HUMG branding and introduce a vector EduFee ledger mark.
- [x] A2. Align semantic colors, typography, spacing, borders, icons, grids, and buttons with V2.
- [x] A3. Simplify the shell to a 220px sidebar with Overview, Students, Tuition, Reports, and Settings.
- [x] A4. Add an Overview vertical slice using current semester and tuition services.

Verification: Release build, full regression suite, self-test, and shell/layout captures.

### Checkpoint B — Core records (P0/P1)

- [x] B1. Redesign Students toolbar, count, empty state, and student-centric grid.
- [x] B2. Polish student detail as a profile/edit sheet without changing persistence.
- [x] B3. Redesign Tuition header, compact metrics, filters, status segments, and financial grid.

Verification: targeted UI acceptance at 900/1080 content widths plus full regression suite.

### Checkpoint C — Ledger flow (P1)

- [ ] C1. Add a read-only tuition detail surface with payment history timeline.
- [ ] C2. Redesign the payment dialog while preserving atomic receipt/payment handling.
- [ ] C3. Clarify MoMo polling and VietQR administrator confirmation states.

Verification: gateway UI/persistence/recovery acceptance plus full regression suite.

### Checkpoint D — Reports and administration (P1/P2)

- [ ] D1. Reframe Statistics as Reports with overview, debt, class summary, and export sections.
- [ ] D2. Polish semester management while preserving price-recalculation confirmations.
- [ ] D3. Add a Settings screen that delegates to existing Email, MoMo, Database, and Semester dialogs.
- [ ] D4. Rebrand receipt/debt document headers and email presentation without changing their data contracts.

Verification: PDF/report acceptance, recovery tests, Release build, and full regression suite.

### Checkpoint E — Quality and handoff

- [ ] E1. Audit keyboard order, access keys, focus cues, accessible names, and non-color status cues.
- [ ] E2. Verify 100%, 125%, and 150% DPI at minimum window size and standard desktop sizes.
- [ ] E3. Refresh screenshots and project documentation.
- [ ] E4. Run Release build, regression suite, self-test, and CI-equivalent checks.

## Quota-safe stopping rule

Stop only at a completed checkpoint. If available execution budget becomes constrained, finish the
current vertical slice, run its verification, update these checkboxes, and leave the next phase untouched.
