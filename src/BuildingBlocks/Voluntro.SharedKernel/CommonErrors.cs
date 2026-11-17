namespace Voluntro.SharedKernel;

/// <summary>
/// Errors that belong to no module. <c>common</c> is the one code prefix that isn't a module key.
/// </summary>
public static class CommonErrors
{
    /// <summary>
    /// The aggregate changed since the client (or the handler) read it: its <see cref="AggregateRoot{TId}.Version"/>
    /// no longer matches. The client reloads and tries again.
    /// </summary>
    public static readonly Error ConcurrencyConflict = Error.Conflict("common.concurrency_conflict");
}
