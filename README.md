# Gestión Financiera — Cloud Banking Demo

Monorepo: **.NET 10 Web API + Angular 21 + SQL Server + Azure**.

**🔗 Live demo: <https://finanzas.mallottidigital.com>** — one-click demo accounts, no signup needed.

A bank-like platform built as a portfolio-grade reference implementation. One bank acts
as the intermediary while its client accounts — individuals and companies — hold
balances, transfer money between each other, request loans and open claims that the
bank mediates.

## What it does

| Area              | Capability                                                                                                                                                                  |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Accounts**      | Every client owns one account with a balance. **No account can go negative** — overdrafts are rejected in the service layer, not only in the UI.                            |
| **Transfers**     | Peer-to-peer payments between clients, or between a client and the bank.                                                                                                    |
| **Ledger**        | Movements are **immutable and append-only**: there is no `PUT`/`DELETE` endpoint for one. Corrections are _stacked_ as new movements, the way `git` never rewrites history. |
| **Loans**         | A client requests an amount with a reason; the bank approves or rejects. On approval the treasury funds the client, who repays in full or in instalments.                   |
| **Claims**        | A client disputes a movement; the bank proposes a corrective transfer; the parties consent and the correction is stacked on the ledger.                                     |
| **Categories**    | A bank-managed catalogue, visible to every client and optionally applied to a movement.                                                                                     |
| **Onboarding**    | Public signup creates a client account in `Pending` state until the bank approves it.                                                                                       |
| **Reporting**     | Any client can export their own movements as PDF or Excel.                                                                                                                  |
| **Notifications** | In-app notifications for money received, loan decisions, claim updates and account changes.                                                                                 |
| **Audit**         | Sensitive mutations write an append-only audit trail, readable by the bank only.                                                                                            |
| **i18n**          | Full interface in English, Spanish and Portuguese.                                                                                                                          |

## Stack

| Layer    | Technology                                                                                                           |
| -------- | -------------------------------------------------------------------------------------------------------------------- |
| Backend  | .NET 10 Web API — Clean Architecture (Domain / Application / Infrastructure / Api)                                   |
| Frontend | Angular 21 — standalone components, Signals, Angular Material (M3), `@ngx-translate`                                 |
| Database | SQL Server 2022 (Docker locally, Azure SQL in production) through EF Core 10                                         |
| Patterns | Repository + FluentValidation + `Result<T>` — expected failures are values, never exceptions                         |
| Auth     | ASP.NET Core Identity; short-lived JWT access token + rotating refresh token in an httpOnly `SameSite=Strict` cookie |
| Reports  | QuestPDF (PDF) + ClosedXML (Excel)                                                                                   |
| Logging  | Serilog (structured) + Application Insights in production                                                            |
| Security | Strict JSON (unknown fields rejected), ProblemDetails (RFC 7807), rate limiting, CSP and security headers            |
| Testing  | xUnit + FluentAssertions; Vitest running the Angular specs in a real Chromium browser                                |
| CI/CD    | GitHub Actions — passwordless OIDC deployments, no secret stored in the repository                                   |
| i18n     | en / es / pt, flat JSON bundles kept in sync                                                                         |

## Repository layout

```
backend/            .NET 10 solution (GestionFinanciera.slnx) — 4 source projects + 2 test projects
frontend/           Angular 21 application (translation bundles in public/i18n/)
.github/workflows/  CI plus the two deployment pipelines
docker-compose.yml  Local SQL Server 2022
.env.example        Template for the local database password (.env is git-ignored)
AGENTS.md           Engineering contract — architecture, security and process rules
```

## Local development

### Prerequisites

- .NET SDK 10
- Node.js 22+ and Angular CLI 21
- Docker Desktop (for SQL Server)

### 1. Start the database

The container password is read from `MSSQL_SA_PASSWORD` and has **no default value**:
a committed fallback would be a credential living inside the repository.

```powershell
Copy-Item .env.example .env        # .env is git-ignored
# edit .env and set MSSQL_SA_PASSWORD
# (8+ characters, using three of: uppercase, lowercase, digits, symbols)
docker compose up -d --wait
```

SQL Server 2022 listens on `localhost:1433` and stores data in the `sqlserver-data` volume.

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

The seeded bank is **Acme Demo Bank**: one operator plus two clients — a person and a
company — so every flow can be explored from the login page with a single click:

| Key     | Email                      | Role                                                            |
| ------- | -------------------------- | --------------------------------------------------------------- |
| `admin` | `demo.admin@gestfin.local` | Bank operator: approves clients, decides loans, mediates claims |
| `ana`   | `demo.ana@gestfin.local`   | Client (person): transfers money and opens claims               |
| `xyz`   | `demo.xyz@gestfin.local`   | Client (company): transfers money and opens claims              |

The accounts already hold balances and ship with sample transfers, a loan and a claim,
so the dashboard and the ledger have realistic data from the first load.

The shared demo password (`Demo:Password`, default `Passw0rd!123`) lives **only in the
backend** — the public API (`GET /api/auth/demo-accounts`) exposes metadata, never
credentials. It is deliberately a documented demo credential: replace it through
`Demo:Password` for any real use, or disable the whole feature with `Demo:Enabled=false`.

The demo resets itself on a schedule: balances return to the seed and any account created
by a visitor is removed.

### Database migrations (EF Core)

Never use `EnsureCreated()`. Every schema change:

```powershell
cd backend
dotnet ef migrations add <DescriptiveName> --project src/GestionFinanciera.Infrastructure --startup-project src/GestionFinanciera.Api
dotnet ef database update --project src/GestionFinanciera.Infrastructure --startup-project src/GestionFinanciera.Api
```

See `AGENTS.md` §7 for the strict migration rules.

## Design notes

- **Tenancy**: every business row carries a `CompanyId` and an EF Core global query
  filter enforces isolation. The demo runs a single bank, but the schema is
  multi-tenant-ready — adding a second bank needs no migration. Cross-tenant lookups
  return `404` rather than `403`, so they never leak the existence of a record.
- **The bank cannot lend money it does not have**: the treasury balance is checked
  before a loan is funded.
- **Corrections are additive**: the ledger is never rewritten in place. A correction is
  a new movement, which is what makes the audit trail trustworthy.

## Tests

```powershell
# from the repository root
dotnet test backend/GestionFinanciera.slnx   # unit + integration
cd frontend; ng test --browsers=chromium     # Angular specs in a real browser
```

Unit tests cover the services and the validators; integration tests exercise the real
PDF, Excel and email builders. The suite is a quality gate in CI — a failing test blocks
the deployment.

## Deployment (Azure)

```
Static Web Apps (Angular SPA)  →  App Service (Linux, .NET 10)  →  Azure SQL
      finanzas.…                       api.…                        + Key Vault
```

- **No long-lived secrets.** The connection string and the JWT signing key live in
  **Azure Key Vault** and are consumed through references resolved with the App
  Service's **system-assigned managed identity**.
- **The application holds no database password at all**: Azure SQL authenticates the
  App Service through Entra ID (`Authentication=Active Directory Managed Identity`).
- **CI/CD authenticates with OIDC**, through a federated identity credential on an
  Entra ID app registration. No publish profile, no deployment token and no client
  secret is stored in GitHub.
- Deployments are gated by a protected GitHub environment and verified by a smoke test
  that exercises the data path, not just `/health`.
- Migrations are applied from an idempotent script generated per commit; the application
  never calls `EnsureCreated()`.

See `AGENTS.md` §14 for the full runbook.

## Conventions

- Code, comments, commits and docs in English. Decision records may be written in the
  maintainer's language (es/pt) with technical terms kept in English (`AGENTS.md` §15).
- Git workflow: `feat/*` → `staging` → `main`; both protected branches take pull
  requests only (`AGENTS.md` §9).
- Security hard rules live in `AGENTS.md` §13 and are not optional.

## License

MIT — see [LICENSE.md](LICENSE.md).
