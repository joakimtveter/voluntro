using Microsoft.Extensions.Logging;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

internal static partial class PersistenceLog
{
    /// <param name="rows">Entity type names and keys only: IDs, never personal data.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "A save of {Context} hit a concurrency conflict on {Rows}; answered common.concurrency_conflict. " +
            "An UPDATE or DELETE matched no row: someone else saved first, or the row never existed (a bug).")]
    public static partial void ConcurrencyConflict(ILogger logger, string context, string rows, Exception exception);
}
