using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Data.EfCore.SqlServer.Repositories
{
    /// <summary>
    /// MSSQL/EF Core implementation of <see cref="ITwUsersRepository"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton only (Database-Providers-Plan.md phase 2a.1) - every member throws
    /// <see cref="NotImplementedException"/> for now. Real LINQ-based implementations (51 methods) land in
    /// phase 2b, alongside the Identity/schema-Users work (chapter 4.1.1). See
    /// <see cref="SqlServerConfigurationRepository"/> for why this is a concrete class rather than typing
    /// <see cref="SqlServerDatabaseManager.UsersRepository"/> directly as <see cref="ITwUsersRepository"/>.
    /// </remarks>
    public class SqlServerUsersRepository : ITwUsersRepository
    {
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
