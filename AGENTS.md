# Gestión Financiera — Bank Demo (Agent Instructions)

Monorepo: **.NET 10 Web API + Angular 21 + SQL Server + Azure**.

> Product pivot approved by Jorge on **2026-09-02**: the MVP is a **bank-like demo** — one
> `Admin` operates as the bank and `User` clients (persons/companies) hold accounts, transfer
> money between each other, request loans and open claims mediated by the Admin. See §2.1.
>
> This file adapts the security best practices from the previous project's AGENTS.md
> (NestJS/Next.js) to the new stack. Nothing from the old security rules was dropped —
> each rule was rewritten for .NET/Angular. New sections: Tenancy, Azure Deployment,
> and Decision Records (the rationale behind each change).

## Table of Contents

| Sec | Title                                                                                                                |
| --- | -------------------------------------------------------------------------------------------------------------------- |
| 1   | [Mandatory Reads Before Any Change](#1-mandatory-reads-before-any-change)                                            |
| 2   | [Stack & Key Decisions (LOCKED)](#2-stack--key-decisions-locked)                                                     |
| 3   | [Architecture Rules (Clean Architecture)](#3-architecture-rules-clean-architecture)                                  |
| 4   | [Project Structure (Strictly Enforced)](#4-project-structure-strictly-enforced)                                      |
| 5   | [Module Convention (.NET Backend)](#5-module-convention-net-backend)                                                 |
| 6   | [Module Convention (Angular Frontend)](#6-module-convention-angular-frontend)                                        |
| 7   | [Database Migration Rules (EF Core — Strict)](#7-database-migration-rules-ef-core--strict)                           |
| 8   | [Tenancy Rules (Single Bank — Multitenant-Ready)](#8-tenancy-rules-single-bank--multitenant-ready)                   |
| 9   | [Git Workflow: feat/\* → staging → main](#9-git-workflow-feat--staging--main)                                        |
| 10  | [Styling — Angular Material (Exclusive)](#10-styling--angular-material-exclusive)                                    |
| 11  | [Internationalization (i18n) — @ngx-translate](#11-internationalization-i18n--ngx-translate)                         |
| 12  | [State Management & Optimistic Updates](#12-state-management--optimistic-updates)                                    |
| 13  | [Security Constraints (Hard Rules)](#13-security-constraints-hard-rules)                                             |
| 14  | [Deployment Rules (Azure)](#14-deployment-rules-azure)                                                               |
| 15  | [Decision Records (Every Change Documents Its Rationale)](#15-decision-records-every-change-documents-its-rationale) |
| 16  | [Agent Change Protocol](#16-agent-change-protocol)                                                                   |
| 17  | [Coding Standards](#17-coding-standards)                                                                             |
| 18  | [Protected Files (Agent MUST NOT Modify)](#18-protected-files-agent-must-not-modify)                                 |
| 19  | [Verification Checklist (Agent Self-Check)](#19-verification-checklist-agent-self-check)                             |

---

## 1. Mandatory Reads Before Any Change

Before writing a single line of code, read:

- This file (`AGENTS.md`)
- If touching the database: the EF Core `DbContext` and the `Migrations/` folder
- If creating frontend components: existing patterns in `frontend/src/app/`
- If modifying an existing backend feature: the full feature folder
- This file again, especially §15 (Decision Records) — every change leaves a rationale behind

---

## 2. Stack & Key Decisions (LOCKED)

Decisions agreed with Jorge on 2026-08-23. **Do not change without explicit approval.**

| Decision          | Choice                                                                                                 | Why                                                                                        |
| ----------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------ |
| Backend           | **.NET 10 Web API** (modern LTS)                                                                       | Modern LTS: a supported, long-term-service release and the current corporate standard      |
| Frontend          | **Angular 21** (standalone components) + **Angular Material**                                          | Corporate standard for .NET shops; ready components                                        |
| Database          | **SQL Server** — Docker `mcr.microsoft.com/mssql/server` locally, **Azure SQL** in prod                | Same engine dev/prod (no drift)                                                            |
| Architecture      | **Clean Architecture** (Domain / Application / Infrastructure / Api)                                   | Keeps the domain free of framework dependencies; the standard for non-trivial .NET systems |
| Backend patterns  | **Repositories + FluentValidation + Result pattern** (+ AutoMapper optional)                           | "Corporate level" — what real companies ask for                                            |
| Auth              | **ASP.NET Core Identity + JWT (access) + refresh token in httpOnly cookie**                            | Secure; fixes the localStorage token debt of the old project                               |
| Roles             | `Admin` (bank operator & mediator) + `User` (client: person or company). **`Finance` removed**         | Bank demo model (Jorge, 2026-09-02)                                                        |
| Multi-tenancy     | **Single bank in the MVP** — `CompanyId` kept on all tables + global query filters (multitenant-ready) | Company = the bank; avoids painful migrations later                                        |
| PDF               | **QuestPDF**                                                                                           | Free for this use case, de-facto standard in .NET                                          |
| Excel export      | **ClosedXML**                                                                                          | Free (MIT), standard                                                                       |
| Email             | **MailKit** (SMTP) or SendGrid SDK                                                                     | MailKit is the modern standard (SmtpClient is legacy)                                      |
| Validation        | **FluentValidation**                                                                                   | Seen in almost every .NET job offer                                                        |
| Logging           | **Serilog** + Application Insights (Azure)                                                             | Structured logging, corporate standard                                                     |
| API docs          | **Swagger / OpenAPI**                                                                                  | Free documentation, testable endpoints                                                     |
| Error contract    | **ProblemDetails** (RFC 7807)                                                                          | Standard ASP.NET Core format                                                               |
| Tests             | **xUnit + FluentAssertions** (unit), **WebApplicationFactory** (integration)                           | Corporate standard                                                                         |
| CI/CD             | **GitHub Actions** (backend → App Service, frontend → Static Web Apps)                                 | Free, standard                                                                             |
| Frontend state    | **Angular Signals + inject-based services**                                                            | Modern Angular idiom (stable since 17, mature in 21)                                       |
| i18n              | **@ngx-translate**, files `en.json` / `es.json` / `pt.json`                                            | Community standard, same pattern as old project                                            |
| Language of code  | English everywhere (code, comments, commits, docs)                                                     | Global standard                                                                            |
| Conversation lang | Rationale sections in the owner's language (es/pt); technical terms kept in English                    | §15                                                                                        |

### 2.1 Product Model — Bank Demo (LOCKED 2026-09-02)

Approved by Jorge on 2026-09-02 via the decision wizard. **This supersedes the old
"internal company expense manager" concept.** The MVP is now a **bank-like demo platform**:

- **One bank in the MVP** (a `Company`, seeded once — e.g. "Acme Demo Bank"). The bank owns a
  **treasury account** with a very high starting balance.
- **Roles are 2**: `Admin` = bank operator/mediator (approves clients, decides loans, mediates
  claims) and `User` = **client** (a person or a company with exactly ONE login in the MVP).
  **`Finance` role is REMOVED.**
- **Clients are independent entities**: they transfer money **between each other** (P2P) and
  with the bank. Money moves between different entities — never "inside" one company.
- **Accounts with balance (wallet)**: every client has one account. **Hard rule: no account can
  go negative** (nobody spends more than they have). The bank cannot lend more than its treasury.
- **Immutable ledger (git-style, append-only)**: a movement is created once and **NEVER edited or
  deleted** — there are NO `PUT`/`DELETE` endpoints for movements. Corrections are **stacked** as
  new movements (e.g. a corrective transfer), exactly like `git` commits: never rewrite history,
  always add on top. Every correction is traceable.
- **Transfers**: any client can pay/receive from another client or the bank without asking
  permission (like a bank account). Overdrafts are rejected.
- **Loans (simple, MVP)**: a client requests a loan (amount + reason) → `Admin` approves/rejects →
  on approval the bank treasury transfers the amount to the client → the client repays anytime
  (full or partial) with a transfer back. No interest, no deadlines in the MVP.
- **Claims (reclamaciones)**: if something is wrong with a movement, the involved client opens a
  claim → `Admin` acts as **mediator** → proposes a **corrective transfer** (e.g. refund the
  difference) → **the parties consent** → the corrective movement is executed and stacked on the
  ledger. Nothing is ever overwritten.
- **Categories**: managed by the `Admin` as a catalog **visible to all clients**; clients may tag
  movements with a category **optionally** (for their own organization).
- **Client onboarding**: public registration creates a client account with status `Pending` — the
  `Admin` must **approve** it before the client can operate. The signup UX must explain this
  ("wait for Admin approval") and how the Admin approves.
- **Demo seed**: bank + Admin + **2 demo clients** (e.g. Ana — person, XYZ SL — company) with
  starting balances, seeded transfers between them, plus one sample loan and one sample claim so
  visitors can explore every flow with the 1-click demo buttons.
- **Demo abuse hardening** (the demo is public, so the threat model is the _visitor_):
  - Demo identities are **exempt from Identity lockout** while `Demo:Enabled=true`. Their password
    is public by design, so there is no secret to brute-force — lockout only let anyone disable the
    1-click buttons with 5 wrong passwords. The periodic reset also clears any lockout left behind.
  - The periodic reset **wipes the category catalog** too. Categories are Admin-writable and the
    demo Admin token is public, so anything injected there survived every reset and was shown to
    the next visitor. Movements/loans/claims/audit were already wiped; categories were the gap.
  - The periodic reset **purges expired/rotated refresh-token rows**: a row is written on every
    login/refresh and the demo identities are permanent, so nothing pruned them (unbounded growth
    on a SQL Basic database).
  - The refresh cookie's `Secure` flag follows the **environment**, never `Request.IsHttps`
    (behind App Service that value depends on `ForwardedHeaders:Enabled` being on).
- **Every client can export their own movements as PDF/Excel** regardless of role (existing
  `GET /api/reports/pdf|excel`), with a quick-access button on the movements page.
- **Tenancy**: `CompanyId` is **kept** on all business tables with global query filters
  (multitenant-ready). In the MVP `Company` = the bank (single seeded row); signup creates a
  client under that bank, NOT a new company.

---

## 3. Architecture Rules (Clean Architecture)

Strict layered architecture with **inward dependency rule** (nothing in an inner layer
depends on an outer layer):

| Layer              | Technology / Project               | Responsibility                                                                                                             |
| ------------------ | ---------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **Domain**         | `GestionFinanciera.Domain`         | Entities, enums, value objects, domain exceptions. Zero dependencies.                                                      |
| **Application**    | `GestionFinanciera.Application`    | Use cases, DTOs, FluentValidation validators, Result pattern, repository interfaces. Depends only on Domain.               |
| **Infrastructure** | `GestionFinanciera.Infrastructure` | EF Core (DbContext, migrations, repositories), Identity, JWT, email, PDF, Excel, audit. Implements Application interfaces. |
| **Presentation**   | `GestionFinanciera.Api`            | Controllers, middleware, filters, `Program.cs`, DI composition root. Depends on Application + Infrastructure.              |

**Data flow (the only allowed direction):**

```
Angular  →  HTTP (REST)  →  Api Controllers  →  Application Services  →  Infrastructure Repositories  →  EF Core  →  SQL Server
```

**Hard rules:**

- **Controllers have ZERO business logic.** They map HTTP → service calls → HTTP responses.
- **Services contain ALL business logic** (validation orchestration, calculations, policies).
- **Repositories contain ONLY data access** — no business decisions.
- **Domain MUST NOT reference** EF Core, HttpContext, or any framework type.
- **Services never see `HttpContext` or claims directly** — the controller resolves the
  tenant/user from the JWT and passes it as parameters (`companyId`, `userId`).
- **All service methods return `Result<T>`** (or `Result`) — never throw for expected errors.
  Exceptions are reserved for unexpected failures.
- **Every public mutation returns the full resource** (never just an ID) so the frontend
  can update state without refetching (§12).

---

## 4. Project Structure (Strictly Enforced)

### Backend (`backend/`)

```
backend/
├── GestionFinanciera.slnx        # .NET 10 solution (new XML format, default since .NET 9/10)
├── src/
│   ├── GestionFinanciera.Api/
│   │   ├── Controllers/            # Thin controllers, one per resource
│   │   ├── Middleware/             # Security headers, tenant resolution, exception handling
│   │   ├── Filters/                # Audit filter, global exception filter
│   │   ├── Extensions/             # IServiceCollection extensions (DI, auth, CORS, rate limiting)
│   │   ├── Program.cs              # Composition root — wiring only
│   │   ├── appsettings.json        # Non-secret config only
│   │   └── Properties/launchSettings.json
│   ├── GestionFinanciera.Application/
│   │   ├── Features/
│   │   │   └── {Feature}/          # e.g. Auth, Movements(ledger), Transfers, Loans, Claims, Categories, Dashboard, Reports
│   │   │       ├── DTOs/           # Records: Create/Update/Query/Response
│   │   │       ├── Validators/     # FluentValidation validators (one per write DTO)
│   │   │       ├── Interfaces/     # I{Feature}Repository, I{Feature}Service
│   │   │       └── {Feature}Service.cs
│   │   ├── Common/
│   │   │   ├── Results/            # Result<T> pattern
│   │   │   ├── Pagination/         # PagedResult<T>, pagination DTO
│   │   │   └── Behaviors/          # (optional) MediatR-style pipelines — do NOT add MediatR without approval
│   │   └── Abstractions/           # ICurrentTenant, ICurrentUser abstractions
│   ├── GestionFinanciera.Domain/
│   │   ├── Entities/               # Company(bank), ClientAccount, Movement(ledger, append-only), Category, Loan, Claim, AuditLog...
│   │   ├── Enums/                  # TransactionType, UserRole, AuditAction...
│   │   ├── ValueObjects/           # Money, DateRange...
│   │   └── Exceptions/             # DomainException, ValidationException...
│   └── GestionFinanciera.Infrastructure/
│       ├── Persistence/
│       │   ├── ApplicationDbContext.cs
│       │   ├── Migrations/         # Generated by `dotnet ef migrations add` — NEVER hand-edited
│       │   ├── Configurations/     # IEntityTypeConfiguration classes
│       │   └── Repositories/       # EF Core repository implementations
│       ├── Identity/               # Identity integration, JWT generator, refresh token store
│       ├── Services/               # EmailService (MailKit), PdfService (QuestPDF), ExcelService (ClosedXML)
│       ├── Audit/                  # AuditLog writer
│       └── DependencyInjection.cs
└── tests/
    ├── GestionFinanciera.UnitTests/          # xUnit — services, validators, domain
    └── GestionFinanciera.IntegrationTests/   # WebApplicationFactory — API + DB (Testcontainers or local SQL)
```

### Frontend (`frontend/`)

```
frontend/
├── public/
│   └── i18n/                        # en.json, es.json, pt.json — @ngx-translate (public/ = web root)
└── src/
    ├── app/
    │   ├── core/                    # Singleton services: auth, tenant, token refresh, API client, guards, interceptors
    │   ├── features/                # Lazy-loaded feature folders
    │   │   ├── auth/                # Login, register (client account request), demo 1-click buttons
    │   │   ├── dashboard/           # Balance, charts, KPI cards
    │   │   ├── movements/           # Immutable movements list: 2 columns (description + amount) → details on click
    │   │   ├── transfers/           # Send money to another client (P2P)
    │   │   ├── loans/               # Request a loan; repay partial/full (client) / decide (Admin)
    │   │   ├── claims/              # Open a claim on a movement; consent to corrective transfer
    │   │   ├── categories/          # Admin-managed catalog; optional tags on movements
    │   │   ├── reports/             # PDF / Excel export UI (own movements, any role)
    │   │   ├── audit/               # Audit log viewer (Admin)
    │   │   └── admin/               # Admin panel: approve pending clients, active clients, treasury
    │   ├── shared/                  # Reusable components, pipes, directives, Material config
    │   ├── app.routes.ts            # Routes with lazy loading + guards
    │   ├── app.config.ts            # Providers (HTTP, translate, material)
    │   └── app.component.ts
    ├── environments/                # environment.ts / environment.prod.ts (API base URL)
    └── styles/                      # Material theme (light/dark), global SCSS
```

### Repository root (`.github/`)

```
.github/
├── dependabot.yml                 # Weekly updates, all targeting staging (§9)
└── workflows/
    ├── ci.yml                     # PR + staging: build, tests, lint, format (no Azure access)
    ├── branch-policy.yml          # PR to main: must come from staging (§9)
    ├── deploy-backend.yml         # main: dotnet publish → App Service (OIDC + manual approval)
    └── deploy-frontend.yml        # main: ng build → Static Web Apps (OIDC + manual approval)
```

`.github/workflows/` is the only path GitHub Actions reads (it is not configurable), so it is an
explicit exception to the rule below. It holds **workflow YAML only** — never application code,
scripts or secrets.

**The agent MUST NOT create files outside this structure.**

---

## 5. Module Convention (.NET Backend)

Every feature follows the same pattern (Contracts → Validation → Service → Controller):

```csharp
// 1. DTO — contract (record, immutable)
public sealed record TransferDto(
    Guid ToClientId,       // receiver = another client of the same bank
    decimal Amount,        // > 0, currency default EUR
    string? CategoryId,    // optional tag from the Admin catalog
    string? Description);

// 2. FluentValidation — defines the API contract rules
public sealed class TransferValidator : AbstractValidator<TransferDto>
{
    public TransferValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be positive.");
        RuleFor(x => x.ToClientId).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

// 3. Service — ALL business logic (balances, ledger), returns Result<T>
public interface ITransferService
{
    Task<Result<MovementDto>> CreateAsync(
        TransferDto dto, Guid companyId, Guid fromClientId, CancellationToken ct);
}

public sealed class TransferService(
    IAccountRepository accounts,
    IMovementRepository movements) : ITransferService
{
    public async Task<Result<MovementDto>> CreateAsync(
        TransferDto dto, Guid companyId, Guid fromClientId, CancellationToken ct)
    {
        // Business rule: the payer can never go negative (overdraft rejected)
        var payer = await accounts.GetByClientAsync(companyId, fromClientId, ct);
        if (payer is null)
            return Result<MovementDto>.Failure(ErrorCode.NotFound, "Account not found.");

        if (payer.Balance < dto.Amount)
            return Result<MovementDto>.Failure(
                ErrorCode.Conflict, "Insufficient funds — the account cannot go negative.");

        var payee = await accounts.GetByClientAsync(companyId, dto.ToClientId, ct);
        if (payee is null)
            return Result<MovementDto>.Failure(ErrorCode.NotFound, "Receiver not found.");

        // Append-only: a movement is created once and never edited/deleted
        var movement = Movement.CreateTransfer(
            companyId, fromClientId, dto.ToClientId, dto.Amount, dto.CategoryId, dto.Description);

        var saved = await movements.AddAsync(movement, ct);
        return Result<MovementDto>.Success(MovementDto.FromEntity(saved));
    }
}

// 4. Controller — thin, delegates to service (POST/GET only — no PUT/DELETE)
[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransfersController(ITransferService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MovementDto>> GetById(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var clientId = User.GetUserId(); // a client's own movements only
        var result = await service.GetByIdAsync(id, companyId, clientId, ct);
        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<MovementDto>> Create(TransferDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var clientId = User.GetUserId();
        var result = await service.CreateAsync(dto, companyId, clientId, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : this.ToActionResult(result);
    }
}
```

**Rules:**

- Primary constructors for DI (`public sealed class X(Y y)`) — modern .NET idiom.
- `sealed` classes by default (unless designed for inheritance).
- Repository interface lives in Application; implementation lives in Infrastructure.
- Every write DTO has a validator; validators are unit-tested.
- Services never expose `Task` without `CancellationToken` on async methods.

---

## 6. Module Convention (Angular Frontend)

- **Standalone components only** — no `NgModule` files for features.
- **Lazy loading** via `loadComponent` in `app.routes.ts` for every feature.
- **ChangeDetectionStrategy.OnPush** on all components.
- **Signals** for component state; services via `inject()`; `computed()`/`effect()` where needed.
- Feature folder shape:

```
features/movements/
├── movements.routes.ts          # Lazy routes
├── pages/
│   ├── movement-list.page.ts    # List: 2 columns (description + amount), click → details
│   └── movement-detail.page.ts  # Read-only details (movements are immutable)
└── services/
    └── movements.service.ts     # Typed HTTP calls to the API client
```

- **Data flows one way**: components → services (injectable) → `HttpClient` → API.
- Components contain **zero business logic** — only presentation and event handling.
- Typed responses: define `MovementDto` interfaces in `app/shared/models/` mirroring
  the backend DTOs (or use OpenAPI-generated client if approved).

---

## 7. Database Migration Rules (EF Core — Strict)

**`EnsureCreated()` is FORBIDDEN** — it bypasses migration history (equivalent of the old
project's forbidden `prisma db push`).

| Command                              | When to Use         | Creates Migration? | Notes                              |
| ------------------------------------ | ------------------- | ------------------ | ---------------------------------- |
| `dotnet ef migrations add <Name>`    | Every schema change | ✅ Yes             | Run from `Infrastructure` project  |
| `dotnet ef database update`          | Local dev only      | ❌ No              | Applies pending migrations locally |
| `dotnet ef migrations script`        | Prod / CI           | ❌ No              | Generates idempotent SQL script    |
| `dotnet ef dbcontext ensure-created` | **NEVER**           | ❌ No              | Bypasses history — forbidden       |

**Hard rules:**

- **NEVER** use `EnsureCreated()` for the real schema.
- **ALWAYS** use `dotnet ef migrations add <descriptive_name>` for every schema change.
- **EVERY** schema change produces ONE new migration in `Migrations/`.
- **NEVER** edit, delete, or rename existing migration files.
- **ALWAYS** commit migrations to git alongside the schema change.
- Rollback = a NEW migration that reverses the change — never modify an applied one.
- Production applies migrations via the generated SQL script during deploy (§14) — or an
  approved startup migration runner; never interactive `database update` in prod.

---

## 8. Tenancy Rules (Single Bank — Multitenant-Ready)

Since 2026-09-02 the product is a **bank demo**: a `Company` row represents **the bank** (one
seeded instance in the MVP). Every record still belongs to the bank via `CompanyId` so the schema
is ready for multiple banks later without migrations.

**Hard rules:**

- Every business entity carries `CompanyId` (`UNIQUEIDENTIFIER`, non-nullable): `Category`,
  `Movement`, `Loan`, `Claim`, `AuditLog`... `ClientAccount` and `ApplicationUser` belong to the bank.
- **Tenant resolution**: `CompanyId` comes ONLY from the JWT claim (`company_id`) — **never** from
  URL, query string, or request body.
- **EF Core global query filter** on every business entity enforces isolation automatically — a
  query can never leak another bank's rows:

```csharp
modelBuilder.Entity<Movement>()
    .HasQueryFilter(m => m.CompanyId == _tenantProvider.CompanyId);
```

- Repositories receive `companyId` as a parameter and include it in every `Where`.
- Cross-tenant access attempts must return `404` (not `403`) to avoid leaking existence.
- The bank (`Company`) is **seeded once** by the demo seeder (e.g. "Acme Demo Bank") together with
  its treasury account and the initial `Admin` user.
- **Registration no longer creates a company.** Signup creates a **client account** (role `User`,
  status `Pending`) under the existing bank; the bank `Admin` approves it before it can operate.
- Clients may only read/write their **own** account and movements. A client can target another
  client **only as transfer counterparty** (the receiver's balance is credited) — never read or
  modify another client's data.

---

## 9. Git Workflow: `feat/*` → `staging` → `main`

### Branch Hierarchy

```
main     → Production. Receives merges from staging only via PR.
staging  → Pre-production (QA, integration). Receives merges from feat/* only via PR.
feat/*   → Feature branches. Created from staging. Push allowed.
```

| Branch    | Created From | Merge Via              | Push Allowed? |
| --------- | ------------ | ---------------------- | ------------- |
| `main`    | —            | PR from `staging` only | ❌ No         |
| `staging` | `main`       | PR from `feat/*` only  | ❌ No         |
| `feat/*`  | `staging`    | —                      | ✅ Yes        |

### Rules

- **NEVER** commit directly to `main` or `staging` — both are protected branches.
- Branch naming: `feat/add-auth-module`, `fix/login-validation-error`,
  `refactor/transfers-service`, `chore/update-dependencies`, `docs/api-readme`.
- Commits follow [Conventional Commits](https://www.conventionalcommits.org/):
  `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`, `style:`.
- Before opening a PR to `staging`:
  - `dotnet test` (backend) and `ng test` (frontend) pass
  - `dotnet build` and `ng build` clean
  - `dotnet format --verify-no-changes` and `ng lint` clean
  - Branch rebased onto latest `staging` (`git rebase staging`)
- PR to `main` only after `staging` validated (tests + manual QA).

### Branch protection (what actually enforces the rules above)

GitHub **has no native rule** for "a pull request into `main` must come from
`staging`", so that rule is enforced as a check: `branch-policy.yml` fails unless
the source branch is `staging` **and** it belongs to this repository (not a fork),
and `PR must come from staging` is a required status check on `main`.

**Merging strategy: merge commits, never squash.** History is the record of why
each change was made, so the individual commits with their rationale are kept.
This is why **"Require linear history" is deliberately NOT enabled** — it would
block merge commits and force squash or rebase.

Required status checks: the two `ci.yml` jobs plus `PR must come from staging`.

**Do NOT add the "Restrict updates" rule.** It does block direct pushes, but it
also refuses _every_ update of the ref — **including the merge GitHub performs**.
With an empty bypass list, which is what we want so the rules apply to the owner
too, the release pull request becomes permanently `BLOCKED` while every check is
green. Measured on 2026-09-16 by removing it and watching `mergeStateStatus` go
from `BLOCKED` to `CLEAN` and back.

**"Require a pull request before merging" already blocks direct pushes on its
own** — a real push is refused with _"Changes must be made through a pull
request"_ — so `update` adds nothing here. If it is ever genuinely needed (say, to
stop a bot writing to `main`), it must be added **with** `bypass_mode:
"pull_request"` so the merge stays possible.

⚠️ `git push --dry-run` does **not** evaluate server-side rules: it simulates the
push locally and succeeds even while a rule would reject it. To test a ruleset,
push for real to a throwaway branch (and delete the ruleset **before** the branch,
since an active ruleset also blocks deleting it).

### Solo maintainer: why there is no required approval

GitHub blocks self-approval (_"Pull request authors cannot approve their own pull
requests"_) and repository owners can merge without an approval. With a single
maintainer, a required approval is therefore either a deadlock or a formality —
it can never be a review by someone else. The controls that **do** work alone are:

| Control                            | Enforced by                                                                                       |
| ---------------------------------- | ------------------------------------------------------------------------------------------------- |
| No direct pushes to `main`         | Require a pull request before merging                                                             |
| No broken code reaches `main`      | Required status checks (114 tests, lint, prod build)                                              |
| `main` only arrives from `staging` | The `branch-policy.yml` check                                                                     |
| No history rewriting               | Block force pushes                                                                                |
| No accidental branch deletion      | Restrict deletions                                                                                |
| A deliberate human pause           | The `production` environment, which **does** allow the maintainer to approve their own deployment |

**When a second maintainer joins:** add `Required approvals: 1`, enable
_Dismiss stale approvals_, and stop merging your own pull requests.

---

## 10. Styling — Angular Material (Exclusive)

### Core Rules

- **ALL** UI built with **Angular Material** components: `MatTable`, `MatFormField`,
  `MatInput`, `MatSelect`, `MatDatepicker`, `MatDialog`, `MatSnackBar`, `MatButton`,
  `MatCard`, `MatToolbar`, `MatSidenav`, `MatPaginator`, `MatIcon`.
- **NEVER** hand-roll controls that Material provides (buttons, inputs, tables).
- Custom styling ONLY via the Material theme (SCSS) in `styles/` — never scattered CSS
  files, never inline styles.
- Theme: Material Design 3 / M3 tokens, with **light and dark palettes** (both supported;
  follows system preference by default, optional manual toggle).

### Responsive & Mobile-First

- **Mobile-first**: design for small screens, then enhance with Material's
  `BreakpointObserver` (`Handset`, `Tablet`, `Web` breakpoints).
- **NEVER use fixed pixel widths** on containers — use `fxLayout`/CSS Grid/flex with
  relative units, `100%`, `max-width` + `margin: auto`.
- Tables: use `MatTable` with horizontal scroll on mobile, or card layouts for handset.
- Touch targets ≥ 48px (Material defaults enforce this — don't override smaller).

### Contrast (WCAG AA)

- **Every** component must be readable in light AND dark themes — 4.5:1 normal text,
  3:1 large text.
- Never define a light-mode color without its dark-mode counterpart.
- Focus states always visible (Material defaults); never rely on color alone.
- Forms: use `MatError`/`MatHint` with `aria-describedby` — validation never only color.

---

## 11. Internationalization (i18n) — @ngx-translate

### Core Rules

- **ALL** user-facing text comes from `@ngx-translate` — **NEVER** hardcoded strings in
  templates or components.
- Translation files: `frontend/public/i18n/{en,es,pt}.json` — **flat or nested JSON,
  same shape in every language**. (Web root is `public/` in Angular 21/Vite — never `src/assets`.)
- All three files MUST stay in sync: adding a key to one without the others is an error.
- Default language: `en`. Detection: stored language preference → `Accept-Language` →
  `en`.

```json
// en.json
{
  "nav": { "dashboard": "Dashboard", "movements": "Movements" },
  "common": { "save": "Save", "cancel": "Cancel", "loading": "Loading..." }
}
```

```typescript
// ✅ GOOD
<button mat-raised-button>{{ 'common.save' | translate }}</button>
<h1>{{ 'nav.dashboard' | translate }}</h1>

// ❌ BAD — hardcoded text
<button mat-raised-button>Save</button>
```

### What Agent MUST NOT Do

- ❌ Write user-facing text as a literal in templates/components
- ❌ Create language files with missing keys (all keys must exist in en/es/pt)
- ❌ Use `navigator.language` without falling back to `en`
- ❌ Hardcode API base URLs in components (use `environments/` + injection)

---

## 12. State Management & Optimistic Updates

**NEVER refetch data after a successful mutation — update local state directly.**

```
❌ BAD:  transfer → POST /transfers → wait → GET /movements (refetch all) → render
✅ GOOD: transfer → POST /transfers → append response to signal/list → render (instant)
✅ BEST: delete a category → remove from list instantly → DELETE /categories/:id → rollback on error
```

| Operation              | Frontend Behavior                         | Backend Requirement                   |
| ---------------------- | ----------------------------------------- | ------------------------------------- |
| **Create (POST)**      | Append returned resource to local list    | `201` + full resource                 |
| **Update (PUT/PATCH)** | Replace item with returned resource       | `200` + full resource                 |
| **Delete (DELETE)**    | Remove item immediately, rollback on fail | `200`/`204`                           |
| **Error (any)**        | Rollback to previous state + toast        | ProblemDetails with readable `detail` |

> **Ledger exception**: movements are append-only — there is NO delete/update on the ledger.
> Those rows apply to mutable resources only (categories, clients, loans, claims).

- Frontend state: **Angular Signals** (`signal`, `computed`, `update`) for component and
  simple shared state; services hold state for global data (auth, tenant).
- Refetching allowed only for: initial load, manual refresh, rollback completion,
  external changes (real-time events).

---

## 13. Security Constraints (Hard Rules)

Adapted from the previous project — **nothing relaxed**. The agent MUST enforce these on
every endpoint:

### Authentication & Tokens

- **Access token**: short-lived JWT (15–30 min) sent ONLY via
  `Authorization: Bearer <token>` — never in URL, body, or JS-readable storage.
- **Refresh token**: long-lived, **httpOnly + Secure + SameSite=Strict cookie** set by the
  backend on login. JavaScript never reads it. (This replaces the old project's
  localStorage approach — documented tech debt is now fixed.)
- **Refresh flow**: `POST /api/auth/refresh` reads the cookie, validates it, rotates the
  token (old one invalidated), returns a new access token.
- **CSRF protection** for cookie-based endpoints: `SameSite=Strict` + origin/host header
  validation on `refresh` and any cookie-authenticated mutation.
- **Never log tokens.** Never return the refresh token in JSON bodies.
- JWT contains claims: `sub`, `name`, `email`, `role`, `company_id`, `exp`. Identity data
  always extracted server-side via claims extension methods — **never from client input**.

### Authorization (RBAC)

- Roles: `Admin` (bank operator/mediator) and `User` (client). `Finance` was removed on 2026-09-02.
- Default policy: `[Authorize]` on every controller. Explicit `[Authorize(Roles = "Admin")]`
  for bank-only endpoints (approve clients, decide loans, mediate claims, audit log, categories
  management, treasury).
- **Movements are immutable**: there are NO update/delete endpoints for movements in the API —
  corrections happen by stacking a new movement (corrective transfer) after consent.
- Client (`User`) restrictions are enforced **in the service layer** (business rules:
  account ownership, balance never negative, bank never lends more than its treasury), not only
  at the HTTP layer.
- **Deny by default** — new endpoints start locked; only public ones are
  `[AllowAnonymous]` (login, register, refresh, health).

### Validation & Input

- **FluentValidation** on every write DTO (whitelist semantics: only declared fields are
  accepted). Additionally configure `System.Text.Json` with
  `UnmappedMemberHandling = Disallow` so unknown JSON fields reject the request (strict —
  equivalent of the old `forbidNonWhitelisted`).
- JSON body size limits (`MaxRequestBodySize`) and string length limits on all inputs.
- Never interpolate user input into SQL (EF Core parameterizes — never use raw SQL with
  concatenation).

### CORS & Headers

- **CORS** restricted to the exact frontend origins (dev: `http://localhost:4200`;
  prod: the Static Web Apps domain + the future subdomain of Jorge's landing page).
  **Never `AllowAnyOrigin`** in production. With credentials (refresh cookie): `AllowCredentials`
  requires explicit origins (never `*`).
- **Security headers middleware** (equivalent of the old Helmet):
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: no-referrer`
  - `Content-Security-Policy` (tight, overridable per env)
  - HSTS (`Strict-Transport-Security`) in production only

### Rate Limiting

- **ASP.NET Core built-in rate limiter** (`AddRateLimiter`) on public endpoints:
  login, register, refresh (e.g., 5/min per IP for login — same spirit as the old
  Upstash limits).
- Account lockout via Identity's `LockoutOnFailedCount` (e.g., 5 attempts → 15 min lock).

### Errors & Logging

- **Global exception handler → ProblemDetails** (RFC 7807): generic `detail`, no stack
  traces, no internal exception messages leaked to clients (stack traces dev-only).
- **Serilog** structured logging; **no `Console.WriteLine`** in committed code.
- **NEVER log** passwords, tokens, refresh tokens, or full credit-card-like data.

### Secrets

- **Agent MUST NEVER read or print real secrets**: connection strings, JWT signing keys,
  SMTP credentials, SendGrid keys.
- Local dev: **user-secrets** (`dotnet user-secrets set ...`) — never commit real values.
- `appsettings.json` committed with placeholders only; `appsettings.Development.json` may
  hold dev-only, non-sensitive values.
- Production: **Azure Key Vault** referenced from **Azure App Settings** with
  `@Microsoft.KeyVault(SecretUri=...)`. The App Setting holds **only the reference**, never
  the value — that is why managing it via CLI is safe. The agent MAY edit the _key names_
  in documentation and `.env.example`-style templates, never real values.
- **NEVER** commit `appsettings.Production.json` with real values, `.env`, or key files.

### Secret Access via Cloud CLI (HARD RULE — Azure CLI)

**The agent MUST NEVER run a command that can print the value of a secret, an application
setting, a connection string, or a Key Vault secret — in any environment and against any
cloud.** Reading a secret value is **never** required to complete a task: the agent works
exclusively with **references, names and metadata**.

**FORBIDDEN — the agent MUST NOT execute these (or the equivalent for another cloud):**

| Forbidden command                                                                            | Why                                                              |
| -------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| `az keyvault secret show ...`                                                                | fetches and prints the secret value                              |
| `az keyvault secret download ...`                                                            | same                                                             |
| `az keyvault secret set --value ...`                                                         | puts the secret in the command line → shell history + transcript |
| `az keyvault key show` / `azure keyvault certificate show --query "cer"`                     | prints key material                                              |
| `az webapp config appsettings list` **without** a `--query` limited to `.name`               | prints every setting in clear text                               |
| `az webapp config connection-string list`                                                    | prints connection strings                                        |
| `az webapp config container show`, `az containerapp ... show` (env vars)                     | prints environment variables                                     |
| `dotnet user-secrets list`, `Get-ChildItem env:`, `printenv`, `set` (bare)                   | prints secret values                                             |
| `gh auth token`                                                                              | prints the GitHub OAuth token in clear text                      |
| `gh auth status --show-token`                                                                | same — and `--show-token` is the only reason to use that flag    |
| reading the `gh` credential store (`hosts.yml`, the OS keyring entry)                        | secret material on disk                                          |
| `docker inspect` on a container holding secrets, `docker compose config`                     | prints environment variables                                     |
| reading `.env`, `appsettings.Production.json`, the user-secrets store, `.pfx`, `cookies.txt` | secret material on disk                                          |
| any `--query` / `--output` expression whose **result** can contain a secret value            | `--query` does NOT sanitize output                               |

**ALLOWED — metadata, structure, and writes that contain no secret:**

- `az keyvault secret list --query "[].name"` (names and attributes only)
- `az webapp config appsettings list --query "[].name"` or `--query "[].{name:name}"`
- `az keyvault show` / `az keyvault list` (vault metadata: SKU, RBAC mode, firewall)
- `az role assignment list --scope <vaultId> --assignee <objectId>` (permissions audit)
- `az webapp identity show` / `az webapp identity assign` (managed identity)
- `gh auth status` — account, active state and **scopes** only; the CLI masks the token itself
  (`gho_****`). This is the way to verify that `gh` is usable without reading the credential.
- `gh pr list`, `gh pr view`, `gh pr create`, `gh pr merge`, `gh run list`, `gh run watch`
- writing **Key Vault references** — `@Microsoft.KeyVault(SecretUri=...)` contains no secret
- the ARM endpoint `.../config/configreferences/appsettings` — reports only **whether** a
  reference resolved, never a value

**Verifying a secret WITHOUT ever reading it** — use one of these instead:

1. **Boolean/shape projection** (never the value itself), e.g.
   `--query "[?name=='X'].{ref:starts_with(value, '@Microsoft.KeyVault'), len:length(value)}"`.
2. **End-to-end behaviour**: call an endpoint that actually _uses_ the secret and assert the
   HTTP status. ⚠️ A `200` on `/health` is **not** proof — it may not touch the database nor
   sign a token. Prefer an endpoint that exercises both (e.g. a login endpoint).
3. **`configreferences/appsettings`** to confirm the platform resolved a reference.

**If a secret is ever exposed** (printed in the terminal, written to a log, or included in
the conversation/transcript), the agent MUST: (a) report it explicitly in the
**Security Warnings** section at the end of the session, (b) treat it as compromised and
rotate it, and (c) never repeat the pattern that caused it.

**Also forbidden:** asking the user to paste a secret into the chat, and using any tool
whose answer travels through the model (`vscode_askQuestions` included) to collect secret
values. If a secret must be stored, the agent instructs the user to type it **directly in
the Azure portal or terminal**.

**Delegated CLI access.** The agent normally authenticates nothing itself: the owner runs
the login flow, and the credential stays in the tool's own store (`az` account cache, `gh`
keyring). The agent then uses the CLI without ever reading the credential — which is why
`gh auth status` (masked) is enough to verify it works, and why reading the token is never
necessary. **Revoke when finished**, e.g. `gh auth logout --hostname github.com --user <user>`.

**Nothing sensitive in anything git keeps.** Committed history is permanent, so secrets
must never reach it through a side door: not in a commit message, not in a squash/merge
subject or body, not in a file under version control. Similarly, `AGENTS.md` and the
`README` are public-facing documents (§4): they may name resources, but never credentials.
When merging with `gh`, pass an explicit `--subject`/`--body` instead of letting the CLI
dump a long pull-request description into the permanent commit — a PR body that reads fine
on the website becomes permanent text in the repository.

### Audit Trail

- Every **create** of a movement and every **create / update / delete** of sensitive data
  (client accounts, categories, loans, claims, users) writes an `AuditLog` row: `CompanyId`,
  `UserId`, `Action`, `Entity`, `EntityId`, `Before`/`After` (JSON), `Timestamp`, `IpAddress`.
  Movements are append-only, so they only ever get an `AuditAction.Create` entry.
- Implemented as a service-level concern (explicit calls in services) or a global
  middleware — approved approach: explicit service calls + an HTTP middleware for
  request metadata (IP).
- Audit log is read-only via API and visible only to `Admin`.

### Endpoint Design

- **Never put user IDs or company IDs in URLs for your own resources.**
  Use dedicated endpoints scoped by the token: `GET /api/movements`,
  `GET /api/movements/my` style. Admin acting on a specific client may use
  `/api/admin/clients/{clientId}` — the admin's own identity still comes from the JWT.
- **Movement endpoints are POST/GET only** — no `PUT`/`DELETE` for ledger entries (§2.1).

---

## 14. Deployment Rules (Azure)

Target architecture. The concrete, deployed values for the demo are:

```
Landing (www.mallottidigital.com) ──iframe (same-site)──┐
                                                        ▼
User → https://finanzas.mallottidigital.com  (Static Web Apps, Free — SWA `swa-gestfin-mallotti`)
              ↓ HTTPS  ·  CORS allowlist (exact origins, credentials)
       https://api.mallottidigital.com  (App Service `app-gestfin-mallotti`, Linux .NET 10, B1 + Always On)
              ↓
       Azure SQL `sqldb-gestfin-prod` (Basic DTU) → RG `rg-gestfin-prod` (Sweden Central)
              ↓
       Secrets: Azure Key Vault `kv-gestfin-prod`  |  Logs: Application Insights
```

⚠️ **Frontend and backend MUST share the same registrable domain** (`finanzas.` + `api.` under
`mallottidigital.com`). The refresh token is an httpOnly cookie with **`SameSite=Strict`**:
under different registrable domains the cookie is never sent → refresh 401 → session lost.
The same rule makes the landing's **iframe embedding work**, because `www.` and `finanzas.`
are the _same site_. Never "fix" this with `SameSite=None` (third-party cookies are blocked).

**Hard rules:**

- **Never** commit secrets; CI/CD reads them from GitHub Secrets / Azure App Settings.
- **Secrets in production live in Azure Key Vault**, referenced from App Settings with
  `@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<Name>/)` —
  **versionless** (trailing slash) so rotation is automatic. The App Service reaches the
  vault through its **system-assigned managed identity** (role `Key Vault Secrets User`);
  the human operator holds `Key Vault Secrets Officer`. The agent MUST NOT read the values
  (§13 — Secret Access via Cloud CLI). Key Vault secret names allow only letters, digits
  and hyphens (`__` is invalid) → convention: app setting `A__B` ⇢ secret `A--B`.
- **Backend deploy** (GitHub Actions → App Service):
  - Build + `dotnet test` → publish → deploy slot
  - Apply migrations with the generated SQL script (or approved startup runner) **before**
    the new code goes live, or as an explicit deploy step — never interactive.
- **Frontend deploy** (GitHub Actions → Static Web Apps):
  - `npm ci` → `ng build --configuration production` → publish `dist/`
  - `environment.prod.ts` contains the public API base URL only (no secrets).
  - **`fileReplacements` is mandatory** in `angular.json` → `configurations.production`
    (`src/environments/environment.ts` ⇢ `environment.prod.ts`). Without it the production
    bundle silently keeps the dev value `apiBaseUrl: '/api'` and `environment.prod.ts` is
    **dead code** → the app calls its own host and every API request 404s.
  - `public/staticwebapp.config.json` ships with the build: `navigationFallback` → `/index.html`
    (deep links survive F5) + security headers. Its CSP pins the inline anti-FOUC script in
    `index.html` with a **sha256 hash** → changing that script requires recomputing the hash.
- **Azure SQL**:
  - Connection string lives in Key Vault, referenced from App Settings — never in code.
  - Enable Entra ID auth as an option; firewall restricted to App Service outbound IPs or
    "Allow Azure services".
  - Backups: automatic geo-redundant for Basic+; verify retention settings.
- **Domain**: frontend `https://finanzas.mallottidigital.com` (Static Web Apps custom domain,
  automatic cert); backend `https://api.mallottidigital.com` (App Service, SNI SSL). Both are
  subdomains of Jorge's registrable domain — see the diagram above for why. DNS lives in
  **Cloudflare** and every record must stay **"DNS only" (grey cloud), never "Proxied"**
  (proxying breaks domain validation and the certificate: 525/526). CORS allowlist contains
  exactly the real origins (`finanzas.`, the SWA default hostname, `http://localhost:4200`),
  never wildcards — the SWA default hostname is required because
  `AuthController.IsSameOriginRequest()` validates `Origin` against that list, so without it
  the refresh returns 403. No `X-Frame-Options: DENY` on the frontend: `frame-ancestors`
  in the CSP controls who may embed it (currently the landing).
- **Health checks**: `/health` endpoint wired to the App Service health check feature;
  `AZURE_APPINSIGHTS_KEY` optional; Serilog → Application Insights sink in prod.
- Every deploy must be reproducible: same commit → same build (lock dependencies).

### CI/CD — GitHub Actions (passwordless OIDC)

Decided with Jorge on **2026-09-16**: the deployments stop being manual and stop depending on
long-lived secrets.

| Workflow              | Trigger                                                          | What it does                                                                        |
| --------------------- | ---------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| `ci.yml`              | PR to `main`/`staging`, push to `staging`                        | `dotnet build/test/format` + `ng lint/test/build`. **No Azure access, no secrets.** |
| `branch-policy.yml`   | PR targeting `main`                                              | Fails unless the source branch is `staging` and not a fork (§9). **No secrets.**    |
| `deploy-backend.yml`  | **merge into `main`** touching `backend/**`, or manual dispatch  | build + test → publish → App Service → smoke test                                   |
| `deploy-frontend.yml` | **merge into `main`** touching `frontend/**`, or manual dispatch | `npm ci` → `ng build --configuration production` → Static Web App → smoke test      |

**Hard rules:**

- **The deploy workflows fire when a pull request is MERGED into `main`, never on the pull
  request itself.** `main` never receives direct pushes (§9), but a merge produces a `push`
  event, so `on: push: branches: [main]` is the correct trigger. Never add a `pull_request`
  trigger to a deploy workflow: it would deploy unmerged code.
- **Authentication is OIDC — never a publish profile, a deployment token or a client secret.**
  GitHub mints a short-lived token for each run; the federated credential on the Entra ID app
  registration `gh-ci-gestfin-prod` exchanges it for an Azure token. **That identity has no
  password at all.**
- The federated credentials restrict _which_ runs may impersonate the identity: only
  `repo:JorgeMallotti/gestionfinancieradocnet:ref:refs/heads/main` and
  `repo:…:environment:production`. A PR from a fork cannot use it.
- **Least privilege**: the CI identity holds exactly two resource-scoped roles —
  `Website Contributor` on `app-gestfin-mallotti` and `Contributor` on `swa-gestfin-mallotti`.
  **Never grant it a role on the resource group or the subscription.** No built-in role lists
  `Microsoft.Web/staticSites/*`, so the SWA deploy needs `Contributor` scoped to that single
  resource; narrowing it with a custom role is the documented hardening follow-up.
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` live in GitHub **Variables**,
  not Secrets: they are _identifiers_, useless without the federated trust. **No secret is stored
  in GitHub at all.**
- The `production` **GitHub Environment requires a manual approval**: a deployment does not start
  until a reviewer approves it.
- **The deploy workflows never apply migrations.** `deploy-backend.yml` generates the idempotent
  SQL script (`dotnet ef migrations script --idempotent`) and uploads it as a build artifact
  (`migrations-<sha>`); applying it remains an explicit review step (§7).
- The runner is **Linux**, so the publish zip uses forward slashes and the
  Windows/`Compress-Archive` Kudu trap cannot happen.
- The smoke test must exercise the **data path** (`POST /api/auth/demo-login`), not only
  `/health`: a 200 from `/health` proves the process started, not that the database nor the
  signing key work (§13, lesson of 2026-09-15).

---

## 15. Decision Records (Every Change Documents Its Rationale)

**Every change ships with its rationale.** The code shows _what_ was built; a decision
record explains _why_ it was built that way and what was rejected. An undocumented
trade-off is a trade-off the next developer will undo by accident.

### Format of every response containing implementation

After the code/task summary, include a section:

```
## 📋 Decision record

**What changed** — plain-language summary of the change (no jargon).

**Why this way** — the decision, plus the alternatives that were rejected and why.

**Key concepts** — 2-5 concepts behind the implementation, each with:
  - a definition in plain words
  - an everyday analogy where it helps (e.g. JWT = a stamped ticket to a theme park)

**How it maps to .NET / Angular** — how this maps to the frameworks (DI, middleware,
  EF Core pipeline, Signals, HttpClient interceptors...).

**Trade-offs and when this applies** — what the design costs, and the situations
  where it is the right (or the wrong) choice.
```

### Rules

- The record is **part of the deliverable**, not a bonus.
- Write the rationale in the repository owner's language (es/pt); technical terms
  stay in English so the vocabulary matches the code.
- Depth adapts to the change: a new concept gets the full treatment, a small
  refactor gets a short note referencing the earlier record (do not repeat).
- When the owner asks a question, answer it from first principles, then connect the
  answer to the code.
- Do not "just do" — a change without its rationale is an unfinished change.
- Target: the whole system can be explained end to end (frontend → API → services →
  EF Core → SQL → Azure) from these records alone.

---

## 16. Agent Change Protocol

The agent MUST follow this sequence for EVERY change:

```
Step 1 — Understand
├── Read the relevant files
├── Read AGENTS.md to confirm alignment
└── Identify all files that need changing

Step 2 — Plan
├── Describe the change
├── List files to create/modify/delete
└── Present the plan for approval

Step 3 — Implement
├── Create/modify files in the correct order
├── Write tests first (test-driven when possible)
└── Verify no existing tests break

Step 4 — Validate
├── Run full test suite (dotnet test / ng test)
├── Run lint & format checks
└── Run build (dotnet build / ng build)

Step 5 — Record (MANDATORY — see §15)
├── Decision record for the change
└── Answer the owner's questions

Step 6 — Document
├── Update README if needed
└── Add XML doc comments / JSDoc for public APIs
```

**The agent MUST NOT skip any step.**

---

## 17. Coding Standards

### C# (.NET 10)

- `nullable` enabled, `ImplicitUsings` enabled, `TreatWarningsAsErrors` in CI.
- PascalCase types/methods, camelCase locals/params, `_camelCase` private fields.
- Async methods: `Async` suffix + `CancellationToken` parameter.
- Records for DTOs; classes for entities/services; `sealed` by default.
- No `var` where the type is not obvious from the right side (readability first).
- No `any`-equivalent: prefer strong types, `Result<T>` over exceptions for expected paths.
- Primary constructors for DI; constructor injection only (no `ServiceLocator`).
- XML doc comments on public APIs.

### TypeScript / Angular

- Angular Style Guide: standalone, `OnPush`, Signals, `inject()`.
- `strict: true` (already default in new Angular projects) — no `any`.
- Explicit return types on service methods; interfaces for DTOs.
- Prefix components `App*`; files kebab-case; classes PascalCase.

### Imports / Naming

- Backend: `GlobalUsings.cs` per project for common namespaces; no circular project refs.
- Frontend: absolute imports via `@/` path alias (configured in `tsconfig`).
- Group imports: external → internal → type imports.

### Testing

- Every service method has a unit test; every validator has tests (valid/invalid cases).
- Every controller endpoint has an integration test via `WebApplicationFactory`
  (with Testcontainers SQL Server or the local Docker SQL).
- Test files co-located: `TransferService.cs` → `TransferServiceTests.cs`.
- xUnit + FluentAssertions; `Microsoft.AspNetCore.Mvc.Testing` for integration.

### General

- All code, comments, commits, PRs, and docs in **English** (exception: the §15
  rationale sections, which may be written in the owner's language).
- No commented-out code — delete it.
- No `Console.WriteLine` — Serilog (backend) / `console.error` only for critical errors
  (frontend).
- Backend uses `next/image`-style best practice equivalent: never inline huge blobs in
  API JSON responses without pagination — paginate everything (`PagedResult<T>`).

---

## 18. Protected Files (Agent MUST NOT Modify)

The agent MUST request explicit permission before modifying:

- `appsettings.json` / `appsettings.Production.json` with real values — **never read or
  edit real secrets**; placeholders/templates are allowed (e.g., `appsettings.example.json`)
- `user-secrets` store — never read or print
- **Azure Key Vault secrets — never read or print the value** (see §13: only names,
  metadata and references may be touched; `az keyvault secret show` is forbidden)
- `.env` / `.env.local` — never read
- `Migrations/*` — applied migration files (never edit/delete/rename)
- `package-lock.json`, `packages.lock.json` — regenerate, never hand-edit
- `.sln`, `.csproj` — only with approval
- `Program.cs` (composition root) — only with approval
- `angular.json`, `tsconfig*.json` — only with approval
- `.gitignore` — only with approval
- `frontend/public/i18n/*.json` — **keep all languages in sync**; never delete a key
  from one language without updating the others

---

## 19. Verification Checklist (Agent Self-Check)

Before finishing any task, the agent MUST verify:

- [ ] **Architecture**: Did I follow Clean Architecture? (Controllers thin, services own logic, repos only data)
- [ ] **Structure**: Are files in the correct folders (§4)?
- [ ] **Migrations**: Did I use `dotnet ef migrations add` (never `EnsureCreated`)? Committed?
- [ ] **Tenancy**: Does every query/entity respect `CompanyId` from the JWT (never client input)?
- [ ] **Git**: Am I on a `feat/*` branch from `staging`?
- [ ] **Styling**: Angular Material only — no custom controls, no inline styles, responsive + both themes?
- [ ] **i18n**: All user-facing text via `@ngx-translate`? en/es/pt in sync?
- [ ] **Optimistic Updates**: No unnecessary refetch after mutation? Backend returns full resources?
- [ ] **Security**: FluentValidation + strict JSON? CORS explicit origins? Refresh cookie httpOnly/SameSite? Rate limiting? ProblemDetails without stack traces? No secrets committed?
- [ ] **No secret exposure**: did I run any command that could print a secret value (`az keyvault secret show`, `appsettings list` without `--query "[].name"`, `gh auth token`, `user-secrets list`, env dumps)? Did I ask the user to paste a secret in the chat? Did anything sensitive leak into a commit message, a merge subject/body or a tracked file? If any secret was exposed, is it reported at the end of the session and marked for rotation? (§13)
- [ ] **Audit**: Sensitive mutations write AuditLog?
- [ ] **Tests**: Unit + integration tests for new code? Do they pass?
- [ ] **Decision record**: Did I include the "📋 Decision record" section (§15)?
- [ ] **Language**: Code/comments in English; rationale in the owner's language?
- [ ] **Build**: `dotnet build` / `ng build` + `dotnet test` / `ng test` pass clean?
