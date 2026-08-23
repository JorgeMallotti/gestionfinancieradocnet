# Gestión Financiera Interna — Agent Instructions

Monorepo: **.NET 10 Web API + Angular 21 + SQL Server + Azure**.

> This file adapts the security best practices from the previous project's AGENTS.md
> (NestJS/Next.js) to the new stack. Nothing from the old security rules was dropped —
> each rule was rewritten for .NET/Angular. New sections: Multi-Tenancy, Azure Deployment,
> and Academic Explanation (Jorge learns while building).

## Table of Contents

| Sec | Title                                                                                               |
| --- | --------------------------------------------------------------------------------------------------- |
| 1   | [Mandatory Reads Before Any Change](#1-mandatory-reads-before-any-change)                           |
| 2   | [Stack & Key Decisions (LOCKED)](#2-stack--key-decisions-locked)                                    |
| 3   | [Architecture Rules (Clean Architecture)](#3-architecture-rules-clean-architecture)                 |
| 4   | [Project Structure (Strictly Enforced)](#4-project-structure-strictly-enforced)                     |
| 5   | [Module Convention (.NET Backend)](#5-module-convention-net-backend)                                |
| 6   | [Module Convention (Angular Frontend)](#6-module-convention-angular-frontend)                       |
| 7   | [Database Migration Rules (EF Core — Strict)](#7-database-migration-rules-ef-core--strict)          |
| 8   | [Multi-Tenancy Rules (Multi-Company)](#8-multi-tenancy-rules-multi-company)                         |
| 9   | [Git Workflow: feat/\* → staging → main](#9-git-workflow-feat--staging--main)                       |
| 10  | [Styling — Angular Material (Exclusive)](#10-styling--angular-material-exclusive)                   |
| 11  | [Internationalization (i18n) — @ngx-translate](#11-internationalization-i18n--ngx-translate)        |
| 12  | [State Management & Optimistic Updates](#12-state-management--optimistic-updates)                   |
| 13  | [Security Constraints (Hard Rules)](#13-security-constraints-hard-rules)                            |
| 14  | [Deployment Rules (Azure)](#14-deployment-rules-azure)                                              |
| 15  | [Academic Explanation (Mandatory — Jorge Learns)](#15-academic-explanation-mandatory--jorge-learns) |
| 16  | [Agent Change Protocol](#16-agent-change-protocol)                                                  |
| 17  | [Coding Standards](#17-coding-standards)                                                            |
| 18  | [Protected Files (Agent MUST NOT Modify)](#18-protected-files-agent-must-not-modify)                |
| 19  | [Verification Checklist (Agent Self-Check)](#19-verification-checklist-agent-self-check)            |

---

## 1. Mandatory Reads Before Any Change

Before writing a single line of code, read:

- This file (`AGENTS.md`)
- If touching the database: the EF Core `DbContext` and the `Migrations/` folder
- If creating frontend components: existing patterns in `frontend/src/app/`
- If modifying an existing backend feature: the full feature folder
- This file again, especially §15 (Academic Explanation) — every change is also a lesson

---

## 2. Stack & Key Decisions (LOCKED)

Decisions agreed with Jorge on 2026-08-23. **Do not change without explicit approval.**

| Decision          | Choice                                                                                               | Why                                                              |
| ----------------- | ---------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| Backend           | **.NET 10 Web API** (modern LTS)                                                                     | Employability in Portugal; modern, supported, corporate standard |
| Frontend          | **Angular 21** (standalone components) + **Angular Material**                                        | Corporate standard for .NET shops; ready components              |
| Database          | **SQL Server** — Docker `mcr.microsoft.com/mssql/server` locally, **Azure SQL** in prod              | Same engine dev/prod (no drift)                                  |
| Architecture      | **Clean Architecture** (Domain / Application / Infrastructure / Api)                                 | Most requested pattern in .NET job interviews                    |
| Backend patterns  | **Repositories + FluentValidation + Result pattern** (+ AutoMapper optional)                         | "Corporate level" — what real companies ask for                  |
| Auth              | **ASP.NET Core Identity + JWT (access) + refresh token in httpOnly cookie**                          | Secure; fixes the localStorage token debt of the old project     |
| Roles             | `Admin`, `Finance`, `User` (per company)                                                             | Spec                                                             |
| Multi-tenancy     | **Full multi-company in the MVP** — `CompanyId` on all business tables, EF Core global query filters | Spec, avoids painful migrations later                            |
| PDF               | **QuestPDF**                                                                                         | Free for this use case, de-facto standard in .NET                |
| Excel export      | **ClosedXML**                                                                                        | Free (MIT), standard                                             |
| Email             | **MailKit** (SMTP) or SendGrid SDK                                                                   | MailKit is the modern standard (SmtpClient is legacy)            |
| Validation        | **FluentValidation**                                                                                 | Seen in almost every .NET job offer                              |
| Logging           | **Serilog** + Application Insights (Azure)                                                           | Structured logging, corporate standard                           |
| API docs          | **Swagger / OpenAPI**                                                                                | Free documentation, testable endpoints                           |
| Error contract    | **ProblemDetails** (RFC 7807)                                                                        | Standard ASP.NET Core format                                     |
| Tests             | **xUnit + FluentAssertions** (unit), **WebApplicationFactory** (integration)                         | Corporate standard                                               |
| CI/CD             | **GitHub Actions** (backend → App Service, frontend → Static Web Apps)                               | Free, standard                                                   |
| Frontend state    | **Angular Signals + inject-based services**                                                          | Modern Angular idiom (stable since 17, mature in 21)             |
| i18n              | **@ngx-translate**, files `en.json` / `es.json` / `pt.json`                                          | Community standard, same pattern as old project                  |
| Language of code  | English everywhere (code, comments, commits, docs)                                                   | Global standard                                                  |
| Conversation lang | Academic explanations to Jorge in **his language (es/pt)** — technical terms kept in English         | §15                                                              |

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
│   │   │   └── {Feature}/          # e.g. Transactions, Categories, Auth, Dashboard, Reports
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
│   │   ├── Entities/               # Company, User, Category, Transaction, AuditLog...
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
└── src/
    ├── app/
    │   ├── core/                    # Singleton services: auth, tenant, token refresh, API client, guards, interceptors
    │   ├── features/                # Lazy-loaded feature folders
    │   │   ├── auth/                # Login, register (company signup)
    │   │   ├── dashboard/           # Charts, KPI cards
    │   │   ├── transactions/        # CRUD + list
    │   │   ├── categories/          # CRUD
    │   │   ├── reports/             # PDF / Excel export UI
    │   │   ├── audit/               # Audit log viewer (Admin)
    │   │   └── admin/               # User management per company (Admin)
    │   ├── shared/                  # Reusable components, pipes, directives, Material config
    │   ├── app.routes.ts            # Routes with lazy loading + guards
    │   ├── app.config.ts            # Providers (HTTP, translate, material)
    │   └── app.component.ts
    ├── assets/i18n/                 # en.json, es.json, pt.json — @ngx-translate
    ├── environments/                # environment.ts / environment.prod.ts (API base URL)
    └── styles/                      # Material theme (light/dark), global SCSS
```

**The agent MUST NOT create files outside this structure.**

---

## 5. Module Convention (.NET Backend)

Every feature follows the same pattern (Contracts → Validation → Service → Controller):

```csharp
// 1. DTO — contract (record, immutable)
public sealed record CreateTransactionDto(
    Guid CategoryId,
    decimal Amount,
    string Currency,
    DateTimeOffset Date,
    string? Description);

// 2. FluentValidation — defines the API contract rules
public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionDto>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be positive.");
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

// 3. Service — ALL business logic, returns Result<T>
public interface ITransactionService
{
    Task<Result<TransactionDto>> CreateAsync(
        CreateTransactionDto dto, Guid companyId, Guid userId, CancellationToken ct);
}

public sealed class TransactionService(
    ITransactionRepository repository,
    ICategoryRepository categories) : ITransactionService
{
    public async Task<Result<TransactionDto>> CreateAsync(
        CreateTransactionDto dto, Guid companyId, Guid userId, CancellationToken ct)
    {
        // Validate → business rules → persist → return full resource
        var category = await categories.GetByIdAsync(dto.CategoryId, companyId, ct);
        if (category is null)
            return Result<TransactionDto>.Failure("Category does not exist.");

        var entity = Transaction.Create(companyId, userId, dto.CategoryId, dto.Amount, dto.Date);
        var saved = await repository.AddAsync(entity, ct);
        return Result<TransactionDto>.Success(TransactionDto.FromEntity(saved));
    }
}

// 4. Controller — thin, delegates to service
[ApiController]
[Route("api/transactions")]
[Authorize]
public sealed class TransactionsController(ITransactionService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetById(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId(); // extension over JWT claims
        var result = await service.GetByIdAsync(id, companyId, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(
        CreateTransactionDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.CreateAsync(dto, companyId, userId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : BadRequest(new ProblemDetails { Detail = result.Error });
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
features/transactions/
├── transactions.routes.ts        # Lazy routes
├── pages/
│   ├── transaction-list.page.ts  # MatTable + filters + pagination
│   └── transaction-form.page.ts  # Reactive form (MatFormField), create/edit
└── services/
    └── transactions.service.ts   # Typed HTTP calls to the API client
```

- **Data flows one way**: components → services (injectable) → `HttpClient` → API.
- Components contain **zero business logic** — only presentation and event handling.
- Typed responses: define `TransactionDto` interfaces in `app/shared/models/` mirroring
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

## 8. Multi-Tenancy Rules (Multi-Company)

This is a **multi-tenant** system: every business record belongs to a company.

**Hard rules:**

- Every business entity carries `CompanyId` (`UNIQUEIDENTIFIER`, non-nullable):
  `Category`, `Transaction`, `AuditLog`, `Report`... `User` belongs to exactly one company.
- **Tenant resolution**: `CompanyId` comes ONLY from the JWT claim (`company_id`) —
  **never** from URL, query string, or request body.
- **EF Core global query filter** on every business entity enforces isolation
  automatically — a query can never leak another company's rows:

```csharp
modelBuilder.Entity<Transaction>()
    .HasQueryFilter(t => t.CompanyId == _tenantProvider.CompanyId);
```

- Repositories receive `companyId` as a parameter and include it in every `Where`.
- Cross-tenant access attempts must return `404` (not `403`) to avoid leaking existence.
- `Admin` manages users **within their own company only**.
- A new company is created on registration (first user becomes its `Admin`).
- Creating a company is the ONLY place a `CompanyId` may be minted from the client
  (via the signup flow) — all other flows derive it from the token.

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

- **NEVER** commit directly to `main` or `staging`.
- **NEVER** merge your own PRs — always request a review.
- Branch naming: `feat/add-auth-module`, `fix/login-validation-error`,
  `refactor/transactions-service`, `chore/update-dependencies`, `docs/api-readme`.
- Commits follow [Conventional Commits](https://www.conventionalcommits.org/):
  `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`, `style:`.
- Before opening a PR to `staging`:
  - `dotnet test` (backend) and `ng test` (frontend) pass
  - `dotnet build` and `ng build` clean
  - `dotnet format --verify-no-changes` and `ng lint` clean
  - Branch rebased onto latest `staging` (`git rebase staging`)
- PR to `main` only after `staging` validated (tests + manual QA).
- Protected branches on GitHub: require PR + 1 approval, dismiss stale approvals,
  status checks (CI), linear history on `main`.

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
- Translation files: `frontend/src/assets/i18n/{en,es,pt}.json` — **flat or nested JSON,
  same shape in every language**.
- All three files MUST stay in sync: adding a key to one without the others is an error.
- Default language: `en`. Detection: stored language preference → `Accept-Language` →
  `en`.

```json
// en.json
{
  "nav": { "dashboard": "Dashboard", "transactions": "Transactions" },
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
❌ BAD:  create → POST /transactions → wait → GET /transactions (refetch all) → render
✅ GOOD: create → POST /transactions → append response to signal/list → render (instant)
✅ BEST: delete → remove from list instantly → DELETE /transactions/:id → rollback on error
```

| Operation              | Frontend Behavior                         | Backend Requirement                   |
| ---------------------- | ----------------------------------------- | ------------------------------------- |
| **Create (POST)**      | Append returned resource to local list    | `201` + full resource                 |
| **Update (PUT/PATCH)** | Replace item with returned resource       | `200` + full resource                 |
| **Delete (DELETE)**    | Remove item immediately, rollback on fail | `200`/`204`                           |
| **Error (any)**        | Rollback to previous state + toast        | ProblemDetails with readable `detail` |

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

- Roles: `Admin`, `Finance`, `User` (per company).
- Default policy: `[Authorize]` on every controller. Explicit `[Authorize(Roles = "Admin")]`
  for admin-only endpoints (user management, audit log, company settings).
- `Finance` and `User` restrictions are enforced **in the service layer** (business rules),
  not only at the HTTP layer.
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
- Production: **Azure App Settings** (or Key Vault) — the agent MAY edit the _key names_
  in documentation and `.env.example`-style templates, never real values.
- **NEVER** commit `appsettings.Production.json` with real values, `.env`, or key files.

### Audit Trail

- Every **create / update / delete** of sensitive data (transactions, categories, users,
  roles) writes an `AuditLog` row: `CompanyId`, `UserId`, `Action`, `Entity`, `EntityId`,
  `Before`/`After` (JSON), `Timestamp`, `IpAddress`.
- Implemented as a service-level concern (explicit calls in services) or a global
  middleware — approved approach: explicit service calls + an HTTP middleware for
  request metadata (IP).
- Audit log is read-only via API and visible only to `Admin`.

### Endpoint Design

- **Never put user IDs or company IDs in URLs for your own resources.**
  Use dedicated endpoints scoped by the token: `GET /api/transactions`,
  `GET /api/transactions/my` style. Admin acting on a specific user may use
  `/api/admin/users/{userId}` — the admin's own identity still comes from the JWT.

---

## 14. Deployment Rules (Azure)

Target architecture (Jorge deploys; the agent guides and prepares everything):

```
User → https://finanzas.<jorge-domain> (Static Web Apps / frontend)
              ↓ HTTPS
       Backend: Azure App Service (Windows or Linux) → Azure SQL
              ↓
       Secrets: App Settings / Key Vault  |  Logs: Application Insights
```

**Hard rules:**

- **Never** commit secrets; CI/CD reads them from GitHub Secrets / Azure App Settings.
- **Backend deploy** (GitHub Actions → App Service):
  - Build + `dotnet test` → publish → deploy slot
  - Apply migrations with the generated SQL script (or approved startup runner) **before**
    the new code goes live, or as an explicit deploy step — never interactive.
- **Frontend deploy** (GitHub Actions → Static Web Apps):
  - `npm ci` → `ng build --configuration production` → publish `dist/`
  - `environment.prod.ts` contains the public API base URL only (no secrets).
- **Azure SQL**:
  - Connection string lives in App Settings (`AZURE_SQL_CONNECTIONSTRING`) — never in code.
  - Enable Entra ID auth as an option; firewall restricted to App Service outbound IPs or
    "Allow Azure services".
  - Backups: automatic geo-redundant for Basic+; verify retention settings.
- **Domain**: the frontend gets a **subdomain** of Jorge's existing domain; HTTPS via
  Static Web Apps custom domains (automatic cert). Backend gets its own
  `api.<subdomain>` or uses the App Service default domain — CORS allowlist must contain
  exactly the real origins, no wildcards.
- **Health checks**: `/health` endpoint wired to the App Service health check feature;
  `AZURE_APPINSIGHTS_KEY` optional; Serilog → Application Insights sink in prod.
- Every deploy must be reproducible: same commit → same build (lock dependencies).

---

## 15. Academic Explanation (Mandatory — Jorge Learns)

**Every implementation the agent delivers MUST be accompanied by an academic explanation
so Jorge learns the .NET/Angular stack while building the MVP — and can answer interview
questions confidently.**

### Format of every response containing implementation

After the code/task summary, include a section:

```
## 📚 Aprende con esto (academic explanation)

**¿Qué hicimos?** — plain-language summary of the change (no jargon).

**¿Por qué así?** — the decision and the alternatives we rejected (and why).

**Conceptos clave** — 2-5 concepts behind the implementation, each with:
  - definition in simple words
  - an analogy from daily life (e.g., JWT = a stamped ticket to a theme park)

**Conexión con .NET / Angular** — how this maps to frameworks (DI, middleware,
  EF Core pipeline, Signals, HttpClient interceptors...).

**Posibles preguntas de entrevista** — 2-4 likely questions + how to answer
  (answer skeleton, not a script to memorize).
```

### Rules

- The explanation is **mandatory**, not optional — it is part of the deliverable.
- Written in **Jorge's language (es/pt)**; technical terms stay in English
  (e.g., "dependency injection" stays as-is) so he learns the vocabulary used in
  job interviews.
- Academic depth adapts to the topic: new concepts get the full treatment; small
  refactors get a brief note referencing past lessons (do not repeat lessons).
- When Jorge asks a question, answer it academically first, then connect it to the code.
- The agent MUST NOT "just do" — every change is a teaching moment. If in doubt, explain.
- Jorge's goal: after the MVP, he can explain the whole system end-to-end
  (frontend → API → services → EF Core → SQL → Azure) in an interview.

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

Step 5 — Explain (MANDATORY — see §15)
├── Academic explanation of the change
└── Answer Jorge's questions

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
- Test files co-located: `TransactionService.cs` → `TransactionServiceTests.cs`.
- xUnit + FluentAssertions; `Microsoft.AspNetCore.Mvc.Testing` for integration.

### General

- All code, comments, commits, PRs, and docs in **English** (exceptions: §15 academic
  explanations to Jorge in his language).
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
- `.env` / `.env.local` — never read
- `Migrations/*` — applied migration files (never edit/delete/rename)
- `package-lock.json`, `packages.lock.json` — regenerate, never hand-edit
- `.sln`, `.csproj` — only with approval
- `Program.cs` (composition root) — only with approval
- `angular.json`, `tsconfig*.json` — only with approval
- `.gitignore` — only with approval
- `frontend/src/assets/i18n/*.json` — **keep all languages in sync**; never delete a key
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
- [ ] **Audit**: Sensitive mutations write AuditLog?
- [ ] **Tests**: Unit + integration tests for new code? Do they pass?
- [ ] **Academic**: Did I include the "📚 Aprende con esto" section (§15)?
- [ ] **Language**: Code/comments in English; explanation to Jorge in es/pt?
- [ ] **Build**: `dotnet build` / `ng build` + `dotnet test` / `ng test` pass clean?
