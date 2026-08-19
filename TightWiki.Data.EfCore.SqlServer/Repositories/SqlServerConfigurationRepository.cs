using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Data.EfCore.SqlServer.Repositories
{
    /// <summary>
    /// MSSQL/EF Core implementation of <see cref="ITwConfigurationRepository"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton only (Database-Providers-Plan.md phase 2a.1) - every member throws
    /// <see cref="NotImplementedException"/> for now. Real LINQ-based implementations land in phase 2a.6.
    /// Kept as a concrete class (rather than typing <see cref="SqlServerDatabaseManager.ConfigurationRepository"/>
    /// directly as <see cref="ITwConfigurationRepository"/>) so the property's signature never needs to change
    /// when that happens, mirroring the explicit-interface-implementation pattern used by the SQLite
    /// <c>DatabaseManager</c> (see Database-Providers-Plan.md chapter 4.1, point 2).
    /// </remarks>
    public class SqlServerConfigurationRepository : ITwConfigurationRepository
    {
        public Task<TwConfigurationEntries> GetConfigurationEntryValuesByGroupName(string groupName)
            => throw new NotImplementedException();

        public Task<List<TwTheme>> GetAllThemes()
            => throw new NotImplementedException();

        public Task<TwWikiDatabaseStatistics> GetWikiDatabaseMetrics()
            => throw new NotImplementedException();

        public Task<bool> IsFirstRun()
            => throw new NotImplementedException();

        public Task<bool> GetCryptoCheck()
            => throw new NotImplementedException();

        public Task SetCryptoCheck()
            => throw new NotImplementedException();

        public Task SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
            => throw new NotImplementedException();

        public Task<List<TwConfigurationNest>> GetConfigurationNest()
            => throw new NotImplementedException();

        public Task<List<TwConfigurationFlat>> GetFlatConfiguration()
            => throw new NotImplementedException();

        public Task<string?> GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
            => throw new NotImplementedException();

        public Task<T?> Get<T>(string groupName, string entryName)
            => throw new NotImplementedException();

        public Task<T> Get<T>(string groupName, string entryName, T defaultValue)
            => throw new NotImplementedException();

        public Task<List<TwMenuItem>> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null)
            => throw new NotImplementedException();

        public Task<TwMenuItem> GetMenuItemById(int id)
            => throw new NotImplementedException();

        public Task DeleteMenuItemById(int id)
            => throw new NotImplementedException();

        public Task<int> UpdateMenuItemById(TwMenuItem menuItem)
            => throw new NotImplementedException();

        public Task<int> InsertMenuItem(TwMenuItem menuItem)
            => throw new NotImplementedException();
    }
}
