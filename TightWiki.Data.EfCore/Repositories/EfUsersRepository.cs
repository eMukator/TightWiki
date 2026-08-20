using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;
using UsersEntities = TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Repositories
{
    /// <summary>
    /// Provider-agnostic (SQL Server/Postgres, per Database-Providers-Plan.md chapter 3) LINQ-over-EF-Core
    /// implementation of <see cref="ITwUsersRepository"/>. Lives in the shared <c>TightWiki.Data.EfCore</c>
    /// project rather than a per-provider driver project, for the same reason as <see cref="EfConfigurationRepository"/>/
    /// <see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/<see cref="EfStatisticsRepository"/>/
    /// <see cref="EfPageRepository"/> (see those classes' doc comments): plain LINQ against
    /// <see cref="TightWikiDbContext"/> needs no provider-specific code here at all. Originally landed as a
    /// <c>SqlServerUsersRepository</c> stub under <c>TightWiki.Data.EfCore.SqlServer/Repositories/</c> in
    /// phase 2a.1; moved here (still a stub) in phase 2b.1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 12 of 51 members (the role CRUD/membership category - <see cref="IsAccountAMemberOfRole"/>,
    /// <see cref="DeleteRole"/>, <see cref="InsertRole"/>, <see cref="DoesRoleExist"/>, <see cref="AutoCompleteRole"/>,
    /// <see cref="AddRoleMemberByname"/>, <see cref="AddRoleMember"/>, <see cref="AddAccountMembership"/>,
    /// <see cref="RemoveRoleMember"/>, <see cref="GetRoleByName"/>, <see cref="GetAllRoles"/>, and
    /// <see cref="GetRoleMembersPaged"/>) were implemented for real in phase 2b.9 - see each member's own doc
    /// comment for which SQLite script it mirrors. The remaining 39 members land across phases 2b.10-2b.13 and
    /// still throw <see cref="NotImplementedException"/>.
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/>/<see cref="Func{ApplicationDbContext}"/> pair rather than an
    /// injected context instance, mirroring <see cref="EfConfigurationRepository"/> (see that class's doc comment)
    /// - <see cref="SqlServer.SqlServerDatabaseManager"/> passes its own <c>CreateDbContext</c>/
    /// <c>CreateApplicationDbContext</c> method groups in as those two delegates. Also takes an
    /// <see cref="ITwConfigurationRepository"/> directly (not another <see cref="Func{TResult}"/>), added in phase
    /// 2b.9 (the phase 2b.1 skeleton did not have it) - mirroring the SQLite reference constructor's own
    /// <c>ConfigurationRepository configurationRepository</c> parameter and the same pattern already used by
    /// <see cref="EfPageRepository"/>/<see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/
    /// <see cref="EfStatisticsRepository"/> - <see cref="GetRoleMembersPaged"/> is the first 2b.9 member that needs
    /// it, to read the "Pagination Size" customization setting (see its own doc comment for the reference's own
    /// quirk of always reading this setting regardless of the caller-supplied <c>pageSize</c> argument).
    /// </para>
    /// </remarks>
    public sealed class EfUsersRepository : ITwUsersRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly Func<ApplicationDbContext> _createIdentityContext;
        private readonly ITwConfigurationRepository _configurationRepository;

        public EfUsersRepository(Func<TightWikiDbContext> createContext, Func<ApplicationDbContext> createIdentityContext,
            ITwConfigurationRepository configurationRepository)
        {
            _createContext = createContext;
            _createIdentityContext = createIdentityContext;
            _configurationRepository = configurationRepository;
        }

        /// <summary>
        /// Mirrors IsAccountAMemberOfRole.sql: whether an Users.AccountRole row exists linking
        /// <paramref name="userId"/> to <paramref name="roleId"/>. Cached under
        /// <see cref="MemCache.Category.Security"/>, same cache key shape (userId + roleId) as the SQLite
        /// reference.
        /// </summary>
        public async Task<bool> IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security, [userId, roleId]);

            return await MemCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
            {
                using var context = _createContext();
                return await context.AccountRoles.AnyAsync(ar => ar.UserId == userId && ar.RoleId == roleId);
            });
        }

        /// <summary>
        /// Mirrors DeleteRole.sql: permanently deletes the role matching <paramref name="roleId"/> together with
        /// every Users.AccountRole/Users.RolePermission row referencing it - but only if that role exists and is
        /// not <see cref="UsersEntities.Role.IsBuiltIn"/> (a silent no-op otherwise, same guard the reference
        /// applies independently to each of its three <c>DELETE</c> statements via a repeated
        /// <c>NOT IN (SELECT ... WHERE IsBuiltIn = 1)</c> subquery). Deletes the two child tables first via LINQ
        /// bulk <c>ExecuteDeleteAsync</c> (fully provider-portable, no raw SQL needed - same idiom as
        /// <see cref="EfEmojiRepository.DeleteEmojiById"/>), then the role itself - required ordering under the
        /// consolidated schema's real FK constraints (<see cref="Configurations.Users.AccountRoleConfiguration"/>/
        /// <see cref="Configurations.Users.RolePermissionConfiguration"/> both declare a real, not
        /// cascade-deleting, foreign key to Users.Role), unlike the SQLite reference where the three statements'
        /// relative order is only cosmetic. Not reproduced: the reference's guard clause is technically also
        /// satisfied (vacuously) when <paramref name="roleId"/> does not exist as a row at all, which would let it
        /// delete orphaned Users.AccountRole/Users.RolePermission rows left over from some earlier, already-broken
        /// state - a corner case with no equivalent under the new schema's real FK constraints (no such orphans can
        /// exist in the first place), so this checks "role exists and is not built-in" directly instead.
        /// </summary>
        public async Task DeleteRole(int roleId)
        {
            using var context = _createContext();

            var isDeletable = await context.Roles.AnyAsync(r => r.Id == roleId && !r.IsBuiltIn);
            if (!isDeletable)
            {
                return;
            }

            await context.AccountRoles.Where(ar => ar.RoleId == roleId).ExecuteDeleteAsync();
            await context.RolePermissions.Where(rp => rp.RoleId == roleId).ExecuteDeleteAsync();
            await context.Roles.Where(r => r.Id == roleId).ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors InsertRole.sql: inserts a new, non-built-in Users.Role row (<see cref="UsersEntities.Role.IsBuiltIn"/>
        /// hardcoded to <see langword="false"/>, same literal <c>0</c> the reference script inserts - there is no
        /// UI path to create a built-in role).
        /// </summary>
        /// <remarks>
        /// Returns whether the insert actually affected a row, <b>not</b> reproduced from the SQLite reference's
        /// own return value: <c>UsersFactory.ExecuteScalarAsync&lt;bool?&gt;("InsertRole.sql", param) ?? false</c>
        /// always evaluates to <see langword="false"/>, insert success or not - confirmed by probing
        /// <c>Microsoft.Data.Sqlite</c>'s <c>DbCommand.ExecuteScalar()</c> directly against an equivalent
        /// <c>INSERT ... SELECT</c> statement with no <c>RETURNING</c>/result set: it returns <see langword="null"/>
        /// regardless of whether the row was inserted, which the reference's own <c>?? false</c> then silently
        /// turns into "failed". This is dead code, not a load-bearing quirk: the only caller
        /// (<c>AdminSecurityController.AddRole</c>) does <c>await usersRepository.InsertRole(...)</c> and discards
        /// the result entirely, so fixing the return value here changes no observable application behavior - it
        /// only makes this member actually satisfy its own interface doc comment ("Returns true if the role was
        /// created successfully").
        /// </remarks>
        public async Task<bool> InsertRole(string name, string? description)
        {
            using var context = _createContext();

            context.Roles.Add(new UsersEntities.Role
            {
                Name = name,
                Description = description,
                IsBuiltIn = false,
            });

            return await context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Mirrors DoesRoleExist.sql: whether a Users.Role row with the exact <paramref name="name"/> exists.
        /// Case sensitivity for the comparison is entirely determined by the DB-level collation on
        /// <see cref="UsersEntities.Role.Name"/> (<see cref="Configurations.Users.RoleConfiguration"/>: SQLite
        /// keeps the reference's <c>COLLATE NOCASE</c>; other providers fall back to the database's own default
        /// collation - see <see cref="TightWikiDbContext.StripNonSqliteNoCaseCollation"/>), same as every other
        /// plain <c>==</c>/<c>Contains</c> comparison against this column in this class - no client-side
        /// <see cref="StringComparer"/> is needed here because this filter is translated to SQL and evaluated by
        /// the database, not materialized into memory first (contrast <see cref="GetRoleMembersPaged"/>'s
        /// in-memory sort, which does need one).
        /// </summary>
        public async Task<bool> DoesRoleExist(string name)
        {
            using var context = _createContext();
            return await context.Roles.AnyAsync(r => r.Name == name);
        }

        public Task<bool> IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = true)
            => throw new NotImplementedException();

        public Task<TwInsertAccountPermissionResult?> InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId)
            => throw new NotImplementedException();

        public Task<bool> IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = false)
            => throw new NotImplementedException();

        /// <summary>
        /// Mirrors AutoCompleteRole.sql: Users.Role rows whose <see cref="UsersEntities.Role.Name"/> contains
        /// <paramref name="searchText"/> (an empty string, matching everything, if null - same as the reference's
        /// <c>searchText ?? string.Empty</c>), ordered by Name, capped at 25 rows. No caching, matching the SQLite
        /// reference. <see cref="TwRole.Description"/>/<see cref="TwRole.IsBuiltIn"/> are deliberately left unset
        /// here, same as the reference script's own column list (it selects only Id/Name) - this is the same
        /// "reference selects fewer columns than the target model has properties for" idiom already documented on
        /// <see cref="EfPageRepository.GetTopRecentlyModifiedPagesInfoByUserId"/>, with no observable behavioral
        /// impact since the only consumer of this member is an autocomplete dropdown that only reads Name.
        /// </summary>
        public async Task<List<TwRole>> AutoCompleteRole(string? searchText)
        {
            using var context = _createContext();
            var text = searchText ?? string.Empty;

            return await context.Roles
                .Where(r => r.Name.Contains(text))
                .OrderBy(r => r.Name)
                .Take(25)
                .Select(r => new TwRole
                {
                    Id = r.Id,
                    Name = r.Name,
                })
                .ToListAsync();
        }

        public Task<List<TwAccountProfile>> AutoCompleteAccount(string? searchText)
            => throw new NotImplementedException();

        /// <summary>
        /// Mirrors AddRoleMemberByName.sql: resolves <paramref name="roleName"/> to a Users.Role.Id, then delegates
        /// to the same insert-and-build-result logic as <see cref="AddRoleMember"/> (see
        /// <see cref="InsertRoleMemberAndBuildResultAsync"/>'s remarks for how a missing role/profile/identity row
        /// is handled). Not reproduced: the reference resolves the role name via an uncorrelated
        /// <c>(SELECT R.Id FROM Role R WHERE R.Name = @RoleName)</c> subquery directly inside the <c>INSERT</c>
        /// statement - if no role matches, that subquery evaluates to <see langword="NULL"/>, which the reference's
        /// real <c>RoleId INTEGER NOT NULL</c> column constraint then turns into a thrown "NOT NULL constraint
        /// failed" exception (confirmed by inspection of Scripts/Initialization/Versions/2.26.0/^004^Users^AccountRole.sql -
        /// there is no application-level guard against this anywhere in the reference). This resolves the role
        /// name first and returns <see langword="null"/> instead of letting an unhandled constraint-violation
        /// exception propagate, consistent with the "returns the result, or null if the operation failed" contract
        /// already documented on this interface member and with how <see cref="InsertRoleMemberAndBuildResultAsync"/>
        /// already handles every other "the row this insert would reference does not exist" case.
        /// </summary>
        public async Task<TwAddRoleMemberResult?> AddRoleMemberByname(Guid userId, string roleName)
            => await InsertRoleMemberAndBuildResultAsync(userId, roleId: null, roleName: roleName);

        /// <summary>
        /// Mirrors AddRoleMember.sql: inserts a new Users.AccountRole row linking <paramref name="userId"/> to
        /// <paramref name="roleId"/>, then returns the newly-added member's profile/identity info. See
        /// <see cref="InsertRoleMemberAndBuildResultAsync"/>'s remarks for the full behavior, including how a
        /// missing role/profile/identity row is handled.
        /// </summary>
        public async Task<TwAddRoleMemberResult?> AddRoleMember(Guid userId, int roleId)
            => await InsertRoleMemberAndBuildResultAsync(userId, roleId, roleName: null);

        /// <summary>
        /// Shared implementation behind <see cref="AddRoleMember"/>/<see cref="AddRoleMemberByname"/>: resolves
        /// <paramref name="roleId"/> directly, or <paramref name="roleName"/> to a Users.Role.Id (exactly one of
        /// the two is supplied by the two public callers), inserts the Users.AccountRole row, then builds the
        /// result by reading Users.Profile (same <see cref="TightWikiDbContext"/>) plus Email/EmailConfirmed and
        /// the "firstname"/"lastname" AspNetUserClaims claims (a second, separate <see cref="ApplicationDbContext"/>
        /// - Identity tables cannot be reached via a LINQ join from <see cref="TightWikiDbContext"/>, see
        /// <see cref="EfConfigurationRepository.GetWikiDatabaseMetrics"/>'s doc comment for the same two-context
        /// split applied to a simpler case). Returns <see langword="null"/> - rather than letting a real foreign
        /// key violation propagate as an unhandled <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> -
        /// when the role or the user's Users.Profile row does not exist, or (mirroring the reference's own
        /// <c>INNER JOIN AspNetUsers</c>) when no matching Identity user is found for <paramref name="userId"/>.
        /// This is a deliberate divergence from the SQLite reference, which has no such guard and would either
        /// throw (missing role, a real <c>NOT NULL</c> violation both here and there) or - if SQLite foreign key
        /// enforcement happens to be off, unlike the consolidated schema's real, always-enforced FK constraints,
        /// see <see cref="Configurations.Users.AccountRoleConfiguration"/> - silently insert an orphaned row and
        /// then return <see langword="null"/> anyway once the follow-up <c>SELECT</c>'s <c>INNER JOIN</c> to
        /// Profile/AspNetUsers fails to match it; pre-checking here reaches the same "null on a bad foreign key"
        /// outcome deterministically, regardless of provider or pragma state.
        /// </summary>
        private async Task<TwAddRoleMemberResult?> InsertRoleMemberAndBuildResultAsync(Guid userId, int? roleId, string? roleName)
        {
            using var context = _createContext();

            int resolvedRoleId;
            if (roleId.HasValue)
            {
                resolvedRoleId = roleId.Value;
                if (!await context.Roles.AnyAsync(r => r.Id == resolvedRoleId))
                {
                    return null;
                }
            }
            else
            {
                var foundRoleId = await context.Roles.Where(r => r.Name == roleName).Select(r => (int?)r.Id).FirstOrDefaultAsync();
                if (foundRoleId == null)
                {
                    return null;
                }
                resolvedRoleId = foundRoleId.Value;
            }

            var profile = await context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return null;
            }

            var accountRole = new UsersEntities.AccountRole { UserId = userId, RoleId = resolvedRoleId };
            context.AccountRoles.Add(accountRole);
            await context.SaveChangesAsync();

            using var identityContext = _createIdentityContext();
            var identityUserId = userId.ToString();

            var identity = await identityContext.Users
                .Where(u => u.Id == identityUserId)
                .Select(u => new { u.Email })
                .FirstOrDefaultAsync();

            if (identity == null)
            {
                return null;
            }

            var firstName = await identityContext.UserClaims
                .Where(c => c.UserId == identityUserId && c.ClaimType == "firstname")
                .Select(c => c.ClaimValue)
                .FirstOrDefaultAsync();

            var lastName = await identityContext.UserClaims
                .Where(c => c.UserId == identityUserId && c.ClaimType == "lastname")
                .Select(c => c.ClaimValue)
                .FirstOrDefaultAsync();

            return new TwAddRoleMemberResult
            {
                Id = accountRole.Id,
                UserId = userId,
                Navigation = profile.Navigation ?? string.Empty,
                AccountName = profile.AccountName ?? string.Empty,
                EmailAddress = identity.Email ?? string.Empty,
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
            };
        }

        /// <summary>
        /// Mirrors AddAccountMembership.sql: inserts a new Users.AccountRole row linking <paramref name="userId"/>
        /// to <paramref name="roleId"/>, then returns its generated Id together with the role's Name - unlike
        /// <see cref="AddRoleMember"/>/<see cref="AddRoleMemberByname"/>, the reference's own follow-up
        /// <c>SELECT</c> only joins Users.Role (not Profile/AspNetUsers), so no cross-<see cref="ApplicationDbContext"/>
        /// lookup is needed here. Returns <see langword="null"/> when the role or the user's Users.Profile row does
        /// not exist - see <see cref="InsertRoleMemberAndBuildResultAsync"/>'s remarks for why this pre-checks
        /// rather than letting a real foreign key violation propagate (the same reasoning applies here even though
        /// the reference's own follow-up <c>SELECT</c> for this particular script does not itself re-check
        /// Profile/AspNetUsers).
        /// </summary>
        public async Task<TwAddAccountMembershipResult?> AddAccountMembership(Guid userId, int roleId)
        {
            using var context = _createContext();

            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
            {
                return null;
            }

            if (!await context.Profiles.AnyAsync(p => p.UserId == userId))
            {
                return null;
            }

            var accountRole = new UsersEntities.AccountRole { UserId = userId, RoleId = roleId };
            context.AccountRoles.Add(accountRole);
            await context.SaveChangesAsync();

            return new TwAddAccountMembershipResult
            {
                Id = accountRole.Id,
                Name = role.Name,
            };
        }

        /// <summary>
        /// Mirrors RemoveRoleMember.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - fully provider-portable,
        /// no raw SQL needed, same idiom used throughout <see cref="EfPageRepository"/>.
        /// </summary>
        public async Task RemoveRoleMember(int roleId, Guid userId)
        {
            using var context = _createContext();
            await context.AccountRoles.Where(ar => ar.RoleId == roleId && ar.UserId == userId).ExecuteDeleteAsync();
        }

        public Task RemoveRolePermission(int id)
            => throw new NotImplementedException();

        public Task RemoveAccountPermission(int id)
            => throw new NotImplementedException();

        public Task<TwInsertRolePermissionResult?> InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId)
            => throw new NotImplementedException();

        public Task<List<TwApparentPermission>> GetApparentAccountPermissions(Guid userId)
            => throw new NotImplementedException();

        public Task<List<TwApparentPermission>> GetApparentRolePermissions(TwRoles role)
            => throw new NotImplementedException();

        public Task<List<TwApparentPermission>> GetApparentRolePermissions(string roleName)
            => throw new NotImplementedException();

        public Task<List<TightWiki.Plugin.Models.TwPermissionDisposition>> GetAllPermissionDispositions()
            => throw new NotImplementedException();

        public Task<List<TightWiki.Plugin.Models.TwPermission>> GetAllPermissions()
            => throw new NotImplementedException();

        public Task<List<TwRolePermission>> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => throw new NotImplementedException();

        public Task<List<TwAccountProfile>> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
            => throw new NotImplementedException();

        public Task AnonymizeProfile(Guid userId)
            => throw new NotImplementedException();

        public Task<bool> IsUserMemberOfAdministrators(Guid userId)
            => throw new NotImplementedException();

        /// <summary>
        /// Mirrors GetRoleByName.sql: the single Users.Role row matching <paramref name="name"/> exactly (all four
        /// columns - Id/Name/Description/IsBuiltIn - same as the reference's own column list). Uses
        /// <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync{TSource}(IQueryable{TSource})"/>,
        /// matching the reference's <c>QuerySingle&lt;TwRole&gt;</c> (Dapper's "exactly one row or throw", not
        /// "first row or default") - throws if no role or more than one role matches.
        /// </summary>
        public async Task<TwRole> GetRoleByName(string name)
        {
            using var context = _createContext();

            return await context.Roles
                .Where(r => r.Name == name)
                .Select(r => new TwRole
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description ?? string.Empty,
                    IsBuiltIn = r.IsBuiltIn,
                })
                .SingleAsync();
        }

        /// <summary>
        /// Mirrors GetAllRoles.sql: every Users.Role row, ordered by Name ascending by default. Custom ordering
        /// mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c> mapping
        /// ("Name"/"Description" - same pattern as <see cref="EfPageRepository.GetPageRevisionsInfoByNavigationPaged"/>);
        /// an unrecognized <paramref name="orderBy"/> throws, same as <c>RepositoryHelpers.TransposeOrderby</c>'s
        /// "No order by mapping..." exception. <see cref="TwRole.IsBuiltIn"/> is deliberately left unset here, same
        /// as the reference script's own column list (it selects only Id/Name/Description, unlike
        /// <see cref="GetRoleByName"/>'s four-column list) - the same "reference selects fewer columns than the
        /// target model has properties for" idiom as <see cref="AutoCompleteRole"/>, with no observable behavioral
        /// impact since the one page that lists roles this way (<c>AdminSecurity/Roles</c>) only ever displays
        /// Name/Description, never IsBuiltIn (that flag is only read off <see cref="GetRoleByName"/>'s result, on
        /// the single-role detail page, to hide the delete button for a built-in role).
        /// </summary>
        public async Task<List<TwRole>> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
        {
            using var context = _createContext();

            var query = context.Roles.AsQueryable();
            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(r => r.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "NAME" => ascending ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name),
                    "DESCRIPTION" => ascending ? query.OrderBy(r => r.Description) : query.OrderByDescending(r => r.Description),
                    _ => throw new InvalidOperationException($"No order by mapping was found in 'GetAllRoles.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Select(r => new TwRole
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description ?? string.Empty,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetRoleMembersPaged.sql: every Users.Profile row for a user with a Users.AccountRole membership
        /// in <paramref name="roleId"/>, combined with Email/EmailConfirmed and the "firstname"/"lastname"/
        /// "timezone"/"language"/"*/country" AspNetUserClaims claims from the separate <see cref="ApplicationDbContext"/>
        /// (same two-context split as <see cref="InsertRoleMemberAndBuildResultAsync"/>, but for a whole page of
        /// rows instead of one). Because ordering/paging in the reference can be driven by columns that live in
        /// that second context (EmailAddress/FirstName/LastName - see the script's own <c>--CONFIG::</c> mapping),
        /// a single server-translated LINQ query cannot express this: the (typically small) set of role members is
        /// pulled into memory first, joined to identity data by <see cref="Guid"/>-keyed dictionaries, then
        /// sorted/paged client-side. Default ordering mirrors the script's own un-transposed
        /// <c>ORDER BY U.AccountName, U.UserId</c> (both ascending); an unrecognized <paramref name="orderBy"/>
        /// throws, same pattern as <see cref="GetAllRoles"/>. All string comparisons/sorts here are explicit
        /// <see cref="StringComparer.Ordinal"/>/<see cref="StringComparer.OrdinalIgnoreCase"/> rather than the
        /// default (culture-sensitive) comparer, since none of this runs through the database - the same class of
        /// fix already applied to in-memory string grouping/dedup elsewhere in <see cref="EfPageRepository"/> (see
        /// that class's doc comment on the phase 2b.5 case-insensitive-dedup bug).
        /// </summary>
        /// <remarks>
        /// <see cref="TwAccountProfile.PaginationPageSize"/>/<see cref="TwAccountProfile.PaginationPageCount"/> are
        /// computed from the "Pagination Size" customization setting, <b>not</b> from <paramref name="pageSize"/> -
        /// the reference script's own <c>GetRoleMembersPaged</c> C# wrapper ignores its <c>pageSize</c> parameter
        /// entirely and always reads the configured value, the same already-established idiom used throughout
        /// <see cref="EfPageRepository"/> (e.g. <see cref="EfPageRepository.GetTopViewedPagesInfo"/>) for reference
        /// members with the identical quirk. The reference's own <see cref="TwAccountProfile.PaginationPageCount"/>
        /// subquery additionally inner-joins AspNetUsers when counting; that is not reproduced here (the plain
        /// Profile+AccountRole count is used instead) since a Users.Profile row with no matching Identity user
        /// would itself be a data integrity anomaly with no legitimate path to exist.
        /// </remarks>
        public async Task<List<TwAccountProfile>> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var membersQuery = from ar in context.AccountRoles
                                join p in context.Profiles on ar.UserId equals p.UserId
                                where ar.RoleId == roleId
                                select p;

            var totalCount = await membersQuery.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            var members = await membersQuery.ToListAsync();

            using var identityContext = _createIdentityContext();
            var identityUserIds = members.Select(p => p.UserId.ToString()).ToList();

            var identities = await identityContext.Users
                .Where(u => identityUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.EmailConfirmed })
                .ToDictionaryAsync(u => u.Id, StringComparer.OrdinalIgnoreCase);

            var relevantClaimTypes = new[] { "firstname", "lastname", "timezone", "language" };
            var claims = await identityContext.UserClaims
                .Where(c => identityUserIds.Contains(c.UserId) && c.ClaimType != null
                    && (relevantClaimTypes.Contains(c.ClaimType) || c.ClaimType.EndsWith("/country")))
                .Select(c => new { c.UserId, c.ClaimType, c.ClaimValue })
                .ToListAsync();

            var claimsByUser = claims
                .GroupBy(c => c.UserId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            string? GetClaimValue(string identityUserId, Func<string, bool> claimTypeMatch)
                => claimsByUser.TryGetValue(identityUserId, out var userClaims)
                    ? userClaims.FirstOrDefault(c => c.ClaimType != null && claimTypeMatch(c.ClaimType))?.ClaimValue
                    : null;

            var profiles = members.Select(p =>
            {
                var identityUserId = p.UserId.ToString();
                identities.TryGetValue(identityUserId, out var identity);

                return new TwAccountProfile
                {
                    UserId = p.UserId,
                    EmailAddress = identity?.Email ?? string.Empty,
                    AccountName = p.AccountName ?? string.Empty,
                    Navigation = p.Navigation ?? string.Empty,
                    FirstName = GetClaimValue(identityUserId, t => t == "firstname"),
                    LastName = GetClaimValue(identityUserId, t => t == "lastname"),
                    TimeZone = GetClaimValue(identityUserId, t => t == "timezone") ?? string.Empty,
                    Language = GetClaimValue(identityUserId, t => t == "language") ?? string.Empty,
                    Country = GetClaimValue(identityUserId, t => t.EndsWith("/country", StringComparison.Ordinal)) ?? string.Empty,
                    CreatedDate = p.CreatedDate,
                    ModifiedDate = p.ModifiedDate,
                    EmailConfirmed = identity?.EmailConfirmed ?? false,
                    PaginationPageSize = paginationSize,
                    PaginationPageCount = paginationPageCount,
                };
            }).ToList();

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            IOrderedEnumerable<TwAccountProfile> ordered = string.IsNullOrEmpty(orderBy)
                ? profiles.OrderBy(x => x.AccountName, StringComparer.Ordinal).ThenBy(x => x.UserId)
                : orderBy.ToUpperInvariant() switch
                {
                    "EMAILADDRESS" => ascending
                        ? profiles.OrderBy(x => x.EmailAddress, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.EmailAddress, StringComparer.Ordinal),
                    "ACCOUNTNAME" => ascending
                        ? profiles.OrderBy(x => x.AccountName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.AccountName, StringComparer.Ordinal),
                    "LASTNAME" => ascending
                        ? profiles.OrderBy(x => x.LastName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.LastName, StringComparer.Ordinal),
                    "FIRSTNAME" => ascending
                        ? profiles.OrderBy(x => x.FirstName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.FirstName, StringComparer.Ordinal),
                    _ => throw new InvalidOperationException($"No order by mapping was found in 'GetRoleMembersPaged.sql' for the field '{orderBy}'."),
                };

            return ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .ToList();
        }

        public Task<List<TwAccountPermission>> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => throw new NotImplementedException();

        public Task<List<TwAccountRoleMembership>> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => throw new NotImplementedException();

        public Task<List<TwAccountProfile>> GetAllUsers()
            => throw new NotImplementedException();

        public Task<List<TwAccountProfile>> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
            => throw new NotImplementedException();

        public Task CreateProfile(Guid userId, string accountName)
            => throw new NotImplementedException();

        public Task<bool> DoesEmailAddressExist(string? emailAddress)
            => throw new NotImplementedException();

        public Task<bool> DoesProfileAccountExist(string navigation)
            => throw new NotImplementedException();

        public Task<TwAccountProfile?> GetBasicProfileByUserId(Guid userId)
            => throw new NotImplementedException();

        public Task<TwAccountProfile> GetAccountProfileByUserId(Guid userId, bool forceReCache = false)
            => throw new NotImplementedException();

        public Task SetProfileUserId(string navigation, Guid userId)
            => throw new NotImplementedException();

        public Task<Guid?> GetUserAccountIdByNavigation(string navigation)
            => throw new NotImplementedException();

        public Task<TwAccountProfile?> GetAccountProfileByNavigation(string? navigation)
            => throw new NotImplementedException();

        public Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash)
            => throw new NotImplementedException();

        public Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password)
            => throw new NotImplementedException();

        public Task<TwProfileAvatar?> GetProfileAvatarByNavigation(string navigation)
            => throw new NotImplementedException();

        public Task UpdateProfile(TwAccountProfile item)
            => throw new NotImplementedException();

        public Task UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType)
            => throw new NotImplementedException();

        public Task<TwAdminPasswordChangeState> AdminPasswordStatus()
            => throw new NotImplementedException();

        public Task SetAdminPasswordClear()
            => throw new NotImplementedException();

        public Task SetAdminPasswordIsChanged()
            => throw new NotImplementedException();

        public Task SetAdminPasswordIsDefault()
            => throw new NotImplementedException();

        #region Security.

        public void ValidateEncryptionAndCreateAdminUser(UserManager<IdentityUser> userManager)
            => throw new NotImplementedException();

        public Task UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims)
            => throw new NotImplementedException();

        #endregion
    }
}
