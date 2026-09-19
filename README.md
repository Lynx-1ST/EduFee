# EduFee

Windows Forms student and tuition management application targeting .NET 10.

## Build and run

On Windows with the .NET 10 SDK:

```powershell
dotnet build 26K1_DotNet.slnx
dotnet run --project 26K1_DotNet
```

SQLite is the live source of truth. On first start, EduFee creates `%LocalAppData%\EduFee\edufee.db` and imports the legacy JSON files from the current working directory when the database is empty. The JSON files remain as migration inputs and are not written during normal operation.

Student, semester, tuition, payment and receipt changes are persisted directly to SQLite. A payment balance update and its receipt commit in one database transaction. The database screen can create a consistent SQLite backup, restore a verified EduFee backup, refresh the UI from SQL, or explicitly replace SQL data from legacy JSON after confirmation.

Receipt and debt-report screens export PDF files directly without requiring a configured printer. Receipt export uses A4 portrait; the multi-page debt report uses A4 landscape and repeats its table header.

## Verification

```powershell
dotnet run --project 26K1_DotNet.RegressionTests
dotnet run --project 26K1_DotNet -- --test
```

The regression runner is a console executable with no additional test packages. It returns a nonzero exit code on failure and uses a unique temporary directory. It covers persistence failures, CSV parsing/export, overdue calculations, password encryption, SQLite constraints and transactions, backup/restore, UI layout, and receipt/debt PDF generation. The application self-test also uses temporary data, simulation-only email and assertions.

## Data and credentials

Normal JSON saves write a temporary file, flush it, then replace the destination. The previous version is retained as `<filename>.bak`. If startup reports unreadable data, close the application and inspect the affected file and backup before restoring it. Never replace a corrupt file with an empty list unless discarding its contents is intentional.

SMTP passwords are encrypted using Windows DPAPI for the current Windows account when email settings are saved. Existing plaintext settings remain readable and upgrade on the next save. The previous `.bak` may still contain a legacy plaintext password; protect existing backups accordingly. Encrypted settings are not portable to another Windows account—re-enter the password there.

CSV import expects `MaSV,HoTen,Lop,NgaySinh,DienThoai,Email` (comma or semicolon separated), supports quoted/multiline fields, and reports invalid rows in the preview. Supported birth dates are `dd/MM/yyyy`, `d/M/yyyy`, and `yyyy-MM-dd`. Missing personal details are never invented.

See [IMPROVEMENTS.md](IMPROVEMENTS.md) for completed changes and remaining priorities.
