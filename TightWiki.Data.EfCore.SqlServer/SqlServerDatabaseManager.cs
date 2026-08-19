using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TightWiki.Data.EfCore.Seeding;
using TightWiki.Data.EfCore.SqlServer.Repositories;
using TightWiki.Library;
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
    /// Phase 2a.1/2a.2 skeleton (Database-Providers-Plan.md, phase 2a). <see cref="InitializeSchema"/>, its
    /// <c>ApplicationDbContext</c>/<c>TightWikiDbContext</c> migrations, and <see cref="DefaultsRepository"/> are
    /// real; the six business repositories and every <see cref="ISpannedRepository"/> member are stubs that
    /// throw <see cref="NotImplementedException"/> until phases 2a.4/2a.6-2a.9/2b land.
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
            //See the matching comment in TightWikiDbContextFactory - migrations for this shared context live in
            //this driver project's assembly, not TightWiki.Data.EfCore's, and its MigrationsHistoryTable is
            //explicit and distinct from ApplicationDbContext's (SqlServerMigrationsHistory).
            optionsBuilder.UseSqlServer(_connectionString,
                b => b.MigrationsAssembly(typeof(SqlServerDatabaseManager).Assembly.GetName().Name)
                      .MigrationsHistoryTable(SqlServerMigrationsHistory.TightWikiDbTableName));
            return new TightWikiDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a new <see cref="ApplicationDbContext"/> (ASP.NET Core Identity, Database-Providers-Plan.md
        /// chapter 4.1.1) configured against the same MSSQL connection string as <see cref="CreateDbContext"/>.
        /// Callers are responsible for disposing the returned context.
        /// </summary>
        private ApplicationDbContext CreateApplicationDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //See the matching comment in ApplicationDbContextFactory - migrations for this shared context live
            //in this driver project's assembly, not TightWiki.Library's, and its MigrationsHistoryTable is
            //explicit and distinct from TightWikiDbContext's (SqlServerMigrationsHistory).
            optionsBuilder.UseSqlServer(_connectionString,
                b => b.MigrationsAssembly(typeof(SqlServerDatabaseManager).Assembly.GetName().Name)
                      .MigrationsHistoryTable(SqlServerMigrationsHistory.ApplicationDbTableName, SqlServerMigrationsHistory.ApplicationDbSchema));
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// EF Core implementation of <see cref="ITwDatabaseManager.InitializeSchema"/>: applies any pending EF Core
        /// migrations via <see cref="Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync"/>
        /// (Database-Providers-Plan.md chapter 4.2).
        /// </summary>
        /// <remarks>
        /// <see cref="ApplicationDbContext"/> (Identity) migrations are applied first, then
        /// <see cref="TightWikiDbContext"/>'s - per chapter 4.1.1 ("Migrace ApplicationDbContext musí běžet před
        /// migracemi TightWiki modelu") and the open question in chapter 8. Note that in the EF model itself
        /// <c>Users.Profile.UserId</c> is <b>not</b> a declared foreign key against <c>AspNetUsers.Id</c> - see
        /// the remarks on <c>Entities.Users.Profile</c> - it is only a logical/matching-by-convention link,
        /// because the two tables live in separate <see cref="Microsoft.EntityFrameworkCore.DbContext"/>s and EF
        /// Core cannot declare a cross-context FK constraint. So this ordering is not enforced by any FK EF would
        /// otherwise fail to create; it is kept anyway both because the plan calls for it and because it is the
        /// only sane bootstrap order (an application with no Identity tables yet has no users to run as).
        /// </remarks>
        public async Task<bool> InitializeSchema()
        {
            using var identityContext = CreateApplicationDbContext();
            var identityPendingMigrations = await identityContext.Database.GetPendingMigrationsAsync();
            var wasIdentitySchemaUpgraded = identityPendingMigrations.Any();
            if (wasIdentitySchemaUpgraded)
            {
                await identityContext.Database.MigrateAsync();
            }

            using var context = CreateDbContext();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var wasWikiSchemaUpgraded = pendingMigrations.Any();
            if (wasWikiSchemaUpgraded)
            {
                await context.Database.MigrateAsync();
            }

            return wasIdentitySchemaUpgraded || wasWikiSchemaUpgraded;
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
