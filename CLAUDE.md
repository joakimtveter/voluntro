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
pnpm test         # Run tests with Vitest
pnpm lint         # Lint with Oxlint
pnpm lint:fix     # Auto-fix lint issues
pnpm format       # Format with Oxfmt
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
- `src/routes/` — File-based routing via TanStack Router
- `src/domains/` — Feature domains: `members`, `events`, `groups`, `venues`. Each domain contains its own components, queries, and forms.
- `src/shared/` — Cross-domain components, hooks, lib utilities, types, constants

**Path aliases:** `#/*` and `@/*` both map to `src/`.

### Backend

**Stack:** .NET 10, ASP.NET Core, Entity Framework Core 10 with SQL Server, Serilog, Swashbuckle/OpenAPI.

**Pattern:** Controllers → Services (interface + implementation) → `AppDbContext`

**Domains:** `Members`, `Events`, `Venues`, `Groups`, `MemberGroup` (join table for group membership)

Key conventions:
- All entities use UUID v7 (`GuidV7`) for primary keys
- Soft deletes via `IsDeleted` flag on all entities
- Groups support parent-child hierarchies via a self-referential FK
- DTOs in `Dtos/` are used for all API request/response shapes
- CORS is configured to allow only `http://localhost:3000`

### Communication

Frontend (port 3000) ↔ REST API (port 5000/5001) ↔ SQL Server (Docker)
