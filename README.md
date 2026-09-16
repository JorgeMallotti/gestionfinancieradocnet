# Gestión Financiera Interna (Internal Financial Management System)

Monorepo: **.NET 10 Web API + Angular 21 + SQL Server + Azure**.

**🔗 Live demo: <https://finanzas.mallottidigital.com>** — one-click demo accounts, no signup needed.

An internal financial management MVP: transactions, categories, dashboard, PDF/Excel
reports, audit log, email notifications, and multi-company support. Built to maximize
employability in the .NET job market — Clean Architecture, corporate-grade patterns.

## Stack

| Layer    | Technology                                                                        |
| -------- | --------------------------------------------------------------------------------- |
| Backend  | .NET 10 Web API (Clean Architecture: Domain / Application / Infrastructure / Api) |
| Frontend | Angular 21 (standalone, Signals, Angular Material, @ngx-translate)                |
| Database | SQL Server 2022 (Docker locally, Azure SQL in production)                         |
| Patterns | Repositories + FluentValidation + Result pattern                                  |
| Auth     | ASP.NET Core Identity + JWT + refresh token in httpOnly cookie                    |
| Reports  | QuestPDF (PDF) + ClosedXML (Excel)                                                |
| Logging  | Serilog + Application Insights (Azure)                                            |
| CI/CD    | GitHub Actions (App Service + Static Web Apps)                                    |

## Repository layout

```
backend/    .NET 10 solution (GestionFinanciera.slnx) — 4 projects + 2 test projects
frontend/   Angular 21 application
docker-compose.yml   Local SQL Server 2022
AGENTS.md   Agent contract — read before any change (security + architecture rules)
```

## Local development

### Prerequisites

- .NET SDK 10
- Node.js 22+ and Angular CLI 21
- Docker Desktop (for SQL Server)

### 1. Start the database

```powershell
docker compose up -d
```

SQL Server 2022 listens on `localhost:1433` (password default in `docker-compose.yml`,
overridable via `MSSQL_SA_PASSWORD` env var).

### 2. Run the backend

```powershell
cd backend
dotnet user-secrets init --project src/GestionFinanciera.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=GestionFinanciera;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True"
dotnet run --project src/GestionFinanciera.Api
```

API: `http://localhost:5119` — Swagger at `/swagger`.

### 3. Run the frontend

```powershell
cd frontend
npm install
ng serve
```

Frontend: `http://localhost:4200` — proxies `/api` to the backend via `proxy.conf.json`.

### Demo accounts (one-click quick access)

The MVP demo ships with a seeded demo company (**Acme Demo SL**) and three
accounts — one per role — so you can explore the app with a single click from
the login page:

| Key       | Email                        | Role    | Can do                                                 |
| --------- | ---------------------------- | ------- | ------------------------------------------------------ |
| `admin`   | `demo.admin@gestfin.local`   | Admin   | Everything: users, categories, transactions, audit log |
| `finance` | `demo.finance@gestfin.local` | Finance | Reports: PDF/Excel exports and email delivery          |
| `user`    | `demo.user@gestfin.local`    | User    | Day-to-day operations on categories and transactions   |

The shared demo password (`Demo:Password` config, default `Passw0rd!123`) lives
**only in the backend** — the public API (`GET /api/auth/demo-accounts`) exposes
metadata, never credentials. The company ships with 12 sample transactions so
the dashboard has realistic data.

Disable the feature entirely in production with the App Setting
`Demo:Enabled=false` (default `true`). The demo password is intentionally a
documented demo credential — replace it via `Demo:Password` for real
deployments.

### Database migrations (EF Core)

Never use `EnsureCreated()`. Every schema change:

```powershell
cd backend
dotnet ef migrations add <DescriptiveName> --project src/GestionFinanciera.Infrastructure --startup-project src/GestionFinanciera.Api
dotnet ef database update --project src/GestionFinanciera.Infrastructure --startup-project src/GestionFinanciera.Api
```

See `AGENTS.md` §7 for the strict migration rules.

## Deployment (Azure)

See `AGENTS.md` §14. Target: App Service (API) + Azure SQL + Static Web Apps (SPA),
custom subdomain of Jorge's existing domain, secrets in App Settings / Key Vault.

## Conventions

- English everywhere in code; academic explanations to Jorge in es/pt (AGENTS.md §15).
- Git workflow: `feat/*` → `staging` → `main` (AGENTS.md §9).
- Security hard rules in AGENTS.md §13 (never skip).
