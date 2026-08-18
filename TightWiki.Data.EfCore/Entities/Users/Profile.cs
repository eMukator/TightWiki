namespace TightWiki.Data.EfCore.Entities.Users
{
    /// <summary>
    /// The wiki-specific profile data for a user account (Users.Profile), one row per Identity user. <see
    /// cref="UserId"/> matches the corresponding AspNetUsers.Id row, but is modeled as a plain <see cref="Guid"/>
    /// column here, not an EF navigation - AspNetUsers lives in a separate DbContext (ApplicationDbContext), see
    /// Database-Providers-Plan.md chapter 4.1.1.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// The unique identifier of the user this profile belongs to (matches AspNetUsers.Id).
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The unique, case-insensitive, URL-safe navigation path used to locate this user's public profile, or
        /// null if not set.
        /// </summary>
        public string? Navigation { get; set; }

        /// <summary>
        /// The unique account name used for login and display, or null if not set.
        /// </summary>
        public string? AccountName { get; set; }

        /// <summary>
        /// A short biography or description provided by the user, or null if not provided.
        /// </summary>
        public string? Biography { get; set; }

        /// <summary>
        /// The user's avatar image bytes, or null if no avatar has been uploaded.
        /// </summary>
        public byte[]? Avatar { get; set; }

        /// <summary>
        /// The date and time this profile was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The date and time this profile was last modified.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The MIME type of <see cref="Avatar"/>, or null if no avatar has been uploaded.
        /// </summary>
        public string? AvatarContentType { get; set; }

        /// <summary>
        /// The account-level permission records for this user.
        /// </summary>
        public ICollection<AccountPermission> AccountPermissions { get; set; } = [];

        /// <summary>
        /// The role memberships assigned to this user.
        /// </summary>
        public ICollection<AccountRole> AccountRoles { get; set; } = [];
    }
}
