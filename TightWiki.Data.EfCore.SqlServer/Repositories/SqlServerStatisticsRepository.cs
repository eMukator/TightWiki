using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Data.EfCore.SqlServer.Repositories
{
    /// <summary>
    /// MSSQL/EF Core implementation of <see cref="ITwStatisticsRepository"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton only (Database-Providers-Plan.md phase 2a.1) - every member throws
    /// <see cref="NotImplementedException"/> for now. Real LINQ-based implementations land in phase 2a.9.
    /// See <see cref="SqlServerConfigurationRepository"/> for why this is a concrete class rather than typing
    /// <see cref="SqlServerDatabaseManager.StatisticsRepository"/> directly as <see cref="ITwStatisticsRepository"/>.
    /// </remarks>
    public class SqlServerStatisticsRepository : ITwStatisticsRepository
    {
        public Task IncrementPageViewCount(int pageId)
            => throw new NotImplementedException();

        public Task MergePageCompilationStatistics(int pageId, double wikifyTimeMs, int matchCount, int errorCount, int outgoingLinkCount, int tagCount, int processedBodySize, int bodySize)
            => throw new NotImplementedException();

        public Task<int> GetPageTotalViewCount(int pageId)
            => throw new NotImplementedException();

        public Task PurgePageStatistics()
            => throw new NotImplementedException();

        public Task<List<TwPageStatistics>> GetPageStatisticsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => throw new NotImplementedException();

        public Task<int> DeletePageStatisticsByPageId(int pageId)
            => throw new NotImplementedException();
    }
}
