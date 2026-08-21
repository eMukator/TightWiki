using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TightWiki.Data.EfCore.Repositories;
using TightWiki.Data.EfCore.Seeding;
using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Data.EfCore.Postgres
{
    /// <summary>
    /// PostgreSQL/EF Core implementation of <see cref="ITwDatabaseManager"/> and <see cref="ISpannedRepository"/> -
    /// the third, build-time-selectable implementation alongside the SQLite
    /// <c>TightWiki.Repository.Helpers.DatabaseManager</c> and <c>TightWiki.Data.EfCore.SqlServer.SqlServerDatabaseManager</c>
    /// (see Database-Providers-Plan.md chapter 4.2, phase 3). Will be selected instead of those two by
    /// <c>Program.cs</c> when built with <c>-p:DataProvider=Postgres</c> once the wiring lands (phase 3.7) - not
    /// referenced from <c>Program.cs</c> yet.
    /// </summary>
    /// <remarks>
    /// This is the phase 3.2 skeleton: the constructor, <see cref="Logger"/>, every repository property
    /// (wired directly to the shared, provider-agnostic <c>Ef*Repository</c> classes in
    /// <c>TightWiki.Data.EfCore</c> - already real, same as <see cref="TightWiki.Data.EfCore.SqlServer.SqlServerDatabaseManager"/>
    /// wires them), <see cref="CreateDbContext"/>/<see cref="CreateApplicationDbContext"/>, and
    /// <see cref="InitializeSchema"/> are real. Every <see cref="ISpannedRepository"/> member (phase 3.5) and
    /// <see cref="ApplyAllSeedData"/> (phase 3.6) are still <see cref="NotImplementedException"/> stubs - mirrors
    /// how <c>SqlServerDatabaseManager</c> looked right after its own phase 2a.1/2a.2, before those follow-up
    /// phases filled them in.
    /// </remarks>
    public sealed class PostgresDatabaseManager : ITwDatabaseManager, ISpannedRepository
    {
        /// <summary>
        /// See <see cref="ITwDatabaseManager.Logger"/>. A plain console logger for the whole lifetime of this
        /// phase 3.2 skeleton - unlike <c>SqlServerDatabaseManager</c>, this is deliberately <b>not</b> promoted
        /// to <see cref="TightWiki.Library.DatabaseLogger"/> after <see cref="LoggingRepository"/> is constructed
        /// below. <see cref="EfLoggingRepository"/> itself is real (shared, provider-agnostic, already
        /// implemented in <c>TightWiki.Data.EfCore</c> - see <see cref="LoggingRepository"/>'s wiring), but
        /// promoting <see cref="Logger"/> to write through it would mean every log call - including from
        /// <see cref="InitializeSchema"/> itself - immediately depends on <see cref="TightWikiDbContext"/>'s
        /// Logging schema actually existing and being migrated, which is exactly the ordering
        /// <c>SqlServerDatabaseManager</c> gets away with only because its own <c>InitializeSchema</c> has already
        /// been battle-tested end to end (phases 2a.1-2a.10). This driver's <see cref="InitializeSchema"/> has not
        /// - it is still unverified against a live PostgreSQL instance beyond construction/connectivity (see
        /// Database-Providers-Plan.md phase 3.2's task notes) - so staying on the always-available
        /// <see cref="ConsoleLogger"/> here avoids a bootstrap dependency this phase cannot yet fully exercise.
        /// The promotion can be revisited once phase 3.4's migrations and phase 3.6's seeding have proven
        /// <see cref="InitializeSchema"/> end to end, same as SQL Server's did.
        /// </summary>
        public ILogger Logger { get; private set; }

        public EfConfigurationRepository ConfigurationRepository { get; private set; }
        ITwConfigurationRepository ITwDatabaseManager.ConfigurationRepository => ConfigurationRepository;

        public EfDefaultsRepository DefaultsRepository { get; private set; }
        ITwDefaultsRepository ITwDatabaseManager.DefaultsRepository => DefaultsRepository;

        public EfEmojiRepository EmojiRepository { get; private set; }
        ITwEmojiRepository ITwDatabaseManager.EmojiRepository => EmojiRepository;

        public EfLoggingRepository LoggingRepository { get; private set; }
        ITwLoggingRepository ITwDatabaseManager.LoggingRepository => LoggingRepository;

        public EfPageRepository PageRepository { get; private set; }
        ITwPageRepository ITwDatabaseManager.PageRepository => PageRepository;

        public EfStatisticsRepository StatisticsRepository { get; private set; }
        ITwStatisticsRepository ITwDatabaseManager.StatisticsRepository => StatisticsRepository;

        public EfUsersRepository UsersRepository { get; private set; }
        ITwUsersRepository ITwDatabaseManager.UsersRepository => UsersRepository;

        /// <summary>
        /// The single new configuration key introduced for the EF Core providers (Database-Providers-Plan.md
        /// chapter 7, "Rozhodnuto" - "Connection string klíč: ConnectionStrings:TightWikiEfCore"), shared across
        /// every EF Core driver project including this one.
        /// </summary>
        private readonly string _connectionString;

        public PostgresDatabaseManager(IConfiguration configuration)
        {
            Logger = new ConsoleLogger();

            _connectionString = configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore', which is required when built with -p:DataProvider=Postgres.");

            //Every repository below is the same shared, provider-agnostic Ef*Repository class SqlServerDatabaseManager
            //wires (TightWiki.Data.EfCore) - none of it is SQL Server specific, so there is nothing left to stub
            //here even though this driver's own InitializeSchema/ISpannedRepository members are still incomplete.
            ConfigurationRepository = new EfConfigurationRepository(CreateDbContext, CreateApplicationDbContext);
            DefaultsRepository = new EfDefaultsRepository();
            LoggingRepository = new EfLoggingRepository(CreateDbContext, ConfigurationRepository);
            EmojiRepository = new EfEmojiRepository(CreateDbContext, ConfigurationRepository);
            PageRepository = new EfPageRepository(CreateDbContext, ConfigurationRepository);
            StatisticsRepository = new EfStatisticsRepository(CreateDbContext, ConfigurationRepository);
            UsersRepository = new EfUsersRepository(CreateDbContext, CreateApplicationDbContext, ConfigurationRepository);
        }

        /// <summary>
        /// Creates a new <see cref="TightWikiDbContext"/> configured against the PostgreSQL connection string.
        /// Callers are responsible for disposing the returned context.
        /// </summary>
        private TightWikiDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TightWikiDbContext>();
            //Migrations for this shared context will live in this driver project's own assembly (phase 3.4), not
            //TightWiki.Data.EfCore's, and its MigrationsHistoryTable is explicit and distinct from
            //ApplicationDbContext's (PostgresMigrationsHistory) - same reasoning as the SQL Server driver's
            //CreateDbContext/TightWikiDbContextFactory.
            optionsBuilder.UseNpgsql(_connectionString,
                b => b.MigrationsAssembly(typeof(PostgresDatabaseManager).Assembly.GetName().Name)
                      .MigrationsHistoryTable(PostgresMigrationsHistory.TightWikiDbTableName, PostgresMigrationsHistory.TightWikiDbSchema));
            return new TightWikiDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a new <see cref="ApplicationDbContext"/> (ASP.NET Core Identity, Database-Providers-Plan.md
        /// chapter 4.1.1) configured against the same PostgreSQL connection string as <see cref="CreateDbContext"/>.
        /// Callers are responsible for disposing the returned context.
        /// </summary>
        private ApplicationDbContext CreateApplicationDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //Migrations for this shared context will live in this driver project's own assembly (phase 3.4), not
            //TightWiki.Library's, and its MigrationsHistoryTable is explicit and distinct from TightWikiDbContext's
            //(PostgresMigrationsHistory) - same reasoning as the SQL Server driver's CreateApplicationDbContext/
            //ApplicationDbContextFactory.
            optionsBuilder.UseNpgsql(_connectionString,
                b => b.MigrationsAssembly(typeof(PostgresDatabaseManager).Assembly.GetName().Name)
                      .MigrationsHistoryTable(PostgresMigrationsHistory.ApplicationDbTableName, PostgresMigrationsHistory.ApplicationDbSchema));
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// EF Core implementation of <see cref="ITwDatabaseManager.InitializeSchema"/>: applies any pending EF Core
        /// migrations via <see cref="Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync"/>
        /// (Database-Providers-Plan.md chapter 4.2), same two-context ordering as
        /// <c>SqlServerDatabaseManager.InitializeSchema</c> - see its remarks for why
        /// <see cref="ApplicationDbContext"/> (Identity) migrates before <see cref="TightWikiDbContext"/>.
        /// </summary>
        /// <remarks>
        /// One PostgreSQL-specific step not present on the SQL Server driver: immediately before
        /// <see cref="TightWikiDbContext"/>'s migrations run, this ensures the <c>citext</c> extension exists
        /// (<c>CREATE EXTENSION IF NOT EXISTS citext;</c>, idempotent - safe even if this runs on every startup
        /// with nothing pending). <c>citext</c> is required by the case-insensitive-text columns phase 3.3 adds to
        /// the shared EF model; without the extension present first, phase 3.4's migrations (which create those
        /// columns) would fail the first time they run against a fresh database. No migrations exist yet as of
        /// phase 3.2 - <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.GetPendingMigrationsAsync"/>
        /// against an empty <see cref="Microsoft.EntityFrameworkCore.Migrations.MigrationsAssemblyAttribute"/>
        /// simply reports none pending, so <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.MigrateAsync"/>
        /// - and therefore the <c>CREATE EXTENSION</c> statement below - is not yet reachable in practice; this
        /// exists so it is already wired correctly once phase 3.4 adds the first migration.
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
                //Must run before MigrateAsync below - see this method's remarks. IF NOT EXISTS makes this safe to
                //repeat on every upgrade that has pending TightWikiDbContext migrations, not just the very first.
                await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS citext;");

                await context.Database.MigrateAsync();
            }

            return wasIdentitySchemaUpgraded || wasWikiSchemaUpgraded;
        }

        /// <summary>
        /// Not implemented yet - lands in phase 3.6 (Database-Providers-Plan.md), mirroring
        /// <c>SqlServerDatabaseManager.ApplyAllSeedData</c> (phase 2a.5/2a.10).
        /// </summary>
        public Task ApplyAllSeedData(ITwSharedLocalizationText localizer, UserManager<IdentityUser> userManager,
            ITwEngine tightEngine, TwDefaultDataType[] defaultDataTypes)
            => throw new NotImplementedException();

        #region Database admin - ISpannedRepository / ITwDatabaseManager. Not implemented yet - lands in phase 3.5.

        public Task<string> VacuumDatabase(string databaseName) => throw new NotImplementedException();

        public Task<string> OptimizeDatabase(string databaseName) => throw new NotImplementedException();

        public Task<string> IntegrityCheckDatabase(string databaseName) => throw new NotImplementedException();

        public Task<string> ForeignKeyCheck(string databaseName) => throw new NotImplementedException();

        public Task<List<(string Name, string Version)>> GetDatabaseVersions() => throw new NotImplementedException();

        public Task<List<(string Name, int PageCount)>> GetDatabasePageCounts() => throw new NotImplementedException();

        public Task<List<(string Name, int PageSize)>> GetDatabasePageSizes() => throw new NotImplementedException();

        #endregion
    }
}
