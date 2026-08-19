using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data.Common;
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
    /// <see cref="InitializeSchema"/>, its <c>ApplicationDbContext</c>/<c>TightWikiDbContext</c> migrations,
    /// <see cref="DefaultsRepository"/>, and every <see cref="ISpannedRepository"/> member (phase 2a.4 - vendor-native
    /// MSSQL maintenance operations, Database-Providers-Plan.md chapter 4.4) are real. The six business
    /// repositories (<see cref="ConfigurationRepository"/> and friends) are still stubs that throw
    /// <see cref="NotImplementedException"/> until phases 2a.6-2a.9/2b land.
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
        /// The 8 schemas <see cref="TightWikiDbContext"/> spans (Database-Providers-Plan.md chapter 4.3), in the
        /// same order as the SQLite <c>DatabaseManager.Databases</c> array
        /// (<c>TightWiki.Repository/Helpers/DatabaseManager.cs</c>) that they replace one-for-one for the purposes
        /// of <see cref="GetDatabasePageCounts"/>/<see cref="GetDatabasePageSizes"/> (chapter 4.4, "Admin obrazovka
        /// „Database"" - "Doporučení: per schéma"). Does <b>not</b> include <c>ApplicationDbContext</c>'s own
        /// schema ("Users" is shared between the two contexts, see <see cref="SqlServerMigrationsHistory"/>) or the
        /// default "dbo" schema that only holds <see cref="TightWikiDbContext"/>'s migrations-history table.
        /// </summary>
        private static readonly string[] TargetSchemas =
        [
            "DeletedPageRevisions", "DeletedPages", "Pages", "Statistics", "Emoji", "Logging", "Users", "Config"
        ];

        /// <summary>
        /// SQL Server's on-disk page size. Unlike SQLite (where <c>PRAGMA page_size</c> is a per-file setting
        /// queried at runtime, see <c>GetDatabasePageSize.sql</c>), SQL Server's page size is a fixed architectural
        /// constant - 8 KB for every database, table, and index, on every supported edition and version - there is
        /// no per-schema or per-object override and nothing to query it from. See
        /// https://learn.microsoft.com/sql/relational-databases/pages-and-extents-architecture-guide.
        /// </summary>
        private const int SqlServerPageSizeBytes = 8192;

        /// <summary>
        /// Opens (and, via the <c>using</c> at each call site's <see langword="await using"/>, closes) a
        /// dedicated <see cref="TightWikiDbContext"/>-backed ADO.NET connection for the raw DBCC/DMV statements
        /// below - none of these are expressible as LINQ, and mixing raw ADO.NET access with EF Core's own
        /// connection management via <c>Database.OpenConnectionAsync</c>/<c>CloseConnectionAsync</c> (rather than
        /// opening the underlying <see cref="DbConnection"/> directly) is the documented-safe way to do that.
        /// </summary>
        private async Task<(TightWikiDbContext Context, DbConnection Connection)> OpenAdminConnection()
        {
            var context = CreateDbContext();
            await context.Database.OpenConnectionAsync();
            return (context, context.Database.GetDbConnection());
        }

        /// <summary>
        /// SQLite's <c>VACUUM</c> (<c>VacuumDatabase.sql</c>) rebuilds the entire file into a fresh copy,
        /// defragmenting storage and reclaiming free pages. SQL Server has no single-statement equivalent, so this
        /// implements the safer of the two realistic candidates:
        /// <list type="bullet">
        /// <item><description><c>ALTER INDEX ALL ... REBUILD</c> per table (chosen) - defragments and compacts
        /// storage in place, is safe to run online/regularly, and is what Microsoft's own maintenance guidance
        /// recommends for this purpose.</description></item>
        /// <item><description><c>DBCC SHRINKDATABASE</c> (deliberately <b>not</b> chosen) - physically returns
        /// free space to the OS, which sounds closer to SQLite's file-shrinking behavior, but Microsoft's own
        /// documentation recommends against it: it aggressively fragments every index in the database (the
        /// opposite of "optimized storage"), the reclaimed space is very likely to be needed again immediately
        /// (data files just grow back, which is itself an expensive operation), and it is explicitly discouraged
        /// as a routine/scheduled operation - see
        /// https://learn.microsoft.com/troubleshoot/sql/database-engine/database-file-operations/considerations-for-shrink-database-file.
        /// </description></item>
        /// </list>
        /// Runs across every table in <see cref="TargetSchemas"/> regardless of the <paramref name="databaseName"/>
        /// argument - after schema consolidation (chapter 4.3) there is exactly one physical MSSQL database, so
        /// there is nothing left for that argument to meaningfully select between; it is only still accepted to
        /// satisfy the shared <see cref="ISpannedRepository"/> signature that <c>AdminController</c> calls through.
        /// </summary>
        public async Task<string> VacuumDatabase(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var tables = new List<(string Schema, string Table)>();
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using (var listCommand = connection.CreateCommand())
                    {
                        listCommand.CommandText = $"""
                            SELECT s.name AS SchemaName, t.name AS TableName
                            FROM sys.tables t
                            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                            WHERE t.is_ms_shipped = 0 AND s.name IN ({schemaList})
                            ORDER BY s.name, t.name;
                            """;
                        using var reader = await listCommand.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            tables.Add((reader.GetString(0), reader.GetString(1)));
                        }
                    }

                    foreach (var (schema, table) in tables)
                    {
                        using var rebuildCommand = connection.CreateCommand();
                        rebuildCommand.CommandTimeout = 300; //Index rebuilds can run long on larger tables.
                        rebuildCommand.CommandText = $"ALTER INDEX ALL ON [{schema}].[{table}] REBUILD;";
                        await rebuildCommand.ExecuteNonQueryAsync();
                    }

                    return $"Rebuilt all indexes on {tables.Count} table(s) across {TargetSchemas.Length} schema(s).";
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA optimize</c> (<c>OptimizeDatabase.sql</c>) refreshes query-planner statistics. The
        /// direct MSSQL equivalent is <c>sp_updatestats</c>, which updates statistics for every table/indexed view
        /// in the database in one call - simpler and safer to run unattended than issuing per-table
        /// <c>UPDATE STATISTICS</c> statements, and (like <c>PRAGMA optimize</c>) cheap enough to run routinely
        /// since it skips tables whose statistics are already up to date. Like <see cref="VacuumDatabase"/>, this
        /// always operates on the whole database - <paramref name="databaseName"/> is accepted only for interface
        /// compatibility.
        /// </summary>
        public async Task<string> OptimizeDatabase(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandTimeout = 300;
                    command.CommandText = "EXEC sp_updatestats;";
                    await command.ExecuteNonQueryAsync();

                    return "sp_updatestats completed - statistics refreshed for every table in the database.";
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA integrity_check</c> (<c>IntegrityCheckDatabase.sql</c>) is the direct analogue of
        /// <c>DBCC CHECKDB</c> - both are read-only structural/corruption checks, deliberately run without any
        /// repair option (admin "Verify" action, Database-Providers-Plan.md chapter 4.4 point 1: "bez REPAIR").
        /// Unlike the SQLite <c>DatabaseManager.IntegrityCheckDatabase</c>, this does <b>not</b> also append
        /// <see cref="ForeignKeyCheck"/>'s result - that concatenation on the SQLite side is a pre-existing bug
        /// (it appends an unawaited <see cref="Task{TResult}"/>'s <see cref="object.ToString"/>, not the actual
        /// check result) that is out of scope to port here; <c>ForeignKeyCheck</c> is exposed as, and stays, its
        /// own <see cref="ISpannedRepository"/> member. <c>DBCC CHECKDB</c> reports structural corruption via a
        /// thrown <see cref="SqlException"/> (severity &gt;= 16), not a result set, so success/failure is
        /// distinguished by catching it - mirroring <c>PRAGMA integrity_check</c> returning descriptive text
        /// rather than throwing. Whole-database, like <see cref="VacuumDatabase"/> -
        /// <paramref name="databaseName"/> is accepted only for interface compatibility.
        /// </summary>
        public async Task<string> IntegrityCheckDatabase(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandTimeout = 300; //DBCC CHECKDB can run long on larger databases.
                    command.CommandText = "DBCC CHECKDB WITH NO_INFOMSGS, ALL_ERRORMSGS;";

                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        return "DBCC CHECKDB completed - no corruption or structural issues found.";
                    }
                    catch (SqlException ex)
                    {
                        var messages = ex.Errors.Cast<SqlError>().Select(e => e.Message);
                        return "DBCC CHECKDB reported issues:\r\n" + string.Join("\r\n", messages);
                    }
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA foreign_key_check</c> (<c>ForeignKeyCheck.sql</c>) lists rows that violate a foreign
        /// key. <c>DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS</c> is the MSSQL equivalent extended to also cover
        /// <c>CHECK</c> constraints (Database-Providers-Plan.md chapter 4.4 point 2: "ověří FK i CHECK constraints
        /// v celé databázi") - run with no table/constraint name argument, it checks every constraint in the
        /// current database, matching "v celé databázi". Unlike <c>DBCC CHECKDB</c>, violations are reported as an
        /// ordinary result set (columns "Table" / "Constraint" / "Where" - confirmed against a live LocalDB
        /// instance; despite what several online references claim, they are not "Table Name" / "Constraint
        /// Name"), not by throwing, so no try/catch is needed here. Whole-database, like the other three members -
        /// <paramref name="databaseName"/> is accepted only for interface compatibility.
        /// </summary>
        public async Task<string> ForeignKeyCheck(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandTimeout = 300;
                    command.CommandText = "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS, ALL_ERRORMSGS;";

                    var violations = new List<string>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tableName = reader["Table"]?.ToString() ?? string.Empty;
                            var constraintName = reader["Constraint"]?.ToString() ?? string.Empty;
                            var where = reader["Where"]?.ToString() ?? string.Empty;
                            violations.Add($"{tableName} / {constraintName}: {where}");
                        }
                    }

                    return violations.Count == 0
                        ? "DBCC CHECKCONSTRAINTS reported no foreign key or check constraint violations."
                        : string.Join("\r\n", violations);
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>GetDatabaseVersion.sql</c> (<c>SELECT SQLITE_VERSION();</c>) reports the same SQLite engine
        /// version for all 8 "databases" - there is no per-database/per-schema schema-version concept on that
        /// side either. For the EF providers, the more informative and genuinely per-"database" analogue is each
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/>'s own applied-migrations history (Database-
        /// Providers-Plan.md chapter 4.4/task 2a.4: "u EF/MSSQL je ekvivalentní zdroj pravdy historie aplikovaných
        /// EF Core migrací") - read from the two distinct <c>__EFMigrationsHistory_*</c> tables
        /// (<see cref="SqlServerMigrationsHistory"/>, chapter 2a.2), reporting the most recently applied
        /// <c>MigrationId</c> for each of the two <see cref="Microsoft.EntityFrameworkCore.DbContext"/>s this
        /// driver manages (<c>ApplicationDbContext</c> for Identity, <c>TightWikiDbContext</c> for everything
        /// else). <c>MigrationId</c> is timestamp-prefixed (<c>yyyyMMddHHmmss_Name</c>), so ordering it as a string
        /// is equivalent to ordering chronologically. Returns one row per <see cref="TargetSchemas"/> entry - the
        /// same 8 schema names <see cref="GetDatabasePageCounts"/>/<see cref="GetDatabasePageSizes"/> use - so that
        /// <c>AdminController.Database()</c>'s <c>FirstOrDefault(o => o.Name == version.Name)</c> join across all
        /// three methods actually matches instead of silently falling through to a zeroed-out
        /// <c>PageCount</c>/<c>PageSize</c>/<c>DatabaseSize</c> for any row.
        /// </summary>
        /// <remarks>
        /// 7 of the 8 schemas (everything except "Users") are owned exclusively by
        /// <see cref="TightWikiDbContext"/>, so they all report the same value - the most recently applied
        /// <c>MigrationId</c> from <c>dbo.__EFMigrationsHistory_TightWikiDb</c>. "Users" is the one schema shared
        /// between <see cref="TightWikiDbContext"/> (which spans all 8 schemas, see
        /// <see cref="SqlServerMigrationsHistory.TightWikiDbTableName"/>) and
        /// <see cref="global::TightWiki.Library.ApplicationDbContext"/> (ASP.NET Core Identity) - each context has
        /// its own, independently versioned migration history, so that one row reports both rather than picking
        /// (and silently discarding information about) just one, formatted as
        /// <c>"TightWikiDb: &lt;migration&gt; / Identity: &lt;migration&gt;"</c>.
        /// </remarks>
        public async Task<List<(string Name, string Version)>> GetDatabaseVersions()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var applicationDbVersion = await GetLatestMigrationId(connection,
                        SqlServerMigrationsHistory.ApplicationDbSchema, SqlServerMigrationsHistory.ApplicationDbTableName);
                    var tightWikiDbVersion = await GetLatestMigrationId(connection,
                        "dbo", SqlServerMigrationsHistory.TightWikiDbTableName);
                    var usersVersion = $"TightWikiDb: {tightWikiDbVersion} / Identity: {applicationDbVersion}";

                    return TargetSchemas
                        .Select(schema => (schema, schema == "Users" ? usersVersion : tightWikiDbVersion))
                        .ToList();
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        private static async Task<string> GetLatestMigrationId(DbConnection connection, string schema, string table)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT TOP (1) [MigrationId] FROM [{schema}].[{table}] ORDER BY [MigrationId] DESC;";
            var result = await command.ExecuteScalarAsync();
            return result as string ?? string.Empty;
        }

        /// <summary>
        /// SQLite's <c>PRAGMA page_count</c> (<c>GetDatabasePageCount.sql</c>) reports the total page count of a
        /// whole database file. Post-consolidation (chapter 4.3) there is one physical MSSQL database, so this
        /// reports the per-<b>schema</b> equivalent instead (chapter 4.4, "Admin obrazovka „Database""), summing
        /// <c>sys.dm_db_partition_stats.reserved_page_count</c> (total pages reserved for a table's data + all its
        /// indexes, across all partitions - the closest MSSQL analogue of "space this schema's data occupies on
        /// disk") for every user table per schema in <see cref="TargetSchemas"/>. <c>LEFT JOIN</c>+<c>COALESCE</c>
        /// so a schema with no tables yet still gets a row with <c>PageCount = 0</c>, keeping the "8 rows" shape
        /// <c>AdminController</c> (unmodified) expects.
        /// </summary>
        public async Task<List<(string Name, int PageCount)>> GetDatabasePageCounts()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using var command = connection.CreateCommand();
                    command.CommandText = $"""
                        SELECT s.name AS SchemaName, COALESCE(SUM(ps.reserved_page_count), 0) AS PageCount
                        FROM sys.schemas s
                        LEFT JOIN sys.tables t ON t.schema_id = s.schema_id AND t.is_ms_shipped = 0
                        LEFT JOIN sys.dm_db_partition_stats ps ON ps.object_id = t.object_id
                        WHERE s.name IN ({schemaList})
                        GROUP BY s.name
                        ORDER BY s.name;
                        """;

                    var results = new List<(string, int)>();
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        results.Add((reader.GetString(0), Convert.ToInt32(reader.GetInt64(1))));
                    }
                    return results;
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA page_size</c> (<c>GetDatabasePageSize.sql</c>) is a per-database-file setting.
        /// SQL Server has no equivalent to query - <see cref="SqlServerPageSizeBytes"/> is a fixed 8 KB constant
        /// for every database/table/index, so this just pairs it with each of the 8 <see cref="TargetSchemas"/>
        /// names, matching the "8 rows" shape <see cref="GetDatabasePageCounts"/> returns (so that
        /// <c>PageCount * PageSize</c>, as computed by <c>AdminController.Database()</c>, yields each schema's
        /// approximate on-disk size in bytes). No database round-trip is needed for a compile-time constant.
        /// </summary>
        public Task<List<(string Name, int PageSize)>> GetDatabasePageSizes()
            => Task.FromResult(TargetSchemas.Select(name => (name, SqlServerPageSizeBytes)).ToList());

        #endregion
    }
}
