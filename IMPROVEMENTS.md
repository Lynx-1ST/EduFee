# Review and improvements — 2026-09-18

> Update 2026-09-19: SQLite is now the runtime source of truth under `%LocalAppData%\EduFee`; payment and receipt writes use one transaction; schema constraints, verified backup/restore, and direct receipt/debt PDF export are implemented. See `KE_HOACH_PHAT_TRIEN.md` for current status. Recommendations below are retained as the original review record and may now be completed.

The project already has useful student, tuition, receipt, import, reporting and email workflows, with shared styling and recent UI improvements. Reliability and data integrity are the priority over another visual redesign.

## Implemented

| Area | Change |
| --- | --- |
| JSON persistence | Complete-file replacement and previous-version backups for the five services; preserve inner exceptions. |
| Corrupt data | Unreadable receipts no longer become an empty history; empty/null student, semester and fee files are rejected; startup closes with an accurate error. |
| Import | Quoted and multiline CSV fields, explicit headers, invalid row feedback, no fabricated contact details or birthdays, one save per batch, duplicate detection and memory rollback on failure. |
| Export | Formula-like cell values are neutralized while retaining UTF-8 BOM output. |
| Tuition status | A due date includes the entire calendar day. Tuition lists and statistics refresh statuses using semester due dates when needed. |
| Amounts | Original tuition is calculated from net tuition plus discount, respecting custom rates. Small remaining balances can be entered in the payment dialog. |
| Payment errors | Failed fee/receipt saves roll back their in-memory changes. The dialog prevents repeat submission after the payment is saved and reports email failure separately. |
| Credentials | Windows DPAPI encryption on email-settings saves, with legacy plaintext read compatibility. |
| Migration | Failed SQL synchronization now throws so the UI cannot report it as a success. Connection strings use the SQLite builder. |
| Batch fees | Exact class matching prevents selecting K1 from also billing K10. |
| Printing | Dispose temporary receipt/debt-notice bitmaps after each print page. |
| Verification | Separate regression runner; isolated, asserted application self-test. Added setup documentation and a data/build-output ignore file. |

## Remaining recommendations, in priority order

1. **Make SQLite the authoritative store with a versioned migration.** Fee payments and receipts are still saved in separate JSON files. A failure between those saves can leave a payment without a receipt; the dialog now reports that condition and blocks repeat submission, but this is not a cross-file transaction. Use one database transaction for both, exact monetary storage, foreign keys, uniqueness constraints, and reconciliation tests. Preserve existing data during migration.
2. **Unify recovery and data location.** Move runtime data to an explicit per-user directory with migration from current working-directory files. The SQL-to-JSON restore screen still replaces multiple files sequentially, and its database backup is a raw file copy; use a transactional restore and SQLite's backup API. Concurrent application instances can still overwrite one another's edits.
3. **Enforce business rules in services.** Guard deletion of students/semesters/fees with financial history; validate semester ranges and unique names; avoid exposing mutable model references to forms. Other edit/delete operations still need memory rollback when saves fail.
4. **Finish email and bank configuration.** Configure and verify the institution's bank details instead of hard-coded values; HTML-encode user text in email templates; make QR loading failures explicit. The current offline QR placeholder is decorative, and bank transfers are not automatically reconciled despite wording in the debt email.
5. **Broaden UI and integration validation.** Resolve existing nullable warnings, check resizing/DPI, navigation, printing, real SMTP and SQLite recovery. The automated regression suite does not exercise those interactions.

## Verification results

- Regression runner: 26 checks passed using isolated synthetic data, including file-lock failures and DPAPI round trips.
- Final build and isolated application self-test results are reported in the task response.
- Existing nullable warnings remain; the initial incremental build did not expose them until recompilation.

No user data was migrated and no real emails were sent during this work. Source files were backed up before editing at `C:\Users\Lynx\AppData\Local\Temp\EduFee-source-before-af574f738e1b423f9646174ad414cdf7` because the project has no Git repository. No Git repository or commits were created.
