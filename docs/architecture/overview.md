# Voluntro — Architecture Overview

**Status:** agreed architecture (overview 2026-11-06, core platform grilled 2026-11-06).
Each module and milestone gets its own planning session; decisions are recorded here,
in [ADRs](../adr/) and in the [glossary](../../GLOSSARY.md).

## Context

Voluntro is a CRM-like app for churches and other volunteer-based organizations,
primarily for the Norwegian market (nb + en UI, Ukrainian later). The developer is
building it to sharpen architecture and development skills, so the design favours
explicit, compiler-enforced boundaries (Clean Architecture + DDD, modular monolith)
over shortcuts, and minimal third-party dependencies (free/OSS only).

## Key decisions

| Topic | Decision | ADR |
|---|---|---|
| Hosting | Self-hosted, one Organization per Installation, no TenantId. Future SaaS = database-per-tenant | [0002](../adr/0002-single-organization-installations.md) |
| Architecture | Modular monolith; 4 projects per module (Domain / Application / Infrastructure / Contracts) | [0001](../adr/0001-modular-monolith-four-projects-per-module.md) |
| Data access | EF Core directly, no repositories; one schema + DbContext per module | [0010](../adr/0010-ef-core-directly-no-repositories.md) |
| Failures | `Result`/`Error` with stable error codes; exceptions only for bugs | [0003](../adr/0003-result-type-for-expected-failures.md) |
| Use cases | Handlers injected directly + hand-written decorators; no mediator | [0004](../adr/0004-handlers-injected-directly-no-mediator.md) |
| Integration | Outbox/inbox per module, at-least-once, plus query interfaces in Contracts | [0005](../adr/0005-outbox-and-inbox-at-least-once-delivery.md) |
| Auth transport | ES256 JWT (10 min, in memory) + rotating refresh cookie with grace window | [0006](../adr/0006-jwt-access-tokens-with-rotating-refresh-cookie.md) |
| Authorization | Global permissions in Roles, resolved per request; resource rules in owning module | [0007](../adr/0007-global-permissions-resolved-per-request.md) |
| Identity | ASP.NET Core Identity as a generic subdomain (user type in Infrastructure) | [0008](../adr/0008-aspnet-core-identity-as-generic-subdomain.md) |
| Identity emails | Sent via integration events consumed by Communication | [0009](../adr/0009-identity-emails-sent-via-integration-events.md) |
| Who signs in | Everyone (staff, volunteers, members, parents). User ≠ Person | — |
| Module toggling | Runtime, by an Administrator; optional modules start disabled | — |
| Platform | .NET 10 LTS, C# 14, EF Core 10 + Npgsql, PostgreSQL, controllers, System.Text.Json, xUnit v3 | — |
| Dev orchestration | .NET Aspire (dev only); self-host via Docker + compose | — |
| Frontend | TanStack Start/Router/Query/Table/Form, Zod, shadcn on Base UI, Paraglide, oxlint + oxfmt | — |

## System context

```
 Browser ──► Caddy ──┬─ /      ► web  (TanStack Start: SSR public routes, client-only authed routes)
                     └─ /api/* ► api  (Voluntro.Api, modular monolith) ──► PostgreSQL (schema per module)
                                         │                              └► /data volume (keys, files)
                                         └──► Providers: Email (SMTP) / SMS / Payment
 Dev: Aspire starts Postgres, MigrationService, Api, web (Vite proxies /api), Mailpit.
```

Same origin keeps the refresh cookie first-party; CORS is never configured.

## Repository layout

```
voluntro/
├─ Voluntro.slnx  global.json (SDK 10.0.x)  .editorconfig
├─ Directory.Build.props     nullable, implicit usings, TreatWarningsAsErrors, AnalysisLevel=latest-recommended
├─ Directory.Packages.props  central package management
├─ GLOSSARY.md   docs/adr/   docs/architecture/
├─ src/
│  ├─ Host/
│  │  ├─ Voluntro.AppHost/          Aspire
│  │  ├─ Voluntro.ServiceDefaults/  OpenTelemetry, health checks (/health/live, /health/ready), resilience
│  │  ├─ Voluntro.MigrationService/ applies every module's migrations, then exits
│  │  └─ Voluntro.Api/              composition root: builder.AddModules(new PlatformModule(), …)
│  ├─ BuildingBlocks/
│  │  ├─ Voluntro.SharedKernel/                   Entity<TId>, AggregateRoot<TId>, IDomainEvent, Result/Error, ID conventions
│  │  ├─ Voluntro.BuildingBlocks.Application/     ICommandHandler/IQueryHandler, IValidator<T>, decorators, ICurrentUser, IIntegrationEvent, IFileStorage
│  │  └─ Voluntro.BuildingBlocks.Infrastructure/  ModuleDbContext base, outbox/inbox + processor, interceptors (domain events, audit), EF conventions, module filter, ProblemDetails, local file storage
│  ├─ Modules/<Module>/Voluntro.<Module>.{Domain,Application,Infrastructure,Contracts}/   (+ <Module>.http)
│  └─ Providers/Voluntro.Providers.Email.Smtp/  (later: Sms.<Vendor>, Payments.<Vendor>, BreachedPasswords.Hibp)
├─ tests/
│  ├─ Voluntro.ArchitectureTests/                hand-written reflection tests
│  └─ Modules/<Module>/ *.Domain.Tests, *.Application.Tests, *.IntegrationTests
├─ web/                                          TanStack Start app
└─ deploy/   Dockerfiles (api, web, migrations), docker-compose.yml, Caddyfile
```

## Module anatomy & dependency rules

```
            Contracts  ◄──────────── (other modules may reference ONLY this)
               ▲
 Domain ◄── Application ◄── Infrastructure ◄── Voluntro.Api / MigrationService
   │            │                 │
 SharedKernel  BB.Application    BB.Infrastructure, EF Core, ASP.NET Core
```

- **Domain**: aggregates, value objects (C# records), domain events, invariants. References SharedKernel only.
- **Application**: command/query handlers and validators. Uses the module's DbContext through a narrow interface (e.g. `IMembershipDbContext`). May reference other modules' Contracts.
- **Infrastructure**: DbContext, EF configurations, migrations, controllers, Contracts implementations, event consumers, `IModule`.
- **Contracts**: integration events, public query interfaces + DTOs, permission constants. No logic.
- Architecture tests enforce the rules that project references can't.

## SharedKernel & application conventions

- **IDs**: `readonly record struct PersonId(Guid Value)` with `New()` → `Guid.CreateVersion7()`; one EF convention registers converters for all ID types.
- **Entities**: `Entity<TId>` (identity equality), `AggregateRoot<TId>` (domain event list). Value objects are records.
- **Failures**: `Result`/`Result<T>`, `Error(Code, ErrorType, Detail?)`; `ErrorType` ∈ Validation, NotFound, Conflict, Forbidden, Unauthorized, Failure → HTTP status.
- **Validation**: domain invariants in value objects/factories (`Email.Create` → `Result<Email>`); request shape in hand-written `IValidator<TCommand>` run by `ValidationDecorator`; field errors as codes.
- **Transactions**: `TransactionDecorator` commits on success; an EF interceptor dispatches domain events, writes outbox rows and sets audit fields in the same transaction.
- **Concurrency**: Postgres `xmin` on every aggregate; `version` in DTOs and update requests; clash → 409 `common.concurrency_conflict`.
- **Auditing**: `CreatedAt/By`, `ModifiedAt/By` via `ICurrentUser`; background work acts as `System`. Full change log later, for sensitive modules only.
- **Time**: `TimeProvider`; `timestamptz` (UTC); Organization's time zone applied at the edges.
- **Background jobs**: hand-written `BackgroundService` + `PeriodicTimer` (outbox, token cleanup, retention).
- **Files**: `IFileStorage`, local disk under `/data/files` first; S3-compatible later.
- **Ops**: options `ValidateOnStart`; no personal data in logs (IDs only); `traceId` on every response.

## API conventions

- Routes `/api/{module}/{resource}`; JSON camelCase, enums as strings, nulls omitted.
- Lists: `?page=1&pageSize=25&sort=lastName:asc&search=…` → `{ items, page, pageSize, totalCount }`.
- Errors: RFC 9457 ProblemDetails with `code`, `traceId`, and `errors: { field: [codes] }` for validation.
- No versioning until needed.
- Built-in `Microsoft.AspNetCore.OpenApi` document; Swagger UI (Swashbuckle `SwaggerUI`) in Development only; `.http` files per module are the versioned way to exercise endpoints.

## Module system

```csharp
public interface IModule
{
    string Key { get; }                    // "checkin"
    bool IsCore { get; }
    IReadOnlyList<string> DependsOn { get; }
    void Register(IServiceCollection services, IConfiguration config);
    IEnumerable<Permission> Permissions { get; }
}
```

- Registered explicitly in `Program.cs`; controllers via `AddApplicationPart`.
- Controllers carry `[Module("key")]`; a filter returns **404** when the module is disabled (in-memory activation cache, refreshed on toggle).
- Disabled modules keep their data, keep consuming integration events, and their permissions are hidden (not removed) in the role editor.
- Enabling checks `DependsOn`; disabling a module others depend on → 409 `platform.module.required_by`.
- `GET /api/platform/modules` → `[{ key, enabled, isCore, dependsOn }]` drives frontend navigation and route guards.

## Module map

| Module | Kind | Owns | Depends on (via Contracts) |
|---|---|---|---|
| **Platform** | core | `Organization` (profile + localization), `ModuleActivation`, Setup | — |
| **Identity** | core | Users, Sign-in methods, 2FA, Sessions, Roles, Invitations | Platform |
| **Membership** | core | People, households, guardian/child relationships, membership status, consents | Identity |
| **Communication** | core | Email sending (SMS later), templates per locale, delivery log (later) | — |
| **Payments** | optional | Payment intents, provider callbacks, refunds | — |
| Children's check-in | optional | Check-in sessions, check-ins, labels/pickup codes | Membership, Calendar |
| Small groups | optional | Groups, leaders, members, meetings | Membership |
| Calendar | optional | Events, recurrence | — |
| Tickets | optional | Ticket types, orders, tickets | Calendar, Payments, Membership |
| Room booking | optional | Rooms, resources, bookings | Calendar |
| Newsletter | optional | Lists, subscriptions, campaigns | Communication, Membership |

## Data & integration

- One PostgreSQL database; one schema + DbContext + migrations history per module; no cross-schema FKs.
- Migrations applied by `Voluntro.MigrationService` only (Aspire `WaitForCompletion`; compose init step).
- Per-module `outbox`/`inbox` tables; generic `OutboxProcessor<TDbContext>`; polling ~1 s; System.Text.Json with a type-name registry (`identity.user_registered.v1`); failed after N attempts; processed rows deleted after ~7 days.
- **Microservice path**: outbox → broker, query interfaces → HTTP/gRPC, schema → own database. Contracts don't change.

## Platform module

- **Organization**: name, short name, organisasjonsnummer (optional), contact email/phone, address, website, logo; default locale (`nb`), enabled locales, time zone (`Europe/Oslo`), country (`NO`), currency (`NOK`).
- **ModuleActivation**: one row per optional module (enabled, changed by/at). Optional modules start disabled.
- **Setup**: while no Users exist, `/api/platform/setup` is open, guarded by a setup token printed to the log at startup. One command creates the Organization, the first Administrator (email treated as confirmed) and enables chosen modules, then signs in. The endpoint closes permanently.

## Identity & authentication

- ASP.NET Core Identity (EF stores, `identity` schema); user type in Infrastructure. A User holds Email (= username, unique, case-insensitive), `DisplayName`, `PreferredLocale`. All other personal data belongs to the Person in Membership.
- **Accounts**: Invitations by staff; Self-registration configurable, **off by default**. Self-registered Users must confirm their email before signing in. `UserRegistered` carries sign-up names for Membership.
- **Roles**: built-in Administrator (all permissions, immutable, ≥1 active) and a built-in default role for new Users (name TBD — see glossary); custom Roles; union of permissions.
- **Permissions**: `{module}.{resource}.{action}`, constants in each module's Contracts, exposed via `IModule.Permissions`, catalogued by Identity at startup; `[HasPermission(...)]` via a dynamic policy provider; resolved per request through cached `IPermissionProvider` keyed by user + `perm_version`.
- **Passwords**: NIST 800-63B — 12–128 chars, no composition rules; lockout 5 attempts / 15 min; built-in rate limiter on login, refresh, setup; optional pluggable breached-password check (off by default).
- **Tokens**: ES256 JWT, 10 min, `kid` + JWKS (current + previous key); refresh 14 days sliding, 90-day absolute cap per family, rotation with reuse detection and 10 s grace window; refresh cookie scoped to `/api/identity/refresh`.
- **Keys**: JWT signing keys and Data Protection key ring generated on first start in `/data/keys` (or supplied via config).
- **Flows**: password → (if 2FA) `{ requiresTwoFactor, challengeToken }` (5 min) → TOTP or recovery code → tokens. Passkey → tokens (counts as MFA). Passkey-only accounts allowed; a User never removes their last Sign-in method.
- **2FA**: optional TOTP with 10 single-use recovery codes; org setting (off by default) to require 2FA for Administrators; `identity.users.reset_mfa` lets an admin reset another User's 2FA and passkeys (audited, User notified).
- **Token lifetimes**: email confirmation & invitation 7 days, password reset 1 hour. Reset always returns 202.
- **Sessions**: Users list and revoke Sessions; password change/reset and MFA reset revoke all.
- **User ↔ Person**: Membership consumes `UserRegistered` to link or create a Person. Users created before Membership exists are backfilled when Membership is built.

## Communication (core sending)

- `IEmailSender` with SMTP provider (MailKit); Mailpit in dev. Sender address from provider config, sender name = Organization name.
- Identity publishes `UserInvited`, `EmailConfirmationRequested`, `PasswordResetRequested`, `TwoFactorReset`; Communication consumes, renders, sends.
- Templates: Fluid (Liquid), embedded per locale (`identity.password_reset.nb.liquid`); DB overrides editable by admins later.
- Recipient locale: User's `PreferredLocale` → Organization default.

## Internationalization

- UI `nb` (default) + `en`; `uk` later as another message file.
- API returns codes, never translated text; frontend translates (errors, permissions, enums).
- Locale resolution: signed-in User's `PreferredLocale` → locale cookie → `Accept-Language` → Organization default. No locale prefix in URLs (may be added for public SEO pages later).

## Pluggable providers

- Abstractions in the owning module's Application layer (`IEmailSender`, `ISmsSender`, `IPaymentProvider`, `IBreachedPasswordChecker`).
- One project per vendor under `src/Providers/`, selected by config (`Providers:Email:Type = "Smtp"`); secrets from env/user-secrets.
- Sends go through the outbox, so provider outages only delay messages.

## Frontend architecture

```
web/src/
├─ routes/            thin file-based routes; _public (SSR) and _authed (ssr: false) layouts
├─ modules/<module>/  api hooks, Zod form schemas, query-key factories, components
├─ shared/            ui (shadcn/Base UI), api core, auth, i18n (Paraglide), theme
```

- **API**: `openapi-typescript` (dev dependency) generates types from the OpenAPI document; `openapi-fetch` typed client; Zod only for forms and other untrusted input.
- **Auth**: access token in memory; on boot the authed layout calls `/api/identity/refresh`; refresh serialized across tabs with Web Locks, new token shared via BroadcastChannel; single-flight refresh on 401.
- **Passkeys**: native WebAuthn JSON APIs (`parseCreationOptionsFromJSON`, `parseRequestOptionsFromJSON`, `toJSON()`), no library.
- **Forms**: TanStack Form + Zod + shadcn `Field` components; server validation codes mapped to fields.
- **Theme & a11y**: light/dark/system; target WCAG 2.1 AA.
- **Tooling**: pnpm; oxlint (TanStack Query plugin via `jsPlugins`, alpha; Router plugin if it loads) + oxfmt; Vitest + Testing Library; Playwright E2E with virtual WebAuthn authenticator.
- **Dev**: Vite proxies `/api` to the Aspire-started API (same origin as production).

## Cross-cutting concerns

- **GDPR** (children's data, Datatilsynet): consent records, audit log, data export, deletion/anonymization, retention — designed into Membership and Check-in.
- **Observability**: `ILogger` + OpenTelemetry via ServiceDefaults; Aspire dashboard in dev; `traceId` in ProblemDetails.
- **Testing**: domain unit tests; handler tests; integration tests via `WebApplicationFactory` + Testcontainers.PostgreSql (real Postgres per collection, reset between tests); one Aspire.Hosting.Testing smoke test; hand-written architecture tests; Playwright for auth flows.
- **Deployment (self-host)**: Dockerfiles for `api`, `web`, `migrations`; `docker-compose.yml` with Postgres, `/data` volume, Caddy.

## Roadmap

**Milestone 1 — Core platform** (in order):
1. Solution skeleton, Aspire AppHost/ServiceDefaults, MigrationService, BuildingBlocks (SharedKernel, decorators, outbox/inbox, interceptors), architecture tests, integration-test harness
2. Platform module: Organization, ModuleActivation, module filter, Setup
3. Identity: password sign-in, JWT + refresh rotation, Sessions, Roles & Permissions, Invitations, Self-registration
4. Communication core: SMTP/MailKit, Fluid templates, consumers for Identity events
5. Frontend shell: i18n, auth client, Setup wizard, sign-in, invitation/reset pages, org settings & modules admin, own security page
6. Identity: TOTP 2FA + recovery codes, then passkeys (with Playwright tests)
7. Docker images, compose, Caddy

**Afterwards** (one planning session each): Membership (incl. User backfill) → Small groups → Calendar → Room booking → Check-in → Payments + Tickets → Newsletter.

## Open questions

- Name of the built-in default Role (must not be "Member" — see glossary)
- NodaTime for Calendar recurrence
- Which SMS and payment vendors to support first
- Scope of the full change log for sensitive modules (Membership, Check-in)
