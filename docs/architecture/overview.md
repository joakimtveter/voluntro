# Voluntro — Architecture Overview

**Status:** agreed architecture overview (2026-11-06). Each module and each foundation
milestone gets its own planning session; decisions made there are recorded here.

## Context

Voluntro is a CRM-like app for churches and other volunteer-based organizations,
primarily for the Norwegian market (nb + en UI, Ukrainian later). The repo is empty
(README, LICENSE, .gitignore). The user is building it to sharpen architecture and
development skills, so the design favours explicit, compiler-enforced boundaries
(Clean Architecture + DDD, modular monolith) over shortcuts, and minimal third-party
dependencies (free/OSS only).

## Decisions

| Topic | Decision |
|---|---|
| Hosting | **Self-hosted, single organization.** No TenantId. Future SaaS = **database-per-tenant** (only connection-string resolution changes). Rule from day one: nothing org-specific in code — name, logo, locale, time zone, enabled modules, providers live in DB/config |
| Who logs in | **Everyone** — staff, volunteers, members, parents. `User` (Identity) ≠ `Person` (Membership); a Person optionally links to a User. Children are Persons without Users |
| Auth transport | **JWT bearer.** Short-lived access token (≈10 min) kept in memory only; opaque refresh token in HttpOnly/Secure/SameSite=Strict cookie, rotated on every use with reuse detection |
| Architecture style | **Modular monolith**, one deployable API, modules ready to be split into services |
| Module layout | **4 projects per module:** Domain / Application / Infrastructure / Contracts |
| Module toggling | **Runtime**, by an admin. Disabled ⇒ UI hidden, API returns 404 |
| Inter-module comms | **Integration events** (in-process bus + transactional outbox/inbox in Postgres) + **small public query interfaces** in each module's Contracts. No shared tables |
| Dev orchestration | **.NET Aspire** (AppHost + ServiceDefaults). Dev-time only; self-hosters get Docker images + docker-compose |
| Platform | .NET 10 (LTS), C# 14, EF Core 10 + Npgsql, PostgreSQL, controllers, no repository pattern, System.Text.Json, xUnit v3 |
| Frontend | TanStack Start, Query, Table, Router; Zod; shadcn/ui on Base UI |

## System context

```
 Browser ──► TanStack Start (SSR + client) ──► Voluntro.Api (modular monolith) ──► PostgreSQL
                                                      │                       (schema per module)
                                                      └──► Providers: Email / SMS / Payment
 Self-host: reverse proxy (e.g. Caddy) serves web at /  and API at /api  → same origin
```

Same origin (API behind `/api`) keeps the refresh cookie first-party and avoids CORS.

## Repository layout

```
voluntro/
├─ Voluntro.slnx
├─ global.json  Directory.Build.props  Directory.Packages.props   (central package mgmt, nullable, warnings-as-errors)
├─ src/
│  ├─ Host/
│  │  ├─ Voluntro.AppHost/            Aspire: Postgres, Api, web (AddViteApp), Mailpit
│  │  ├─ Voluntro.ServiceDefaults/    OpenTelemetry, health checks, resilience
│  │  └─ Voluntro.Api/                Composition root only: Program.cs discovers & registers modules
│  ├─ BuildingBlocks/
│  │  ├─ Voluntro.SharedKernel/       Entity, AggregateRoot, ValueObject, IDomainEvent, Result/Error, strongly-typed IDs
│  │  ├─ Voluntro.BuildingBlocks.Application/     ICommand/IQuery + handlers, own dispatcher, IIntegrationEvent, IEventBus, IClock (TimeProvider)
│  │  └─ Voluntro.BuildingBlocks.Infrastructure/  ModuleDbContext base, outbox/inbox, EF conventions (snake_case), module-toggle filter, ProblemDetails mapping, audit interceptor
│  ├─ Modules/
│  │  └─ <Module>/
│  │     ├─ Voluntro.<Module>.Domain/
│  │     ├─ Voluntro.<Module>.Application/
│  │     ├─ Voluntro.<Module>.Infrastructure/   EF DbContext + migrations, controllers (Api/ folder), IModule implementation
│  │     └─ Voluntro.<Module>.Contracts/        integration events, public query interfaces + DTOs
│  └─ Providers/
│     ├─ Voluntro.Providers.Email.Smtp/
│     ├─ Voluntro.Providers.Sms.<Vendor>/
│     └─ Voluntro.Providers.Payments.<Vendor>/
├─ tests/
│  ├─ Voluntro.ArchitectureTests/
│  └─ Modules/<Module>/ Voluntro.<Module>.Domain.Tests, .Application.Tests, .IntegrationTests
└─ web/                               TanStack Start app
```

## Module anatomy & dependency rules

```
            Contracts  ◄──────────── (other modules' Application may reference ONLY this)
               ▲
 Domain ◄── Application ◄── Infrastructure ◄── Voluntro.Api (composition root)
   │            │                 │
 SharedKernel  BB.Application    BB.Infrastructure, EF Core, ASP.NET Core
```

- **Domain**: aggregates, value objects, domain events, invariants. References SharedKernel only. No EF, no ASP.NET.
- **Application**: use cases as command/query handlers. Depends on Domain, BuildingBlocks.Application, and *other modules' Contracts*. Uses the module's DbContext through a narrow interface (e.g. `IMembershipDbContext` exposing `DbSet`s) — EF directly, no repositories.
- **Infrastructure**: DbContext, EF configurations, migrations, controllers, Contracts implementations, event consumers, `IModule` registration.
- **Contracts**: plain records, no logic. Published language to the rest of the system.
- **Forbidden**: any reference from one module to another module's Domain/Application/Infrastructure; Domain referencing EF/ASP.NET. Enforced by architecture tests (hand-written reflection tests or NetArchTest — decide in foundation session).

**CQRS without MediatR** (now commercially licensed): a small hand-written dispatcher with
decorators (logging, validation, transaction). Commands load aggregates and call domain
methods; queries may project straight from EF with `AsNoTracking()` and skip the domain.
Expected failures return `Result<T>`; controllers map errors to RFC 9457 ProblemDetails
with stable error codes.

## Module system

```csharp
public interface IModule
{
    string Key { get; }                    // "checkin"
    bool IsCore { get; }                   // core modules can't be disabled
    IReadOnlyList<string> DependsOn { get; }
    void Register(IServiceCollection services, IConfiguration config);
    IEnumerable<Permission> Permissions { get; }
}
```

- `Voluntro.Api` registers every module (controllers via `AddApplicationPart`).
- **Runtime toggle**: Platform module stores enabled modules. Controllers carry a
  `[Module("checkin")]` attribute; a filter returns 404 when disabled (cached, invalidated on change).
  Enabling checks `DependsOn` (e.g. Tickets → Calendar). The frontend reads
  `GET /api/platform/modules` to build navigation.
- Integration-event consumers keep running for disabled modules so data stays consistent if re-enabled.

## Module map

| Module | Kind | Owns | Depends on (via Contracts) |
|---|---|---|---|
| **Platform** | core | Org profile, branding, locale, time zone, enabled modules | — |
| **Identity** | core | Users, credentials, passkeys, 2FA, refresh tokens, roles → permissions | Platform |
| **Membership** | core | People, households, relationships (guardian/child), membership status, contact info, consents | Identity (User link) |
| **Communication** | core | Email/SMS sending, templates per locale, delivery log | Membership |
| **Payments** | optional | Payment intents, provider callbacks, refunds | — |
| Children's check-in | optional | Check-in sessions, check-ins, labels/pickup codes | Membership, Calendar |
| Small groups | optional | Groups, leaders, members, meetings | Membership |
| Calendar | optional | Events, recurrence | — |
| Tickets | optional | Ticket types, orders, tickets | Calendar, Payments, Membership |
| Room booking | optional | Rooms, resources, bookings | Calendar |
| Newsletter | optional | Lists, subscriptions, campaigns | Communication, Membership |

## Data

- One PostgreSQL database, **one schema and one DbContext per module**, each with its own
  `__ef_migrations_history` table in its schema.
- No foreign keys across schemas; modules reference each other by ID only.
- IDs: strongly-typed wrappers around `Guid.CreateVersion7()` (time-ordered, index-friendly).
- Timestamps as `timestamptz` (UTC) via `DateTimeOffset` + `TimeProvider`; org time zone
  (`Europe/Oslo` by default) applied at the edges. Reconsider NodaTime when planning Calendar recurrence.
- Audit fields + audit log via an EF `SaveChanges` interceptor.

## Inter-module communication

1. **Domain events**: raised by aggregates, dispatched in-process inside the module within the same transaction.
2. **Integration events** (in Contracts, e.g. `PersonCreated`, `UserRegistered`): written to the module's
   `outbox` table in the same transaction as the state change. A `BackgroundService` publishes them to
   an in-memory bus; consumers record handled message IDs in an `inbox` table (idempotency).
3. **Synchronous reads**: public query interfaces in Contracts (e.g. `IMembershipQueries.GetGuardians(childId)`),
   implemented in the owning module's Infrastructure.

**Microservice path**: outbox publishes to a broker (e.g. RabbitMQ) instead of memory; query
interfaces become HTTP/gRPC clients; schema → own database. Contracts don't change.

## Identity & authentication

- ASP.NET Core Identity (EF stores, `identity` schema) for password hashing, lockout, TOTP, recovery codes and **.NET 10's built-in passkey (WebAuthn) support**.
- Hand-written token service issues **JWTs signed asymmetrically (ES256/RS256)** and exposes a JWKS endpoint, so future services can validate with the public key. Validation through `Microsoft.AspNetCore.Authentication.JwtBearer` (first-party).
- Flows:
  - Password → if 2FA is enabled, returns `{ requiresTwoFactor, challengeToken }` (short-lived, single purpose) → TOTP or recovery code → tokens.
  - Passkey → assertion verified → tokens (counts as MFA).
  - `POST /api/identity/refresh` rotates the refresh token (hashed in DB, family-based reuse detection revokes the whole family).
- **Authorization**: permission-based policies (`membership.people.read`). Each module declares
  its permissions; roles are admin-defined bundles of permissions. Permissions travel in the JWT
  or are resolved per request (decide in the Identity session).
- **User ↔ Person**: on `UserRegistered`, Membership links to an existing Person by verified email or creates one. Parents act for their children through the guardian relationship in Membership.
- **SSR caveat**: access tokens live in browser memory, so SSR can't call authenticated endpoints directly. Default: SSR for public pages (events, ticket sales, newsletter sign-up); client-side fetching for authenticated areas. Revisit in the frontend session.

## Internationalization

- UI languages: `nb` (default) and `en`, structured so `uk` is just another message file.
- API returns **error codes, not translated text**; the frontend translates them.
- Backend translates only outbound content (email/SMS templates per locale) using the Person's preferred language, falling back to the org default.
- Library choice for the frontend (Paraglide JS vs i18next) is decided in the frontend session.

## Pluggable providers

- Abstractions in Communication/Payments Application layers: `IEmailSender`, `ISmsSender`, `IPaymentProvider`.
- Each vendor implementation is its own project under `src/Providers/`, exposing a registration extension.
  Selected by config (`Providers:Email:Type = "Smtp"`); secrets come from environment/user-secrets.
- Sends go through the outbox, so a provider outage only delays messages.
- Initial candidates: SMTP (dev: Mailpit via Aspire); Norwegian SMS/payment vendors (e.g. Vipps MobilePay) chosen per module.

## Frontend architecture

```
web/src/
├─ routes/                 thin file-based routes (TanStack Router), import from modules
├─ modules/<module>/       api client, Zod schemas, query-key factories, components, hooks
├─ shared/                 ui (shadcn/Base UI), api client core (token handling, refresh), i18n, auth context
```

- One app, feature folders mirroring backend modules; disabled modules are hidden from navigation and route guards.
- The API publishes OpenAPI (built-in `Microsoft.AspNetCore.OpenApi`). Hand-written Zod schemas vs generated types is decided in the frontend session.
- API client keeps the access token in memory, refreshes on 401 with a single-flight lock.

## Cross-cutting concerns

- **GDPR** (children's data, Datatilsynet): consent records, audit log, data export, deletion/anonymization, retention policies. Designed into Membership and Check-in from the start.
- **Observability**: `ILogger` + OpenTelemetry via ServiceDefaults; Aspire dashboard in dev.
- **Testing**: domain unit tests; handler tests; integration tests against real Postgres (Aspire.Hosting.Testing or Testcontainers — never the EF in-memory provider); architecture tests for the dependency rules.
- **Deployment (self-host)**: Docker images for `api` and `web`, `docker-compose.yml` with Postgres and a reverse proxy; Aspire can generate the compose file.

## Suggested build order

1. Solution skeleton, Aspire AppHost/ServiceDefaults, BuildingBlocks, architecture tests
2. Platform module + runtime module toggle
3. Identity: password login, JWT + refresh rotation
4. Identity: TOTP 2FA, then passkeys
5. Membership (People, households, guardians, consents)
6. Communication + SMTP provider + outbox in practice
7. Frontend shell: auth, i18n, module-aware navigation
8. Feature modules, one planning session each: Small groups → Calendar → Room booking → Check-in → Payments + Tickets → Newsletter

## Open questions for later sessions

- Permissions in JWT vs resolved per request
- Architecture tests: hand-written vs NetArchTest
- Frontend i18n library; OpenAPI codegen vs hand-written Zod
- SSR strategy for authenticated pages
- NodaTime for Calendar recurrence
- Which SMS/payment vendors to support first

## Verification

- This deliverable: `docs/architecture/overview.md` exists, matches this plan, and every decision in the table above is reflected.
- Later milestones verify against it: `aspire run` starts Postgres, API, web and Mailpit; architecture tests fail on any forbidden cross-module reference; each module's integration tests run against real Postgres.
