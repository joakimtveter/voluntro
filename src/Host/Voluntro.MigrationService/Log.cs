namespace Voluntro.MigrationService;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Critical, Message = "Applying the module migrations failed.")]
    public static partial void MigrationsFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Applying the module migrations was cancelled before it finished.")]
    public static partial void MigrationsCancelled(ILogger logger);
}
