# Handlers are injected directly, with no mediator

Controllers depend on `ICommandHandler<TCommand, TResult>` / `IQueryHandler<TQuery, TResult>` directly. Cross-cutting behaviour (validation, transactions, logging) is added with hand-written decorators, registered by a small `Decorate` helper. We rejected MediatR (now commercially licensed) and a self-built `ISender.Send(...)` mediator. Both are service locators that hide a controller's real dependencies and break "go to definition" from controller to handler.

## Consequences

- Handlers never call `SaveChanges`. A transaction decorator commits on success, and an EF interceptor dispatches domain events, writes outbox messages and sets audit fields in the same transaction.
