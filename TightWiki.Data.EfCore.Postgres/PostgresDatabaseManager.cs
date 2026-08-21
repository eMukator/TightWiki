using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
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
    /// This is the phase 3.2 skeleton, extended in phase 3.5: the constructor, <see cref="Logger"/>, every
    /// repository property (wired directly to the shared, provider-agnostic <c>Ef*Repository</c> classes in
    /// <c>TightWiki.Data.EfCore</c> - already real, same as <see cref="TightWiki.Data.EfCore.SqlServer.SqlServerDatabaseManager"/>
    /// wires them), <see cref="CreateDbContext"/>/<see cref="CreateApplicationDbContext"/>, <see cref="InitializeSchema"/>,
    /// and every <see cref="ISpannedRepository"/> member (phase 3.5 - vendor-native PostgreSQL maintenance
    /// operations, Database-Providers-Plan.md chapter 4.4/4.2) are real. <see cref="ApplyAllSeedData"/> (phase 3.6)
    /// is still a <see cref="NotImplementedException"/> stub - mirrors how <c>SqlServerDatabaseManager</c> looked
    /// right after its own phase 2a.1/2a.2/2a.4, before its own follow-up phases filled the rest in.
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

        #region Database admin - ISpannedRepository / ITwDatabaseManager.

        /// <summary>
        /// The 8 schemas <see cref="TightWikiDbContext"/> spans (Database-Providers-Plan.md chapter 4.3) - the same
        /// 8 names, in the same order, as <c>SqlServerDatabaseManager.TargetSchemas</c> and confirmed against this
        /// driver's own migration (<c>Migrations\TightWikiDb\*_InitialCreate.cs</c>'s <c>EnsureSchema</c> calls).
        /// Does <b>not</b> include <c>ApplicationDbContext</c>'s own migrations-history table's schema ("Users" is
        /// shared between the two contexts, see <see cref="PostgresMigrationsHistory"/>) or the default "public"
        /// schema that only holds <see cref="TightWikiDbContext"/>'s migrations-history table.
        /// </summary>
        private static readonly string[] TargetSchemas =
        [
            "DeletedPageRevisions", "DeletedPages", "Pages", "Statistics", "Emoji", "Logging", "Users", "Config"
        ];

        /// <summary>
        /// Opens (and, via each call site's <see langword="await using"/>, closes) a dedicated
        /// <see cref="TightWikiDbContext"/>-backed ADO.NET connection for the raw admin statements below - none of
        /// these are expressible as LINQ, and mixing raw ADO.NET access with EF Core's own connection management
        /// via <c>Database.OpenConnectionAsync</c>/<c>CloseConnectionAsync</c> (rather than opening the underlying
        /// <see cref="DbConnection"/> directly) is the documented-safe way to do that. Direct analogue of
        /// <c>SqlServerDatabaseManager.OpenAdminConnection</c>.
        /// </summary>
        /// <remarks>
        /// Opening the connection this way does not start any ambient transaction - <c>OpenConnectionAsync</c> only
        /// calls <see cref="DbConnection.OpenAsync()"/> under the hood, nothing more. That matters specifically for
        /// <see cref="VacuumDatabase"/>: PostgreSQL refuses to run <c>VACUUM</c> "inside a transaction block". Every
        /// admin method here issues one statement per <see cref="DbCommand.ExecuteNonQueryAsync()"/>/
        /// <see cref="DbCommand.ExecuteReaderAsync()"/> call rather than concatenating several ';'-separated
        /// statements into one <see cref="DbCommand.CommandText"/> - PostgreSQL's simple query protocol implicitly
        /// wraps a *multi-statement* message in a single transaction (as if <c>BEGIN</c> had been issued), which is
        /// the actual, commonly-hit cause of "VACUUM cannot run inside a transaction block" from ADO.NET/ORM code,
        /// not connection-opening by itself. As long as that one-statement-per-command discipline is kept, the
        /// plain autocommit connection this method returns needs no further configuration (no explicit
        /// <c>SET AUTOCOMMIT</c> concept exists in PostgreSQL/Npgsql - autocommit is simply "no open transaction").
        /// </remarks>
        private async Task<(TightWikiDbContext Context, DbConnection Connection)> OpenAdminConnection()
        {
            var context = CreateDbContext();
            await context.Database.OpenConnectionAsync();
            return (context, context.Database.GetDbConnection());
        }

        /// <summary>
        /// SQLite's <c>VACUUM</c> (<c>VacuumDatabase.sql</c>) rebuilds the entire file into a fresh copy,
        /// reclaiming free space and defragmenting storage; SQL Server's driver runs <c>ALTER INDEX ALL REBUILD</c>
        /// per table for the same purpose (no single-statement whole-database equivalent exists there either).
        /// PostgreSQL, uniquely among the three, has an actual <c>VACUUM</c> statement of its own, so this runs
        /// <c>VACUUM (ANALYZE) "schema"."table";</c> for every table across all 8 <see cref="TargetSchemas"/> - one
        /// statement per table (see <see cref="OpenAdminConnection"/>'s remarks for why that matters), reclaiming
        /// dead-tuple space left behind by updates/deletes and refreshing planner statistics in the same pass
        /// (<c>ANALYZE</c> option), same spirit as SQLite's <c>VACUUM</c> and MSSQL's index rebuild. Deliberately
        /// not <c>VACUUM FULL</c>: that variant requires an exclusive table lock and rewrites the table to a new
        /// file to return space to the OS, which is exactly the kind of disruptive, not-safe-to-run-routinely
        /// operation the SQL Server driver's own doc comment explains why <c>DBCC SHRINKDATABASE</c> was rejected
        /// for - plain <c>VACUUM</c> is safe to run online/regularly and is what PostgreSQL's own maintenance
        /// guidance recommends for this purpose. Runs across every table in <see cref="TargetSchemas"/> regardless
        /// of the <paramref name="databaseName"/> argument - after schema consolidation there is exactly one
        /// physical PostgreSQL database, so there is nothing left for that argument to meaningfully select between;
        /// it is only still accepted to satisfy the shared <see cref="ISpannedRepository"/> signature.
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
                            SELECT n.nspname AS SchemaName, c.relname AS TableName
                            FROM pg_class c
                            JOIN pg_namespace n ON n.oid = c.relnamespace
                            WHERE c.relkind = 'r' AND n.nspname IN ({schemaList})
                            ORDER BY n.nspname, c.relname;
                            """;
                        using var reader = await listCommand.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            tables.Add((reader.GetString(0), reader.GetString(1)));
                        }
                    }

                    foreach (var (schema, table) in tables)
                    {
                        //One statement per ExecuteNonQueryAsync call - see OpenAdminConnection's remarks on why
                        //VACUUM needs that (never concatenate these into one multi-statement CommandText).
                        using var vacuumCommand = connection.CreateCommand();
                        vacuumCommand.CommandTimeout = 300; //VACUUM can run long on larger tables.
                        vacuumCommand.CommandText = $"VACUUM (ANALYZE) \"{schema}\".\"{table}\";";
                        await vacuumCommand.ExecuteNonQueryAsync();
                    }

                    return $"VACUUM (ANALYZE) completed on {tables.Count} table(s) across {TargetSchemas.Length} schema(s).";
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA optimize</c> (<c>OptimizeDatabase.sql</c>) refreshes query-planner statistics; the
        /// MSSQL driver runs <c>sp_updatestats</c> for the same purpose. The PostgreSQL equivalent is a bare
        /// <c>ANALYZE;</c> with no table argument, which collects statistics for every table in the current
        /// database in one call - simpler and safer to run unattended than issuing per-table <c>ANALYZE</c>
        /// statements, and (like <c>PRAGMA optimize</c>/<c>sp_updatestats</c>) cheap enough to run routinely.
        /// <see cref="VacuumDatabase"/> already runs <c>ANALYZE</c> as part of its own <c>VACUUM (ANALYZE)</c> per
        /// table, so this is somewhat redundant with it in practice - it is kept as its own
        /// <see cref="ISpannedRepository"/> member regardless, matching the shape of the interface/MSSQL driver
        /// (an admin action to refresh statistics without also paying for a full <c>VACUUM</c> pass). Like
        /// <see cref="VacuumDatabase"/>, this always operates on the whole database - <paramref name="databaseName"/>
        /// is accepted only for interface compatibility.
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
                    command.CommandText = "ANALYZE;";
                    await command.ExecuteNonQueryAsync();

                    return "ANALYZE completed - planner statistics refreshed for every table in the database.";
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA integrity_check</c> (<c>IntegrityCheckDatabase.sql</c>) and MSSQL's <c>DBCC CHECKDB</c>
        /// are both read-only, whole-database structural/corruption checks. PostgreSQL has <b>no</b> built-in
        /// statement that does the same thing for the whole database - there is no <c>DBCC CHECKDB</c> analogue
        /// here. The closest available tool is the first-party <c>amcheck</c> contrib extension
        /// (<c>CREATE EXTENSION IF NOT EXISTS amcheck;</c>, idempotent), which only verifies the internal structural
        /// consistency of individual B-tree indexes (<c>bt_index_check</c>, with <c>heapallindexed := true</c> to
        /// also cross-check that every heap tuple is represented in the index) - it does <b>not</b> check heap/table
        /// data integrity, TOAST consistency, or anything beyond B-tree indexes the way <c>DBCC CHECKDB</c> checks
        /// an entire database. This is deliberately documented as a narrower, structural *index* check, not a full
        /// <c>DBCC CHECKDB</c> equivalent - none exists on PostgreSQL. Every B-tree index across all 8
        /// <see cref="TargetSchemas"/> is checked, one <c>bt_index_check</c> call per index (never concatenated -
        /// same one-statement-per-command discipline as <see cref="VacuumDatabase"/>, though <c>bt_index_check</c>
        /// itself has no VACUUM-style transaction restriction; consistency is kept for uniformity/error isolation).
        /// If <c>amcheck</c> cannot be created (not installed on the server, or insufficient privilege to run
        /// <c>CREATE EXTENSION</c>), this returns a descriptive message explaining that instead of letting the
        /// underlying exception propagate uncontextualized - matching <c>PRAGMA integrity_check</c>/
        /// <c>DBCC CHECKDB</c> both returning descriptive text on failure rather than throwing. Whole-database, like
        /// <see cref="VacuumDatabase"/> - <paramref name="databaseName"/> is accepted only for interface
        /// compatibility.
        /// </summary>
        public async Task<string> IntegrityCheckDatabase(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    try
                    {
                        using var createExtension = connection.CreateCommand();
                        createExtension.CommandText = "CREATE EXTENSION IF NOT EXISTS amcheck;";
                        await createExtension.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        //Most commonly a PostgresException with SqlState 42501 (insufficient_privilege) if the
                        //connection's role isn't allowed to CREATE EXTENSION, or an "extension ... is not
                        //available" error if amcheck's control file isn't installed on the server at all - either
                        //way, return a message instead of an uncontextualized exception (see this method's remarks).
                        return "Integrity check skipped - the 'amcheck' extension is not available and could not "
                            + "be created on this PostgreSQL server (missing server-side installation, or the "
                            + $"connection's role lacks CREATE EXTENSION privilege). Underlying error: {ex.Message}";
                    }

                    var indexes = new List<(string Schema, string Table, string Index, long Oid)>();
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using (var listCommand = connection.CreateCommand())
                    {
                        listCommand.CommandText = $"""
                            SELECT n.nspname, t.relname, i.relname, i.oid::bigint
                            FROM pg_index ix
                            JOIN pg_class i ON i.oid = ix.indexrelid
                            JOIN pg_class t ON t.oid = ix.indrelid
                            JOIN pg_namespace n ON n.oid = t.relnamespace
                            JOIN pg_am am ON am.oid = i.relam
                            WHERE am.amname = 'btree' AND n.nspname IN ({schemaList})
                            ORDER BY n.nspname, t.relname, i.relname;
                            """;
                        using var reader = await listCommand.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            indexes.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3)));
                        }
                    }

                    var issues = new List<string>();
                    foreach (var (schema, table, index, oid) in indexes)
                    {
                        using var checkCommand = connection.CreateCommand();
                        checkCommand.CommandTimeout = 300; //bt_index_check can run long on larger indexes.
                        //oid comes from pg_catalog (system-generated, not user input) - safe to interpolate. The
                        //explicit ::regclass cast is required because bt_index_check's parameter type is regclass,
                        //not oid; the two are binary-compatible so the cast never fails for a valid oid.
                        checkCommand.CommandText = $"SELECT bt_index_check(index => {oid}::regclass, heapallindexed => true);";

                        try
                        {
                            await checkCommand.ExecuteNonQueryAsync();
                        }
                        catch (PostgresException ex)
                        {
                            issues.Add($"{schema}.{table} / {index}: {ex.MessageText}");
                        }
                    }

                    return issues.Count == 0
                        ? $"amcheck (bt_index_check) completed - no corruption found across {indexes.Count} B-tree "
                            + $"index(es) in {TargetSchemas.Length} schema(s). Note: this is a structural check of "
                            + "B-tree indexes only, not a full-database check - PostgreSQL has no DBCC CHECKDB equivalent."
                        : "amcheck (bt_index_check) reported issues:\r\n" + string.Join("\r\n", issues);
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA foreign_key_check</c> (<c>ForeignKeyCheck.sql</c>) lists rows that violate a foreign
        /// key; MSSQL's driver runs <c>DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS</c> for both foreign key and check
        /// constraints. The PostgreSQL equivalent queries <c>pg_constraint</c> directly for foreign key (<c>'f'</c>)
        /// and check (<c>'c'</c>) constraints whose <c>convalidated</c> flag is false - a constraint that exists but
        /// has never been (or is currently being) validated against existing data, PostgreSQL's own record of "this
        /// constraint might not actually hold for every row". Every <see cref="TargetSchemas"/> schema is covered by
        /// filtering on the constrained table's namespace. Because every constraint EF Core migrations create is
        /// created as <c>VALID</c> by default (<c>NOT VALID</c> is an opt-in migration option nothing in this
        /// codebase uses), the expected result against a normally-migrated database is always an empty list - this
        /// is not searching for "violations" the way <c>PRAGMA foreign_key_check</c>/<c>DBCC CHECKCONSTRAINTS</c> do
        /// (neither of which PostgreSQL's catalog can answer directly without literally re-scanning every row of
        /// every constrained table, which is what <c>ALTER TABLE ... VALIDATE CONSTRAINT</c> does) - it reports
        /// constraints PostgreSQL itself doesn't yet trust, which normally means none. Whole-database, like the
        /// other members - <paramref name="databaseName"/> is accepted only for interface compatibility.
        /// </summary>
        public async Task<string> ForeignKeyCheck(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using var command = connection.CreateCommand();
                    command.CommandText = $"""
                        SELECT n.nspname AS SchemaName, c.relname AS TableName, con.conname AS ConstraintName
                        FROM pg_constraint con
                        JOIN pg_class c ON c.oid = con.conrelid
                        JOIN pg_namespace n ON n.oid = con.connamespace
                        WHERE con.contype IN ('f','c') AND NOT con.convalidated AND n.nspname IN ({schemaList})
                        ORDER BY n.nspname, c.relname, con.conname;
                        """;

                    var violations = new List<string>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            violations.Add($"{reader.GetString(0)}.{reader.GetString(1)} / {reader.GetString(2)}");
                        }
                    }

                    return violations.Count == 0
                        ? "No unvalidated foreign key or check constraints found (EF Core migrations create every "
                            + "constraint as VALID, so this is expected on a normally-migrated database)."
                        : "Unvalidated foreign key or check constraints found:\r\n" + string.Join("\r\n", violations);
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>GetDatabaseVersion.sql</c> (<c>SELECT SQLITE_VERSION();</c>) reports the same engine version
        /// for all 8 "databases" - there is no per-database/per-schema schema-version concept on that side. Like the
        /// MSSQL driver, the more informative and genuinely per-"database" analogue for the EF providers is each
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/>'s own applied-migrations history - read from the
        /// two distinct <c>__EFMigrationsHistory_*</c> tables (<see cref="PostgresMigrationsHistory"/>), reporting
        /// the most recently applied <c>MigrationId</c> for each of the two contexts this driver manages
        /// (<c>ApplicationDbContext</c> for Identity, <see cref="TightWikiDbContext"/> for everything else).
        /// <c>MigrationId</c> is timestamp-prefixed (<c>yyyyMMddHHmmss_Name</c>), so ordering it as text is
        /// equivalent to ordering chronologically. Returns one row per <see cref="TargetSchemas"/> entry - the same
        /// 8 schema names <see cref="GetDatabasePageCounts"/>/<see cref="GetDatabasePageSizes"/> use - so that
        /// <c>AdminController.Database()</c>'s join across all three methods by schema name actually matches.
        /// </summary>
        /// <remarks>
        /// 7 of the 8 schemas (everything except "Users") are owned exclusively by <see cref="TightWikiDbContext"/>,
        /// so they all report the same value - the most recently applied <c>MigrationId</c> from
        /// <c>public.__EFMigrationsHistory_TightWikiDb</c>. "Users" is the one schema shared between
        /// <see cref="TightWikiDbContext"/> (which spans all 8 schemas) and
        /// <see cref="global::TightWiki.Library.ApplicationDbContext"/> (ASP.NET Core Identity) - each context has
        /// its own, independently versioned migration history, so that one row reports both rather than picking
        /// just one, formatted as <c>"TightWikiDb: &lt;migration&gt; / Identity: &lt;migration&gt;"</c> - same
        /// format as the MSSQL driver's.
        /// </remarks>
        public async Task<List<(string Name, string Version)>> GetDatabaseVersions()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var applicationDbVersion = await GetLatestMigrationId(connection,
                        PostgresMigrationsHistory.ApplicationDbSchema, PostgresMigrationsHistory.ApplicationDbTableName);
                    var tightWikiDbVersion = await GetLatestMigrationId(connection,
                        PostgresMigrationsHistory.TightWikiDbSchema, PostgresMigrationsHistory.TightWikiDbTableName);
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
            command.CommandText = $"SELECT \"MigrationId\" FROM \"{schema}\".\"{table}\" ORDER BY \"MigrationId\" DESC LIMIT 1;";
            var result = await command.ExecuteScalarAsync();
            return result as string ?? string.Empty;
        }

        /// <summary>
        /// PostgreSQL's on-disk page (block) size, queried at runtime via <c>SHOW block_size;</c> rather than
        /// hardcoded. Unlike SQL Server (a fixed architectural 8 KB constant on every edition/version, see
        /// <c>SqlServerDatabaseManager.SqlServerPageSizeBytes</c>), PostgreSQL's <c>block_size</c> is a
        /// compile-time server build option - it defaults to 8192 bytes for virtually every distributed build, but
        /// is not a guaranteed constant the way SQL Server's page size is, so it is queried here instead of assumed.
        /// Shared by <see cref="GetDatabasePageCounts"/> and <see cref="GetDatabasePageSizes"/>.
        /// </summary>
        private static async Task<int> GetBlockSize(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SHOW block_size;";
            var result = await command.ExecuteScalarAsync();
            return int.Parse((string)result!);
        }

        /// <summary>
        /// SQLite's <c>PRAGMA page_count</c> (<c>GetDatabasePageCount.sql</c>) reports the total page count of a
        /// whole database file. Post-consolidation there is one physical PostgreSQL database, so - like the MSSQL
        /// driver - this reports the per-<b>schema</b> equivalent instead: for every schema in
        /// <see cref="TargetSchemas"/>, sums <c>pg_total_relation_size(oid)</c> (a table's heap + all its indexes +
        /// TOAST + free space map, the closest PostgreSQL analogue of "space this schema's data occupies on disk")
        /// across every regular table (<c>relkind = 'r'</c>) in that schema, then divides the byte total by
        /// <see cref="GetBlockSize"/> to turn it into a page count comparable to SQLite's/MSSQL's. <c>LEFT JOIN</c>
        /// so a schema with no tables yet still gets a row with <c>PageCount = 0</c>, keeping the "8 rows" shape
        /// <c>AdminController</c> expects.
        /// </summary>
        public async Task<List<(string Name, int PageCount)>> GetDatabasePageCounts()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var blockSize = await GetBlockSize(connection);
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using var command = connection.CreateCommand();
                    command.CommandText = $"""
                        SELECT n.nspname AS SchemaName, COALESCE(SUM(pg_total_relation_size(c.oid)), 0) AS TotalBytes
                        FROM pg_namespace n
                        LEFT JOIN pg_class c ON c.relnamespace = n.oid AND c.relkind = 'r'
                        WHERE n.nspname IN ({schemaList})
                        GROUP BY n.nspname
                        ORDER BY n.nspname;
                        """;

                    var results = new List<(string, int)>();
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var totalBytes = reader.GetInt64(1);
                        results.Add((reader.GetString(0), (int)(totalBytes / blockSize)));
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
        /// SQLite's <c>PRAGMA page_size</c> (<c>GetDatabasePageSize.sql</c>) is a per-database-file setting, queried
        /// at runtime. Unlike the MSSQL driver (which pairs every schema with a fixed 8 KB compile-time constant),
        /// PostgreSQL's page size is itself queried at runtime via <see cref="GetBlockSize"/> (see its doc comment
        /// for why it isn't hardcoded) and paired with each of the 8 <see cref="TargetSchemas"/> names, matching the
        /// "8 rows" shape <see cref="GetDatabasePageCounts"/> returns (so that <c>PageCount * PageSize</c>, as
        /// computed by <c>AdminController.Database()</c>, yields each schema's approximate on-disk size in bytes).
        /// </summary>
        public async Task<List<(string Name, int PageSize)>> GetDatabasePageSizes()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var blockSize = await GetBlockSize(connection);
                    return TargetSchemas.Select(name => (name, blockSize)).ToList();
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        #endregion
    }
}
