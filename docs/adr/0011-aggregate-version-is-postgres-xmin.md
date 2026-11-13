# An aggregate's concurrency version is Postgres `xmin`, exposed as `Version` on `AggregateRoot`

Every aggregate is protected against concurrent edits by optimistic concurrency on Postgres' `xmin` system column, which changes on every write to a row. `AggregateRoot<TId>` declares `public uint Version { get; private set; }`, and a convention in `ModuleDbContext` maps it as the row version on `xmin`. We chose this over an EF shadow property because DTO projections and update handlers need the version typed (`x.Version`, `Entry(x).Property(e => e.Version).OriginalValue = request.Version`). With a shadow property, every query and update would reach it through `EF.Property<uint>(x, "Version")`, with a name constant that Application has to share. Only the database assigns `Version`; domain code never sets it or reads meaning into it. `xmin` is a 32-bit transaction ID that wraps around, so versions are compared for **equality only** ("has it changed?"), never ordered ("which is newer?").

## Considered options

- **Shadow property added by convention**: keeps SharedKernel free of a value only persistence assigns, but makes the version stringly typed everywhere it's used.
- **An explicit counter column incremented on save**: orderable and database-neutral, but it's one more column and an interceptor to maintain, for a database we don't plan to swap. `xmin` costs no column and no code.

## Consequences

- `version` appears in DTOs and update requests. A stale version is a 409 `common.concurrency_conflict`.
- `Version` is 0 on a new aggregate until it is saved; EF reads `xmin` back after every insert and update.
- The generated migration lists `xmin` (`type: "xid", rowVersion: true`), but Npgsql's SQL generator skips system columns, so no column is created. A column named `version` in a migration means the convention is broken.
- Only the root's row carries the version. A change that writes only child entities leaves `Version` unchanged, so concurrent edits of children go undetected unless such changes also write the root's row (open; decided with the command pipeline).
