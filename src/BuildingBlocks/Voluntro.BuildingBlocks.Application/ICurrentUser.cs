namespace Voluntro.BuildingBlocks.Application;

/// <summary>Who the current request or background job acts as. Used for audit stamps, later for permissions.</summary>
/// <remarks>
/// Resolved per scope. Background work (an outbox consumer, a cleanup job) runs in a scope with no request, and must act
/// as <see cref="SystemUser.Id"/>, never throw: an implementation that reads the request, like Identity's, falls back
/// to System when there is none. The outbox delivery tests check it for consumers.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>The acting user's ID, or <see cref="SystemUser.Id"/> for background work.</summary>
    Guid UserId { get; }
}

/// <summary>The well-known user that background work, and every request until Identity exists, acts as.</summary>
public static class SystemUser
{
    /// <summary>Not <see cref="Guid.Empty"/>, so an unset ID is never mistaken for the System user.</summary>
    public static readonly Guid Id = new("00000000-0000-0000-0000-000000000001");
}
