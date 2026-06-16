# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Voluntro is a CRM application for volunteer-based organizations. It is a monorepo with three components:
- `voluntro-frontend` — React SPA
- `voluntro-backend` — ASP.NET Core REST API
- `voluntro-database` — Docker-based SQL Server

## Commands

### Frontend (`voluntro-frontend/`)
```bash
pnpm dev          # Start dev server on port 3000
pnpm build        # Production build
pnpm test         # Run tests with Vitest (run mode, non-interactive)
pnpm lint         # Lint with Oxlint
pnpm lint:fix     # Auto-fix lint issues
pnpm format       # Format with Oxfmt
pnpm format:check # Check formatting without writing
```

### Backend (`voluntro-backend/VoluntroApi/`)
```bash
dotnet build                               # Build solution
dotnet run                                 # Run API (requires DB)
dotnet test                                # Run tests
dotnet ef migrations add <MigrationName>   # Create new migration
dotnet ef database update                  # Apply pending migrations
```
Swagger UI is available at `/swagger/ui` in development.

### Database (`voluntro-database/`)
```bash
docker-compose up    # Start SQL Server 2025 container
docker-compose down  # Stop container
```
Dev connection string uses SA credentials defined in `docker-compose.yml`.

## Architecture

### Frontend

**Stack:** React 19, TypeScript 6, TanStack Start (Vite-based), TanStack Router, TanStack Query, TanStack Form, Tailwind CSS 4, Zod, shadcn/Base UI.

**Structure:**
- `src/routes/` — File-based routing via TanStack Router; route files own their page layout and search params
- `src/domains/` — Feature domains: `members`, `events`, `groups`, `venues`, `tags`, `member-types`, `group-membership`. Each domain contains components, Zod schemas (`.schema.ts`), TypeScript types (`.types.ts`), and TanStack Query hooks (`use-*.ts`)
- `src/shared/` — Cross-domain components, hooks, lib utilities, types, constants

**Path aliases:** `#/*` and `@/*` both map to `src/`.

**Data fetching pattern:** All API calls go through `apiFetch` in `src/shared/lib/fetch/api-fetch.ts`, which wraps `fetch`, adds JSON headers, and throws a typed `ApiError` on non-2xx responses. Query hooks export both `useXxx` and `useXxxQueryOptions` (for SSR pre-fetching). Query keys are centralized in `src/shared/constants/query-keys.ts`.

**Form pattern:** TanStack Form + Zod. Schemas export both `XxxFormValues` (input shape) and `XxxPayload` (output shape after transforms, e.g. empty strings → null). Reusable field components live in `src/shared/components/app-form-fields/`.

**Search params:** Route search params are validated with Zod schemas using `validateSearch`. Navigation uses `navigate({ search: (prev) => ({ ...prev, key: value }) })` to preserve other params.

**Admin routes:** `/admin/*` routes have access to soft-deleted data and GDPR erasure; regular `/members/*` routes only see active records.

### Backend

**Stack:** .NET 10, ASP.NET Core, Entity Framework Core 10 with SQL Server, Serilog, Swashbuckle/OpenAPI.

**Pattern:** Controllers → Services (interface + implementation) → `AppDbContext`

**Domains:** `Members`, `Events`, `Venues`, `Groups`, `Tags`, `MemberTypes`, `MemberGroup` (group membership join table), `MemberTag` (tags join table)

**Key conventions:**
- All entities use UUID v7 (`GuidV7`) for primary keys, generated via `GuidV7Generator` in `AppDbContext`
- Soft deletes via `IsDeleted` flag on all entities; admin endpoints accept `IncludeDeleted` query param
- Delete behavior: member/tag/group relationships cascade; group-to-parent and member-to-MemberType are `Restrict`
- Groups support parent-child hierarchies via a self-referential FK (`ParentGroupId`)
- DTOs in `Dtos/` are used for all API request/response shapes; request DTOs use `init`-only properties
- `Dtos/Shared/` holds `PagedResult<T>` and other cross-domain response shapes
- Custom validation attributes live in `Validation/` (e.g. `NotInFutureAttribute`)
- `QueryController` handles combobox/autocomplete queries used by picker fields in the frontend
- There is a seeded default `MemberType` with a hardcoded UUID (`019ebbd6-fca1-73e0-9ec9-181a6ea57fc8`)
- CORS is configured to allow only `http://localhost:3000`

**Admin vs. public API:** Public controllers are at `api/[controller]` (e.g. `api/members`). Admin controllers are at `api/admin/[entity]` and expose soft-delete visibility and GDPR erasure.

### Communication

Frontend (port 3000) ↔ REST API (port 5000/5001) ↔ SQL Server (Docker)
