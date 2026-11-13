namespace Voluntro.MigrationService;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Critical, Message = "Applying the module migrations failed.")]
    public static partial void MigrationsFailed(ILogger logger, Exception exception);
}
