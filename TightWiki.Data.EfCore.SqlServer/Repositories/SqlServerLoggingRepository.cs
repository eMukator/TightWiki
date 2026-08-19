using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Data.EfCore.SqlServer.Repositories
{
    /// <summary>
    /// MSSQL/EF Core implementation of <see cref="ITwLoggingRepository"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton only (Database-Providers-Plan.md phase 2a.1) - every member throws
    /// <see cref="NotImplementedException"/> for now. Real LINQ-based implementations land in phase 2a.7.
    /// See <see cref="SqlServerConfigurationRepository"/> for why this is a concrete class rather than typing
    /// <see cref="SqlServerDatabaseManager.LoggingRepository"/> directly as <see cref="ITwLoggingRepository"/>.
    /// </remarks>
    public class SqlServerLoggingRepository : ITwLoggingRepository
    {
        public Task PurgeLogs()
            => throw new NotImplementedException();

        public Task CreateTablesIfNotExist()
            => throw new NotImplementedException();

        public Task WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null)
            => throw new NotImplementedException();

        public Task WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null)
            => throw new NotImplementedException();

        public Task<int> GetExceptionCount()
            => throw new NotImplementedException();

        public Task<List<TwLogEntry>> GetLogEntriesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? severity = null)
            => throw new NotImplementedException();

        public Task<TwLogEntry> GetLogEntryById(int id)
            => throw new NotImplementedException();

        public Task<List<TwEventLogSeverity>> GetSeverities()
            => throw new NotImplementedException();
    }
}
