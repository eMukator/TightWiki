using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
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
    /// Skeleton only (Database-Providers-Plan.md phase 2b.1 - pure architectural wiring, no business logic) - every
    /// member throws <see cref="NotImplementedException"/> for now. Real LINQ-based implementations (51 methods)
    /// land across phases 2b.2-2b.13.
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/>/<see cref="Func{ApplicationDbContext}"/> pair rather than an
    /// injected context instance, mirroring <see cref="EfConfigurationRepository"/> (see that class's doc comment)
    /// - <see cref="SqlServer.SqlServerDatabaseManager"/> passes its own <c>CreateDbContext</c>/
    /// <c>CreateApplicationDbContext</c> method groups in as those two delegates. The second delegate is not used
    /// by any member yet - it exists now so this class's constructor signature never needs to change once
    /// <see cref="ValidateEncryptionAndCreateAdminUser"/>/<see cref="UpsertUserClaims"/> (which need
    /// <see cref="ApplicationDbContext"/>/<see cref="UserManager{TUser}"/> for ASP.NET Core Identity) are
    /// implemented for real in phase 2b.13.
    /// </para>
    /// </remarks>
    public sealed class EfUsersRepository : ITwUsersRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly Func<ApplicationDbContext> _createIdentityContext;

        public EfUsersRepository(Func<TightWikiDbContext> createContext, Func<ApplicationDbContext> createIdentityContext)
        {
            _createContext = createContext;
            _createIdentityContext = createIdentityContext;
        }

        public Task<bool> IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false)
            => throw new NotImplementedException();

        public Task DeleteRole(int roleId)
            => throw new NotImplementedException();

        public Task<bool> InsertRole(string name, string? description)
            => throw new NotImplementedException();

        public Task<bool> DoesRoleExist(string name)
            => throw new NotImplementedException();

        public Task<bool> IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = true)
            => throw new NotImplementedException();

        public Task<TwInsertAccountPermissionResult?> InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId)
            => throw new NotImplementedException();

        public Task<bool> IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = false)
            => throw new NotImplementedException();

        public Task<List<TwRole>> AutoCompleteRole(string? searchText)
            => throw new NotImplementedException();

        public Task<List<TwAccountProfile>> AutoCompleteAccount(string? searchText)
            => throw new NotImplementedException();

        public Task<TwAddRoleMemberResult?> AddRoleMemberByname(Guid userId, string roleName)
            => throw new NotImplementedException();

        public Task<TwAddRoleMemberResult?> AddRoleMember(Guid userId, int roleId)
            => throw new NotImplementedException();

        public Task<TwAddAccountMembershipResult?> AddAccountMembership(Guid userId, int roleId)
            => throw new NotImplementedException();

        public Task RemoveRoleMember(int roleId, Guid userId)
            => throw new NotImplementedException();

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

        public Task<TwRole> GetRoleByName(string name)
            => throw new NotImplementedException();

        public Task<List<TwRole>> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
            => throw new NotImplementedException();

        public Task<List<TwAccountProfile>> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => throw new NotImplementedException();

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
