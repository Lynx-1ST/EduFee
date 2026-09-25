# EduFee — Student Tuition Management System

[English](README.md) | [Tiếng Việt](README_VI.md)

![CI](https://github.com/Lynx-1ST/EduFee/actions/workflows/ci.yml/badge.svg)

EduFee is a .NET 10 Windows Forms desktop application for managing student records, semesters, tuition fees, outstanding balances, payments, and financial receipts. Runtime data is stored locally in SQLite at `%LocalAppData%\EduFee\edufee.db`.

---

## Features

### 1. Student Management

- Create, edit, and delete student records with student code, full name, date of birth, class name, email, and phone number.
- Search by student code or full name and filter by class name.
- Import and export student data using UTF-8 CSV files.
- Keep `StudentCode` separate from the internal SQLite primary key.

### 2. Semester and Tuition Management

- Manage semesters with start date, end date, tuition due date, and active-semester state.
- Create tuition fees based on credit count using the current system tuition rate.
- Apply tuition discounts and scholarship policies.
- Automatically calculate payment status: *Unpaid*, *Partially Paid*, *Paid*, *Overdue*, and *Late Paid*.
- Create tuition records in batch for students sharing the same class name.

### 3. Payments and Receipts

- Record partial or full tuition payments.
- Update tuition balances and create receipts within a single atomic SQLite transaction.
- Reject overpayments and prevent direct modification of receipt-backed paid amounts.
- Store immutable receipt snapshots so historical documents remain stable after student or tuition data changes.
- Support VietQR bank-transfer QR codes and optional MoMo Sandbox payments through a shared gateway interface.

### 4. Reports and Statistics

- Display semester-level totals for tuition, collected amount, outstanding debt, and completion rate.
- View debt details by student and aggregate statistics by class name.
- Export student/debt data to CSV.
- Generate tuition receipts and multi-page debt reports as PDF using the built-in GDI+ rendering pipeline.
- Preview and print receipts through Windows printing APIs.

### 5. Data Safety and Administration

- Store operational data in SQLite with foreign keys and financial constraints.
- Manage schema upgrades through `PRAGMA user_version` with sequential migrations through schema v5, including persisted gateway transactions.
- Keep payment updates and receipt creation ACID-compliant.
- Validate database integrity and schema compatibility during restore operations.
- Create safety backups before database replacement.
- Protect SMTP and MoMo Sandbox secrets using Windows DPAPI.
- Restrict the desktop application to a single running instance.

---

## Technology Stack

- **Language / Platform:** C# 14, .NET 10, Windows Forms
- **Database:** SQLite with `Microsoft.Data.Sqlite 10.0`
- **UI / Graphics:** WinForms, GDI+, Per-Monitor V2 High DPI
- **QR Generation:** `QRCoder 1.8.0`
- **CI:** GitHub Actions on `windows-latest`

---

## Requirements

- Windows 10 or Windows 11 (64-bit)
- .NET 10 SDK for development
- .NET Desktop Runtime 10 for framework-dependent published builds

---

## Build and Run

From the repository root:

```powershell
dotnet build 26K1_DotNet.slnx
dotnet run --project 26K1_DotNet
```

---

## Demo Mode

Run EduFee with an isolated demo database:

```powershell
dotnet run --project 26K1_DotNet -- --demo
```

Demo mode uses temporary, isolated data and does not modify the main runtime database. The current fixture contains 3 students, 1 semester, 3 tuition records, and 1 payment receipt.

---

## MoMo Sandbox

This integration is for **test payments only**. It uses MoMo's `captureWallet` create API and transaction query API on `https://test-payment.momo.vn`; it cannot switch to Production.

- Configure your own Sandbox Partner Code, Access Key and Secret Key in the MoMo settings dialog. The secret stays masked and is saved with Windows DPAPI for the current Windows user. Do not copy credentials into source code or CI secrets.
- A new order is persisted before the network request. Creating or scanning a QR code does not record tuition. Only a matching, successful query response can update tuition, create a receipt and mark the gateway transaction complete in one SQLite transaction.
- Failed, cancelled and expired results are persisted with their provider result code and completion time for audit. Pending and unknown results remain recoverable; none of these non-success states changes tuition or creates a receipt.
- On startup, enabled MoMo Sandbox accounts automatically recheck durable pending/unknown orders. A confirmed success creates its receipt atomically; provider failures are closed; network errors stay recoverable. The same recovery can be run manually from **Settings → Recheck pending MoMo transactions**.
- The payment dialog queries every four seconds. Closing it stops polling; it does not cancel a remote MoMo payment. Network failures do not mark a payment failed.
- `RedirectUrl` and `IpnUrl` default to `https://localhost/` for the query-only desktop flow. No callback listener is provided; the local return page will not load. Supply URLs you control if needed for your Sandbox account. A redirect or IPN is never treated as proof of payment.
- Demo mode always uses the internal mock. Automated tests use fake HTTP responses and require no MoMo credentials or Internet access to the provider.

The implementation is verified offline. An end-to-end Sandbox payment still requires your valid Sandbox credentials and MoMo test wallet. Production payments, public webhooks, refunds and recurring payments are outside this integration.

Provider references: [wallet create API](https://developers.momo.vn/v3/vi/docs/payment/api/wallet/onetime/) and [result codes](https://developers.momo.vn/v3/docs/payment/api/result-handling/resultcode/). See the [integration plan](26K1_DotNet/Docs/MOMO_SANDBOX_INTEGRATION_PLAN.md).

## Testing

MoMo Sandbox tests use a custom HTTP handler; CI never calls MoMo and requires no payment credentials. Demo mode always selects the internal mock gateway.

Run the application self-test:

```powershell
dotnet run --project 26K1_DotNet -- --test
```

Run the regression and acceptance suite:

```powershell
dotnet run --project 26K1_DotNet.RegressionTests
```

The regression suite covers areas including:

- SQLite persistence and constraints
- schema migration and rollback/retry behavior
- StudentCode migration and uniqueness
- financial transaction atomicity
- receipt snapshots
- CSV import/export
- backup/restore validation
- PDF/report generation
- VietQR transfer flows
- MoMo signatures, response validation, duplicate-payment protection and schema v5 rollback/retry
- startup and selected UI behaviors

GitHub Actions also runs Release build, regression tests, and the application self-test for pushes and pull requests targeting `main`.

---

## Release Publish

Create a framework-dependent Release build:

```powershell
dotnet publish 26K1_DotNet -c Release -o output/publish --self-contained false
```

The target machine must have the compatible .NET Desktop Runtime installed.

---

## Data and Configuration

### SQLite Database

Default runtime path:

```text
%LocalAppData%\EduFee\edufee.db
```

### Legacy JSON Migration

Legacy JSON data can be imported through the database configuration UI.

The command-line migration mode:

```powershell
dotnet run --project 26K1_DotNet -- --migrate
```

reads the legacy JSON files from the current working directory.

Expected files:

```text
students.json
semesters.json
tuitionfees.json
receipts.json
```

### SMTP

SMTP settings are configured from the application settings screen. Stored passwords are protected with Windows DPAPI.

---

## Architecture

```text
WinForms UI
    ↓
Service Layer
    ↓
SqliteRepository
    ↓
SQLite
```

Payment flow:

```text
FormPayment
    ↓
TuitionService
    ↓
SqliteRepository
    ↓
BEGIN TRANSACTION
    ├── UPDATE TuitionFees
    └── INSERT PaymentReceipts
    ↓
COMMIT
```

Any failure during the transaction results in a rollback.

---

## Project Documentation

- [Development Roadmap](KE_HOACH_PHAT_TRIEN.md)
- [Technical Improvements and Design Notes](IMPROVEMENTS.md)
- [UI/UX Design Specification](UI_Plan.md)
- [Vietnamese README](README_VI.md)

---

## Scope Notes

The current project intentionally remains a local Windows desktop application. The core scope does not include:

- a standalone class-management module
- multi-role authentication
- Web API or web frontend
- mobile application
- cloud synchronization
- production banking/payment-gateway integration or real-money acceptance
- microservices
- AI features
- WPF/WinUI rewrite
