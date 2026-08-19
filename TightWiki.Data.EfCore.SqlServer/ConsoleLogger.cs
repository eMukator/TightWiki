using Microsoft.Extensions.Logging;

namespace TightWiki.Data.EfCore.SqlServer
{
    /// <summary>
    /// Minimal <see cref="ILogger"/> implementation used as the bootstrap-time logger for
    /// <see cref="SqlServerDatabaseManager"/>, mirroring TightWiki.Library.ConsoleLogger (the equivalent
    /// used by the SQLite <c>DatabaseManager</c> before its own <c>DatabaseLogger</c> is available). Not
    /// referenced from TightWiki.Library directly to keep this driver project's ProjectReferences limited to
    /// TightWiki.Data.EfCore and TightWiki.Plugin (see Database-Providers-Plan.md chapter 3).
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            var color = logLevel switch
            {
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.Gray
            };

            var originalColor = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] [{logLevel}] ");
            Console.ForegroundColor = originalColor;

            Console.WriteLine(message);

            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exception);
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
