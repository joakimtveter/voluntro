# An aggregate's concurrency version is Postgres `xmin`, exposed as `Version` on `AggregateRoot`

Every aggregate is protected against concurrent edits by optimistic concurrency on Postgres' `xmin` system column, which changes on every write to a row. `AggregateRoot<TId>` declares `public uint Version { get; private set; }`, and a convention in `ModuleDbContext` maps it as the row version on `xmin`. We chose this over an EF shadow property because DTO projections and update handlers need the version typed (`x.Version`, `Entry(x).Property(e => e.Version).OriginalValue = request.Version`). With a shadow property, every query and update would reach it through `EF.Property<uint>(x, "Version")`, with a name constant that Application has to share. Only the database assigns `Version`; domain code never sets it or reads meaning into it. `xmin` is a 32-bit transaction ID that wraps around, so versions are compared for **equality only** ("has it changed?"), never ordered ("which is newer?").

## Considered options

- **Shadow property added by convention**: keeps SharedKernel free of a value only persistence assigns, but makes the version stringly typed everywhere it's used.
- **An explicit counter column incremented on save**: orderable and database-neutral, but it's one more column and an interceptor to maintain, for a database we don't plan to swap. `xmin` costs no column and no code.

## Consequences

- `version` appears in DTOs and update requests. A stale version is a 409 `common.concurrency_conflict`.
- `Version` is 0 on a new aggregate until it is saved; EF reads `xmin` back after every insert and update.
- The generated migration lists `xmin` (`type: "xid", rowVersion: true`), but Npgsql's SQL generator skips system columns, so no column is created. A column named `version` in a migration means the convention is broken.
- Only the root's row carries the version. A change that writes only child entities would leave `Version` unchanged, so the audit interceptor stamps the root's `ModifiedAt/By` whenever a child is added, modified or deleted. That writes the root's row with its `xmin` check, even when the stamp's values are unchanged (see the overview, Auditing and Concurrency).
- Only `Entity<>` children stamp the root. Rows that aren't entities don't: owned types in a table of their own (`OwnsMany`) and many-to-many join rows. A change to those alone leaves `Version` unchanged and a concurrent one goes undetected. Aggregates reference each other by ID, so a many-to-many between them doesn't arise; how value objects are mapped is still open (decided with Platform, see the overview).
- A child moved from one aggregate to another (its owning foreign key changed) fails the save: only the aggregate it joins would be stamped, and the one it left would change without a new version.
- A missing `version` in a request binds as 0, which `xmin` never is (Postgres doesn't assign transaction ID 0). Update validators report it as `version: required` (`errors.RequireVersion(…)`), so it is a 400, not a 409 that no reload can fix.
- Update handlers compare the loaded `Version` with the request's (`EnsureVersion`), and the `xmin` check at save time catches an edit that lands between the load and the commit. Application never sets `OriginalValue`.
