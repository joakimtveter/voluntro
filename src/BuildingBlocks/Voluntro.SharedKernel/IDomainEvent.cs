namespace Voluntro.SharedKernel;

/// <summary>
/// Something meaningful that happened inside an aggregate, dispatched in the same transaction.
/// Implement as a record, e.g. <c>sealed record PersonRegistered(PersonId PersonId) : IDomainEvent</c>.
/// Event IDs and timestamps belong on integration events, not here.
/// </summary>
public interface IDomainEvent;
