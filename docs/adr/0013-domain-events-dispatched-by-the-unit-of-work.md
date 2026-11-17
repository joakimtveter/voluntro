# Domain events are dispatched by the unit of work, before the one save

A command's domain events are handled in the same save as the change that raised them. Each module's `IUnitOfWork` is a `ModuleUnitOfWork<TContext>` (BB.Infrastructure), which wraps the module's context and the scope's services. Before it saves, it collects the events from every tracked aggregate, clears them, and runs their `IDomainEventHandler<TEvent>`s (BB.Application, `Task<Result> HandleAsync(TEvent, ct)`). It repeats while handlers raise new events, up to 10 rounds; an 11th round throws `InvalidOperationException`, because events raising each other forever is a bug. Only then does it call `SaveChangesAsync` once (ADR-0012), so the audit interceptor stamps whatever the handlers changed, and the outbox rows they added commit with the rest. A handler changes tracked entities of its own module through the module's narrow context and never saves. Reacting in another module always goes through an integration event and the outbox (ADR-0005, ADR-0014).

We chose the unit of work over a `SavingChanges` interceptor. In the unit of work the order (dispatch, then save and audit) is written out in code, and a handler's failed `Result` becomes the unit of work's failed `Result`, the same path a concurrency conflict takes. An interceptor's correctness would depend on being added before the audit interceptor, and it can only fail by throwing. The context itself can't dispatch: it is built from `DbContextOptions` alone and can't see the scope's handlers or `TimeProvider`.

## Considered options

- **A `DomainEventInterceptor` ahead of `AuditInterceptor`**: dispatches on every save, including a direct `SaveChangesAsync`, but the order is implicit and expected failures need exceptions. In EF Core 10 an interceptor's `SavingChangesAsync` runs once, outside the retrying execution strategy, so retries were not the deciding factor (either place runs the dispatch only once).
- **A dispatcher passed to every module context's constructor**: keeps dispatch in the context, at the cost of a constructor parameter in every module.
- **Reading the application service provider out of the EF options**: works, but relies on EF internals.

## Consequences

- A direct `context.SaveChangesAsync()` (a test, a hand-written background job) dispatches nothing. The pipeline, through `IUnitOfWork`, is the only supported way to save. The outbox processor's consumer saves go through it too, so a consumer's own domain events are dispatched.
- A failed `Result` from a domain event handler stops the round: later handlers don't run, nothing is saved, and the transaction decorator discards the changes and returns the failure. So a handler can veto a command.
- Several handlers for one event run in registration order. Events in one round run in the order they were raised, but across aggregates in the order the change tracker returns them, which is not guaranteed. Handlers must not depend on the order of different events.
- Domain event handlers are registered through the module's registrar (`services.AddHandlers<TContext>()`), which binds them to that module's unit of work. `AddModule` throws when the container holds an `IDomainEventHandler<…>` that didn't come from the registrar, because the unit of work would never call it.
- The translation of `DbUpdateConcurrencyException` into `common.concurrency_conflict` moves from `ModuleDbContext` to `ModuleUnitOfWork` (amends ADR-0012). `ModuleDbContext` no longer implements `IUnitOfWork`.
