# Modular monolith with four projects per module

Voluntro ships as one deployable API, but every module (Platform, Identity, Membership, Check-in, …) is split into four projects: Domain, Application, Infrastructure and Contracts. A module may reference another module **only through its Contracts project**. We chose this over a classic shared-layer Clean Architecture (one Domain/Application/Infrastructure for the whole app) and over one project per module with folders. The compiler enforces the boundaries instead of conventions, and any module can later be lifted out into its own service without untangling it first. Each module also owns its own Postgres schema and DbContext, with no foreign keys across schemas.

## Consequences

- The solution will have ~40+ projects. This is accepted as the price of compiler-enforced boundaries.
- Hand-written architecture tests (reflection over `GetReferencedAssemblies()`) cover what project references can't, e.g. "Domain references only SharedKernel".
