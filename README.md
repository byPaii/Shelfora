# Shelfora — Library Management System

**Manage your collection. Support your readers. Keep every loan in view.**

A database-backed library administration app for small school, community, and private libraries. Shelfora brings book inventory, member records, borrowing and returns, overdue tracking, and reports into one responsive workspace.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![C Sharp](https://img.shields.io/badge/C%23-12-239120?style=flat-square&logo=csharp&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework-Core%208-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Microsoft SQL Server](https://img.shields.io/badge/Microsoft-SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)
![Razor](https://img.shields.io/badge/Razor-Views-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=flat-square&logo=bootstrap&logoColor=white)
![Chart.js](https://img.shields.io/badge/Chart.js-4.4-FF6384?style=flat-square&logo=chartdotjs&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-Tests-512BD4?style=flat-square&logo=dotnet&logoColor=white)

## ✨ Features

- **Library dashboard** — Database-driven collection, copy, member, and overdue totals; borrowing and return trends; recent activity; popular books; and low-stock titles.
- **Book catalog** — Create, search, filter, sort, paginate, edit, and view books, including ISBN, category, publisher, year, shelf, and copy availability.
- **Category management** — Maintain categories and prevent duplicate names or removal while books are assigned.
- **Member management** — Register, search, filter, sort, and view members with their circulation history and membership status.
- **Borrowing workflow** — Record loans for active members, validate inventory and dates, calculate due dates from library policy, and update stock in a database transaction.
- **Full and partial returns** — Record individual return events, retain return history, protect against duplicate submissions, and return copies to inventory atomically.
- **Overdue tracking** — Find loans with outstanding copies and calculate overdue days and estimated fines using the saved daily rate.
- **Reports and CSV** — Review inventory, borrowing, return, overdue, popular-book, and member-activity reports; export filtered results as CSV.
- **Library settings** — Save the displayed library name, contact details, loan duration, and estimated daily fine rate.
- **Responsive interface** — Razor pages, Bootstrap components, Bootstrap Icons, and Chart.js with a mobile navigation menu and print-friendly reports.
- **Fictional demo data** — Load sample categories, books, members, and circulation records for local evaluation.

## 🧭 Main Pages

| Page | What it covers |
| --- | --- |
| Dashboard | Current library totals, trends, recent activity, popular books, overdue loans, and low stock |
| Books | Catalog search, category and availability filters, publication-year filter, details, and borrowing history |
| Categories | Category creation, editing, and protected deletion |
| Members | Member search and status filters, member details, and transaction history |
| Borrowing | Transaction list, search and status filters, transaction details, and recording returns |
| Overdue | Outstanding loans, overdue duration, and estimated fines |
| Reports | Inventory, borrowing, returns, overdue, popular-book, and member-activity reports with CSV export |
| Library settings | Library identity and saved circulation policy |

## 🧰 Technology

| Technology | Version | Purpose |
| --- | --- | --- |
| .NET SDK | 8.x | Build and run the application |
| ASP.NET Core MVC | 8.x | Routing, controllers, and server-rendered web app |
| C# | 12 | Application and business logic |
| Razor Views | ASP.NET Core 8 | Server-rendered pages and shared layouts |
| Entity Framework Core | 8.0.11 | SQL Server persistence, queries, and migrations |
| Microsoft SQL Server | Express 2025 locally; SQL Server-compatible provider | Relational database |
| Bootstrap | 5.3.3 | Responsive layout and components |
| Bootstrap Icons | 1.11.3 | Interface icons |
| Chart.js | 4.4.7 | Dashboard circulation chart |
| xUnit | 2.9.2 | Automated business-rule tests |

## 🚀 Run Locally

### Prerequisites

- .NET 8 SDK
- SQL Server Express or another supported SQL Server instance
- The EF Core CLI for applying migrations: `dotnet tool install --global dotnet-ef --version 8.0.11`
- Network access in the browser for Bootstrap, Bootstrap Icons, and Chart.js, which are loaded from public CDNs

The default connection string targets `localhost\SQLEXPRESS`. This workspace was set up with SQL Server Express 2025. To use a different SQL Server, set `SHELFORA_SQLSERVER` in PowerShell before running commands:

```powershell
$env:SHELFORA_SQLSERVER = "Server=YOUR_SQL_SERVER;Database=Shelfora;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
```

For SQL authentication, use a private secret or environment variable for credentials; never commit a real password or production connection string.

### Restore, migrate, and seed

Run these commands from the project root:

```powershell
dotnet restore
dotnet ef database update
dotnet run --no-launch-profile -- --seed
```

The seed command applies any pending migrations and inserts fictional sample records once. Normal application startup does not seed data automatically. To start with an empty database, skip the `--seed` command.

### Start the website

```powershell
dotnet run
```

Open the address printed by the application. The included launch profile uses `https://localhost:7164` and `http://localhost:5164`.

If Windows Application Control blocks the generated app executable in a synced or restricted folder, build first and launch the assembly through the .NET host instead:

```powershell
dotnet build
dotnet .\bin\Debug\net8.0\Shelfora.dll
```

The original workspace is in OneDrive, where executable loading may be restricted by local Windows policy. In that case, use a local working copy such as `C:\Shelfora` and run the commands there.

## 🗄️ Database & Migrations

The application uses SQL Server only; it does not silently fall back to SQLite or an in-memory database. The initial migration is `Migrations/20261014000000_InitialCreate.cs`.

```powershell
# Install the matching EF Core CLI if needed
dotnet tool install --global dotnet-ef --version 8.0.11

# Apply migrations
dotnet ef database update

# Scaffold a migration after changing the EF model
dotnet ef migrations add DescribeYourChange

# Apply the new migration
dotnet ef database update
```

Check `SHELFORA_SQLSERVER` or `appsettings.json` before applying migrations so they target the intended database. Migrations change the configured SQL Server database; do not point them at production unless that is intentional.

## 🧪 Build & Test

```powershell
dotnet build
dotnet test .\Tests\Shelfora.Tests\Shelfora.Tests.csproj
```

The current xUnit suite checks due-date calculation, fine estimates, and CSV quoting and spreadsheet-formula escaping. Build and all 9 tests passed in the development environment used to prepare this project.

## 📏 Circulation Rules

- A member must be active to borrow. A member with outstanding books cannot be deactivated.
- A borrow quantity must be positive and no greater than the book's available copies.
- A due date cannot precede the borrow date. New loans use the configured default duration (14 days initially).
- Borrowing and returns update inventory and circulation records in SQL Server transactions.
- Return quantities must be positive and cannot exceed the outstanding quantity. Each return is preserved as a separate history record.
- Return submission tokens prevent a retry of the same form from recording the same return twice. SQL Server row versions protect concurrent inventory changes.
- A book with borrowing history cannot be deleted. Total copies cannot be reduced below the number currently borrowed.
- A category with assigned books cannot be deleted. A member with transaction history cannot be deleted; deactivate the member instead.
- A loan is overdue when its due date has passed and copies remain outstanding.
- **Estimated fine = overdue days × outstanding copies × configured daily rate.** The application does not record fine payments.
- CSV exports quote values and guard against spreadsheet formula injection.

## 🗂️ Project Structure

```text
Controllers/                 MVC routes for dashboard and library workflows
Models/                      Entities, form models, and page models
Data/                        EF Core DbContext and fictional seed process
Services/                    Circulation, date/fine rules, and CSV formatting
Migrations/                  SQL Server schema migration and model snapshot
Views/                       Razor pages and shared navigation/layout
wwwroot/css/                 Responsive Shelfora styles
wwwroot/js/                  Small vanilla JavaScript interactions
Tests/Shelfora.Tests/        xUnit business-rule tests
Program.cs                   Application startup and dependency injection
appsettings.json             Local SQL Server connection and logging defaults
```

## 🔐 Security & Limitations

- Shelfora is currently a **local, single-user administrative demo**. Sign-in, authentication, and user authorization are not implemented.
- Anti-forgery tokens protect state-changing MVC forms, but do not replace authentication or authorization.
- Do not expose this application to the internet or use it as a multi-user service until an identity provider, authorization policies, production secrets, HTTPS, backups, monitoring, and a security review are in place.
- The demo seed data is fictional and uses reserved `example.test` email addresses.
- Estimated fines are informational only; no payments are recorded.
- The UI vendor assets are loaded from CDNs and require browser network access.

## 🏭 Production Notes

Before a production deployment:

1. Configure a dedicated SQL Server database and credentials through a secret manager.
2. Review and apply migrations deliberately; use `dotnet ef database update` as a deployment step.
3. Build and verify with `dotnet build` and `dotnet test`.
4. Configure authentication, authorization, HTTPS, logging, backups, and operational monitoring.
5. Do not run the fictional development seed process against production data.
6. Review the application’s single-user limitation and add access controls before accepting real library or member information.
