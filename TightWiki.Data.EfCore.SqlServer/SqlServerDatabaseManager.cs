using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TightWiki.Data.EfCore.Seeding;
using TightWiki.Data.EfCore.SqlServer.Repositories;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Data.EfCore.SqlServer
{
    /// <summary>
    /// MSSQL/EF Core implementation of <see cref="ITwDatabaseManager"/> and <see cref="ISpannedRepository"/> - the
    /// second, build-time-selectable implementation alongside the SQLite <c>TightWiki.Repository.Helpers.DatabaseManager</c>
    /// (see Database-Providers-Plan.md chapter 4.2). Selected instead of the SQLite implementation by <c>Program.cs</c>
    /// when built with <c>-p:DataProvider=SqlServer</c> (<c>SQLSERVER_PROVIDER</c>).
    /// </summary>
    /// <remarks>
    /// Phase 2a.1 skeleton only (Database-Providers-Plan.md, phase 2a). <see cref="InitializeSchema"/> and
    /// <see cref="DefaultsRepository"/> are real; the six business repositories and every <see cref="ISpannedRepository"/>
    /// member are stubs that throw <see cref="NotImplementedException"/> until phases 2a.4/2a.6-2a.9/2b land.
    /// </remarks>
    public class SqlServerDatabaseManager : ITwDatabaseManager, ISpannedRepository
    {
        /// <summary>
        /// See <see cref="ITwDatabaseManager.Logger"/>. Always a plain console logger for now - unlike the SQLite
        /// <c>DatabaseManager</c>, there is no working <see cref="ITwLoggingRepository"/> yet to promote to a
        /// database-backed logger (see <see cref="LoggingRepository"/>).
        /// </summary>
        public ILogger Logger { get; private set; }

        public SqlServerConfigurationRepository ConfigurationRepository { get; private set; }
        ITwConfigurationRepository ITwDatabaseManager.ConfigurationRepository => ConfigurationRepository;

        public EfDefaultsRepository DefaultsRepository { get; private set; }
        ITwDefaultsRepository ITwDatabaseManager.DefaultsRepository => DefaultsRepository;

        public SqlServerEmojiRepository EmojiRepository { get; private set; }
        ITwEmojiRepository ITwDatabaseManager.EmojiRepository => EmojiRepository;

        public SqlServerLoggingRepository LoggingRepository { get; private set; }
        ITwLoggingRepository ITwDatabaseManager.LoggingRepository => LoggingRepository;

        public SqlServerPageRepository PageRepository { get; private set; }
        ITwPageRepository ITwDatabaseManager.PageRepository => PageRepository;

        public SqlServerStatisticsRepository StatisticsRepository { get; private set; }
        ITwStatisticsRepository ITwDatabaseManager.StatisticsRepository => StatisticsRepository;

        public SqlServerUsersRepository UsersRepository { get; private set; }
        ITwUsersRepository ITwDatabaseManager.UsersRepository => UsersRepository;

        /// <summary>
        /// The single new configuration key introduced for the EF Core providers (Database-Providers-Plan.md
        /// chapter 7, "Rozhodnuto" - "Connection string klíč: ConnectionStrings:TightWikiEfCore").
        /// </summary>
        private readonly string _connectionString;

        public SqlServerDatabaseManager(IConfiguration configuration)
        {
            Logger = new ConsoleLogger();

            _connectionString = configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore', which is required when built with -p:DataProvider=SqlServer.");

            ConfigurationRepository = new SqlServerConfigurationRepository();
            DefaultsRepository = new EfDefaultsRepository();
            EmojiRepository = new SqlServerEmojiRepository();
            LoggingRepository = new SqlServerLoggingRepository();
            PageRepository = new SqlServerPageRepository();
            StatisticsRepository = new SqlServerStatisticsRepository();
            UsersRepository = new SqlServerUsersRepository();
        }

        /// <summary>
        /// Creates a new <see cref="TightWikiDbContext"/> configured against the MSSQL connection string. Callers
        /// are responsible for disposing the returned context.
        /// </summary>
        private TightWikiDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TightWikiDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);
            return new TightWikiDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// EF Core implementation of <see cref="ITwDatabaseManager.InitializeSchema"/>: applies any pending EF Core
        /// migrations via <see cref="Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync"/>
        /// (Database-Providers-Plan.md chapter 4.2). No migrations exist yet as of phase 2a.1 (they land in
        /// phase 2a.2), so this currently always finds zero pending migrations and returns false - the same "no
        /// pending changes" outcome it will report once migrations exist and are already applied.
        /// </summary>
        public async Task<bool> InitializeSchema()
        {
            using var context = CreateDbContext();

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (!pendingMigrations.Any())
            {
                return false;
            }

            await context.Database.MigrateAsync();
            return true;
        }

        /// <summary>
        /// Not implemented yet - seeding a freshly migrated MSSQL database is wired up once the four repositories
        /// it depends on (<see cref="ConfigurationRepository"/>, <see cref="PageRepository"/>) have real
        /// implementations (phases 2a.6-2a.9).
        /// </summary>
        public Task ApplyAllSeedData(ITwSharedLocalizationText localizer, UserManager<IdentityUser> userManager,
            ITwEngine tightEngine, TwDefaultDataType[] defaultDataTypes)
            => throw new NotImplementedException();

        #region Database admin - ISpannedRepository / ITwDatabaseManager.

        /// <summary>
        /// Not implemented yet - vendor-native maintenance operations (Database-Providers-Plan.md chapter 4.4,
        /// e.g. DBCC CHECKDB) land in phase 2a.4.
        /// </summary>
        public Task<string> VacuumDatabase(string databaseName)
            => throw new NotImplementedException();

        public Task<string> OptimizeDatabase(string databaseName)
            => throw new NotImplementedException();

        public Task<string> IntegrityCheckDatabase(string databaseName)
            => throw new NotImplementedException();

        public Task<string> ForeignKeyCheck(string databaseName)
            => throw new NotImplementedException();

        public Task<List<(string Name, string Version)>> GetDatabaseVersions()
            => throw new NotImplementedException();

        public Task<List<(string Name, int PageCount)>> GetDatabasePageCounts()
            => throw new NotImplementedException();

        public Task<List<(string Name, int PageSize)>> GetDatabasePageSizes()
            => throw new NotImplementedException();

        #endregion
    }
}
