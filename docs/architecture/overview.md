# Voluntro — Architecture Overview

**Status:** agreed architecture (overview 2026-11-06, core platform grilled 2026-11-06, module system & API conventions grilled 2026-11-12).
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
Caddy terminates TLS (HTTPS redirects, HSTS); the API serves plain HTTP behind it and has no `UseHttpsRedirection`.

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
│  │  ├─ Voluntro.SharedKernel/                   IEntityId<TSelf>, Entity<TId>, AggregateRoot<TId>, IHasDomainEvents, IDomainEvent, Result/Result<T>, Error/ErrorType
│  │  ├─ Voluntro.BuildingBlocks.Contracts/       PageRequest, PagedList<T>; later IIntegrationEvent, Permission (depends on SharedKernel only)
│  │  ├─ Voluntro.BuildingBlocks.Application/     ICommandHandler/IQueryHandler, IValidator<T>, decorators, ICurrentUser, IFileStorage
│  │  └─ Voluntro.BuildingBlocks.Infrastructure/  IModule + AddModule, module gate, ApiController, ProblemDetails, ModuleDbContext base, outbox/inbox + processor, interceptors (domain events, audit), EF conventions, local file storage
│  ├─ Modules/<Module>/Voluntro.<Module>.{Domain,Application,Infrastructure,Contracts}/   (+ <Module>.http)
│  └─ Providers/Voluntro.Providers.Email.Smtp/  (later: Sms.<Vendor>, Payments.<Vendor>, BreachedPasswords.Hibp)
├─ tests/
│  ├─ Voluntro.AppHost.Tests/                    Aspire.Hosting.Testing smoke test (needs Docker)
│  ├─ Voluntro.ArchitectureTests/                hand-written reflection tests (see Architecture tests below)
│  ├─ Voluntro.Api.IntegrationTests/             WebApplicationFactory harness + test-only sample module (module system, errors, conventions)
│  ├─ BuildingBlocks/Voluntro.SharedKernel.Tests/ unit tests for the primitives (plain xUnit Assert)
│  └─ Modules/<Module>/ *.Domain.Tests, *.Application.Tests, *.IntegrationTests
├─ web/                                          TanStack Start app
└─ deploy/   Dockerfiles (api, web, migrations), docker-compose.yml, Caddyfile
```

## Module anatomy & dependency rules

```
            Contracts  ◄──────────── (other modules may reference ONLY this)
               ▲    │
               │   SharedKernel, BB.Contracts
               │
 Domain ◄── Application ◄── Infrastructure ◄── Voluntro.Api / MigrationService
   │            │                 │
 SharedKernel  BB.Application    BB.Infrastructure, EF Core, ASP.NET Core
```

- **Domain**: aggregates, value objects (C# records), domain events, invariants. References SharedKernel only; no packages.
- **Application**: command/query handlers and validators. Uses the module's DbContext through a narrow interface (e.g. `IMembershipDbContext`). May reference other modules' Contracts. Never references BuildingBlocks.Infrastructure.
- **Infrastructure**: DbContext, EF configurations, migrations, controllers, Contracts implementations, event consumers, `IModule`.
- **Contracts**: integration events, public query interfaces + DTOs, permission constants. No logic. References SharedKernel and BuildingBlocks.Contracts only; no packages, and **no other module's Contracts** (Contracts are leaves, so cross-module DTOs use `Guid`, not another module's ID types).
- **BuildingBlocks** never reference a module. BuildingBlocks.Contracts and BuildingBlocks.Application never reference BuildingBlocks.Infrastructure.
- In the diagram above and the tables below, **BB** is short for **BuildingBlocks** (`BB.Contracts` = `Voluntro.BuildingBlocks.Contracts`).
- Architecture tests enforce the rules that project references can't.

### Architecture tests

Allowed Voluntro references for module X (Y = any other module; ✅ allowed, ❌ forbidden):

| From ↓ / To → | SharedKernel | BB.Contracts | BB.Application | BB.Infrastructure | X.Domain | X.Application | X.Infrastructure | X.Contracts | Y.Contracts | Y.(other) |
|---|---|---|---|---|---|---|---|---|---|---|
| X.Domain | ✅ | ❌ | ❌ | ❌ | — | ❌ | ❌ | ❌ | ❌ | ❌ |
| X.Contracts | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | — | ❌ | ❌ |
| X.Application | ✅ | ✅ | ✅ | ❌ | ✅ | — | ❌ | ✅ | ✅ | ❌ |
| X.Infrastructure | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | — | ✅ | ✅ | ❌ |

- **Mechanism**: the assembly references recorded in each DLL's metadata (ADR-0001), read from the file without loading it. The compiler only records references whose types are used, so the rules govern *used* dependencies; an unused `ProjectReference` passes.
- **Known blind spot**: a dependency that is only a compile-time constant leaves no reference. `const` values and `nameof(...)` are inlined by the compiler, so an assembly that uses nothing from another assembly except e.g. a permission constant (`const string` in a module's Contracts) passes every rule. As soon as it also uses a type, method or `static readonly` field from that assembly, the reference is recorded and the rules apply. Accepted for now; closing it would need source-level analysis (a Roslyn analyzer) instead of metadata.
- **Discovery**: the test project globs `ProjectReference`s to `src/BuildingBlocks/**` and `src/Modules/**` and scans the `Voluntro.*` assemblies listed in its own `deps.json`. A module is guarded as soon as it exists, before it is wired into the Api. The output folder itself is not scanned, so a DLL left behind by a renamed or deleted project (or another branch) is ignored.
- **Naming**: a module assembly is exactly `Voluntro.{Module}.{Domain|Application|Infrastructure|Contracts}` with a single-segment PascalCase module name; `SharedKernel`, `BuildingBlocks`, `Providers`, `Api`, `AppHost` and `ServiceDefaults` are reserved and never a module name. Any other scanned `Voluntro.*` assembly fails the build as unclassified.
- **Packages**: SharedKernel, BuildingBlocks.Contracts, Domain and Contracts may reference only the .NET shared framework (BuildingBlocks.Contracts included because every module's Contracts depends on it). Exceptions (e.g. NodaTime) get an explicit allow-list when the first one arrives. Application and Infrastructure are unrestricted.
- **Structure**: loading (assembly → referenced names) is separate from pure classification and rule functions. The rules are unit-tested on made-up reference graphs, so every rule is proven to fire even before any module exists. One `[Fact]` per rule against the real graph, failing with all `"A → B"` violations; a sanity test fails if any project on disk under `src/BuildingBlocks` or `src/Modules` is missing from the scan.
- **Out of scope for now**: Host projects (may reference anything) and Providers (rule added with the first provider).

## SharedKernel & application conventions

- **SharedKernel** is one flat namespace (`Voluntro.SharedKernel`) with no package references (enforced by an architecture test).
- **IDs**: `readonly record struct PersonId(Guid Value) : IEntityId<PersonId>`. `IEntityId<TSelf>` declares `Guid Value`, `static abstract New()` (→ `Guid.CreateVersion7()`) and `static abstract From(Guid)`, so generic code (EF converters, tests) can create and rehydrate IDs without reflection. Each ID overrides `ToString()` to return the bare Guid (clean structured logs). `From` is total: `From(Guid.Empty)` is legal, so a bogus route ID becomes a normal NotFound. One EF convention registers converters for all `IEntityId<>` types. v7 is ordered only across milliseconds; test ordering via `Guid.CreateVersion7(DateTimeOffset)`.
- **Entities**: abstract class `Entity<TId> where TId : struct, IEntityId<TId>`, `Id { get; private init; }`. Equal when same runtime type and same `Id` (`IEquatable`, `==`/`!=`, hash of type + Id), so different entity types sharing an ID type never compare equal. `protected Entity(TId id)` throws on an empty ID; a parameterless `protected Entity()` exists for EF materialization. No EF lazy-loading proxies (they break the runtime-type check). Value objects are records; no base class.
- **Aggregates & domain events**: `AggregateRoot<TId> : Entity<TId>, IHasDomainEvents`, with `protected Raise(IDomainEvent)`. The non-generic `IHasDomainEvents` (read-only `DomainEvents`, `ClearDomainEvents()`) lets the EF interceptor collect events without knowing `TId`. `IDomainEvent` is a pure marker; events are records with no timestamp/ID (those belong on integration events and outbox rows).
- **Failures**: `sealed record Error(Code, Type, Detail?)`, value equality. `Code` is validated in the constructor against `^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$` (throws on mismatch); that the first segment is the owning module's key is checked by an architecture test. One static factory per type (`Error.NotFound(code, detail?)`, …). `ErrorType` ∈ Failure (= 0, so an accidental default is a 500, not a 400), Validation, NotFound, Conflict, Forbidden, Unauthorized, with explicit values → HTTP status. Modules declare error catalogs next to the aggregate in Domain: `public static class PersonErrors { public static readonly Error NotFound = Error.NotFound("membership.person.not_found"); }`. `Detail` is English text for developers; it is returned as the ProblemDetails `detail` and appears in logs, so it **never contains personal data** (IDs only). Field-level validation errors: decided with the validation decorator.
- **Results**: `Result` holds `Error?` with `[MemberNotNullWhen(true, nameof(Error))] IsFailure`; `Result<T> : Result where T : notnull` (a missing thing is a NotFound error, not a successful null). Private constructors; factories on the non-generic `Result` (`Success()`, `Failure(error)`, `Success<T>(value)`, `Failure<T>(error)`), because CA1000 forbids statics on generic types. Implicit conversions `T → Result<T>`, `Error → Result<T>`, `Error → Result`, so handlers `return person;` / `return PersonErrors.NotFound;`. Reading `Value` from a failure throws `InvalidOperationException` (a bug); `Failure(null)` throws `ArgumentNullException`. The inheritance lets decorators constrain `where TResponse : Result`.
- **Handlers**: `IQueryHandler<TQuery, TResponse>` with `Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken ct)`; no `IQuery<T>` marker. Command handler interfaces are added with the first command, together with the decorators. Each module registers its handlers explicitly and scoped in `IModule.Register` (`services.AddScoped<IQueryHandler<GetPerson, PersonDto>, GetPersonHandler>()`); no assembly scanning.
- **Analyzers**: CA1716 (identifiers matching VB keywords, e.g. `Error`, `New`) is disabled in `.editorconfig`; Voluntro is C#-only.
- **Validation**: domain invariants in value objects/factories (`Email.Create` → `Result<Email>`); request shape in hand-written `IValidator<TCommand>` run by `ValidationDecorator`; field errors as codes.
- **Transactions**: `TransactionDecorator` commits on success; an EF interceptor dispatches domain events, writes outbox rows and sets audit fields in the same transaction.
- **Concurrency**: Postgres `xmin` on every aggregate; `version` in DTOs and update requests; clash → 409 `common.concurrency_conflict`.
- **Auditing**: `CreatedAt/By`, `ModifiedAt/By` via `ICurrentUser`; background work acts as `System`. Full change log later, for sensitive modules only.
- **Time**: `TimeProvider`; `timestamptz` (UTC); Organization's time zone applied at the edges.
- **Background jobs**: hand-written `BackgroundService` + `PeriodicTimer` (outbox, token cleanup, retention).
- **Files**: `IFileStorage`, local disk under `/data/files` first; S3-compatible later.
- **Ops**: options `ValidateOnStart`; no personal data in logs (IDs only); `traceId` on every response.

## API conventions

- Routes `/api/{module}/{resource}`; ID segments use the `{id:guid}` constraint, so a malformed ID is a 404 like any unknown one. Actions bind `Guid` and convert with `PersonId.From(id)`.
- JSON camelCase, enums as camelCase strings in both directions (integer enum values are rejected on input), nulls omitted. `JsonConventions.Apply` holds the rules; `AddApiJsonConventions()` applies them to both MVC's `JsonOptions` and the HTTP `JsonOptions` (used by ProblemDetails outside MVC and by the OpenAPI generator).
- Lists: `?page=1&pageSize=25&sort=lastName:asc&search=…` → `{ items, page, pageSize, totalCount }`. `PageRequest` / `PagedList<T>` live in BuildingBlocks.Contracts so module Contracts can use them. Defaults page 1, pageSize 25; the `PageRequest` constructor clamps page to ≥ 1 and pageSize to 1–100. `sort` and `search` are raw strings until the first real list endpoint designs the sort grammar, the per-endpoint allow-list and the EF paging extension.
- Controllers derive from an abstract `ApiController : ControllerBase` (`[ApiController]`), return `ActionResult<TDto>` and branch explicitly: `result.IsFailure ? Problem(result.Error) : Ok(result.Value)`. `[ProducesResponseType<ProblemDetails>(…)]` only where an action can return it.
- Errors: RFC 9457 ProblemDetails with `code`, `traceId`, and `errors: { field: [codes] }` for validation.
  - `ErrorType` → status in one `switch`: Validation 400, NotFound 404, Conflict 409, Forbidden 403, Unauthorized 401, Failure 500 (a unit test covers every enum value). `detail` = `Error.Detail`.
  - `code` is present when there is one: on responses built from an `Error`, and `common.unexpected` on the 500. Framework-produced responses (unknown route, disabled module, automatic 400) carry none; the frontend falls back to the status.
  - `traceId` is the bare 32-hex W3C trace ID, the same value as the `X-Trace-Id` response header.
  - Unhandled exceptions → `UseExceptionHandler` → 500 with no exception message or stack trace in any environment; logged once.
  - `UseStatusCodePages` gives every bodyless 4xx/5xx a ProblemDetails body.
  - Known gap: `[ApiController]`'s automatic 400 (e.g. `?page=abc`) returns English messages, not codes. Replaced with the validation decorator.
- No versioning until needed.
- Built-in `Microsoft.AspNetCore.OpenApi` document at `/openapi/v1.json` and Swagger UI (Swashbuckle `SwaggerUI`) at `/swagger`, both in Development only; `.http` files per module are the versioned way to exercise endpoints.

## Module system

```csharp
public interface IModule
{
    string Key { get; }                    // "checkin"
    bool IsCore { get; }
    IReadOnlyList<string> DependsOn { get; }
    void Register(IServiceCollection services, IConfiguration config);
    // IEnumerable<Permission> Permissions { get; }   added with Identity, when permissions are first catalogued
}
```

- `IModule` lives in BuildingBlocks.Infrastructure. Modules are registered explicitly in `Program.cs` via `builder.AddModules(...)`, which calls `services.AddModule(module, config)` for each. `AddModule` calls `Register`, adds the module as an `IModule` singleton and adds the module's assembly (`module.GetType().Assembly`) as an MVC application part. The integration-test harness registers test-only modules through the same `AddModule`.
- `AddModule` throws at startup on a duplicate `Key` or a second module claiming the same assembly.
- A controller belongs to the module whose assembly contains it; there is no `[Module]` attribute, so a controller can't be left ungated or carry a mistyped key. Controllers in Host projects belong to no module and are never gated.
- `IModuleActivation { bool IsEnabled(string moduleKey); }` is synchronous (in-memory activation cache, refreshed on toggle) and the single authority: Platform's implementation returns `true` for core modules. Until Platform exists, `AllModulesEnabled` is registered with `TryAdd`.
- The **module gate** is middleware (`app.UseModuleGate()`) after routing and **before authentication and authorization**, so a disabled module answers 404 before any 401/403 could reveal it exists. It reads the endpoint's `ControllerActionDescriptor`, maps the controller's assembly to its module, and sets a bodyless 404; `UseStatusCodePages` makes that response identical to an unknown route's. A matched endpoint that is not a controller action is gated by path instead: `/api/{key}` belongs to module `{key}`. This covers the endpoints routing itself answers with (405 with an `Allow` header for a wrong method, 415 for a wrong content type), which would otherwise reveal a disabled module's routes.
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
- **Roles**: built-in Administrator (all permissions, immutable, ≥1 active) and **Basic**, the built-in default Role every new User receives (self-service permissions only); custom Roles; union of permissions.
- **Permissions**: `{module}.{resource}.{action}`, constants in each module's Contracts, exposed via `IModule.Permissions` (added to `IModule` with Identity), catalogued by Identity at startup; `[HasPermission(...)]` via a dynamic policy provider; resolved per request through cached `IPermissionProvider` keyed by user + `perm_version`.
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
2. Platform module: Organization, ModuleActivation, Setup
3. Identity: password sign-in, JWT + refresh rotation, Sessions, Roles & Permissions, Invitations, Self-registration
4. Communication core: SMTP/MailKit, Fluid templates, consumers for Identity events
5. Frontend shell: i18n, auth client, Setup wizard, sign-in, invitation/reset pages, org settings & modules admin, own security page
6. Identity: TOTP 2FA + recovery codes, then passkeys (with Playwright tests)
7. Docker images, compose, Caddy

**Afterwards** (one planning session each): Membership (incl. User backfill) → Small groups → Calendar → Room booking → Check-in → Payments + Tickets → Newsletter.

## Open questions

- NodaTime for Calendar recurrence
- Which SMS and payment vendors to support first
- Scope of the full change log for sensitive modules (Membership, Check-in)
