using Microsoft.EntityFrameworkCore;
using ConfigEntities = TightWiki.Data.EfCore.Entities.Config;
using DeletedPageRevisionsEntities = TightWiki.Data.EfCore.Entities.DeletedPageRevisions;
using DeletedPagesEntities = TightWiki.Data.EfCore.Entities.DeletedPages;
using EmojiEntities = TightWiki.Data.EfCore.Entities.Emoji;
using LoggingEntities = TightWiki.Data.EfCore.Entities.Logging;
using PagesEntities = TightWiki.Data.EfCore.Entities.Pages;
using StatisticsEntities = TightWiki.Data.EfCore.Entities.Statistics;
using UsersEntities = TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore
{
    /// <summary>
    /// Provider-agnostic shared EF Core model for TightWiki. Consolidates the 8 physically separate SQLite
    /// databases (Config, DeletedPageRevisions, DeletedPages, Emoji, Logging, Pages, Statistics, Users) into one
    /// logical database with 8 schemas, per Database-Providers-Plan.md chapter 4.3 - <b>not</b> 8 independent
    /// islands, hence the cross-schema navigations declared on some of the entities below (see e.g.
    /// <see cref="PagesEntities.Page.CreatedByUser"/> / <see cref="UsersEntities.Profile.Pages_CreatedPages"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>DbSet</c> property names are the plain pluralized entity name (matching the <c>_Scaffold/</c>
    /// reference dump's convention) unless that name would collide across schemas - e.g. both Pages and
    /// DeletedPages declare a <c>Page</c>/<c>PageComment</c>/etc. entity - in which case the property is
    /// prefixed with the schema name (<c>DeletedPages_Pages</c>, <c>Pages_PageComments</c>, ...), matching the
    /// same disambiguation convention used for the reverse-navigation collections on
    /// <see cref="UsersEntities.Profile"/>.
    /// </para>
    /// <para>
    /// Fluent configuration is not hand-wired per entity here - <see cref="OnModelCreating"/> applies every
    /// <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/> in this assembly
    /// (the <c>Configurations/</c> folder) via <c>ApplyConfigurationsFromAssembly</c>, the idiomatic EF Core way
    /// to register dozens of configuration classes without listing them one by one.
    /// </para>
    /// </remarks>
    public class TightWikiDbContext : DbContext
    {
        /// <summary>
        /// Accepts the non-generic <see cref="DbContextOptions"/> (rather than
        /// <see cref="DbContextOptions{TightWikiDbContext}"/>) so that per-provider driver projects can configure
        /// this same shared context - each driver project calls <c>UseSqlServer</c>/<c>UseNpgsql</c> against a
        /// plain <see cref="DbContextOptionsBuilder"/>, matching the pattern used by
        /// <see cref="Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext{TUser}"/> for the same
        /// reason. <c>Microsoft.Extensions.DependencyInjection</c>'s <c>AddDbContext&lt;TContext&gt;</c> supports
        /// either constructor shape.
        /// </summary>
        public TightWikiDbContext(DbContextOptions options)
            : base(options)
        {
        }

        //Config schema (7 entities) - no cross-schema navigations.
        public DbSet<ConfigEntities.ConfigurationEntry> ConfigurationEntries { get; set; } = null!;
        public DbSet<ConfigEntities.ConfigurationGroup> ConfigurationGroups { get; set; } = null!;
        public DbSet<ConfigEntities.CryptoCheck> CryptoChecks { get; set; } = null!;
        public DbSet<ConfigEntities.DataType> DataTypes { get; set; } = null!;
        public DbSet<ConfigEntities.MenuItem> MenuItems { get; set; } = null!;
        public DbSet<ConfigEntities.Theme> Themes { get; set; } = null!;
        public DbSet<ConfigEntities.VersionState> VersionStates { get; set; } = null!;

        //Emoji schema (2 entities) - no cross-schema navigations.
        public DbSet<EmojiEntities.Emoji> Emojis { get; set; } = null!;
        public DbSet<EmojiEntities.EmojiCategory> EmojiCategories { get; set; } = null!;

        //Logging schema (2 entities) - no cross-schema navigations.
        public DbSet<LoggingEntities.Log> Logs { get; set; } = null!;
        public DbSet<LoggingEntities.Severity> Severities { get; set; } = null!;

        //Statistics schema (1 entity) - PageStatistic has a cross-schema navigation to Pages.Page.
        public DbSet<StatisticsEntities.PageStatistic> PageStatistics { get; set; } = null!;

        //Users schema (8 entities) - Profile carries the reverse side of every cross-schema *UserId navigation
        //declared on the Pages/DeletedPages/DeletedPageRevisions entities below.
        public DbSet<UsersEntities.AccountPermission> AccountPermissions { get; set; } = null!;
        public DbSet<UsersEntities.AccountRole> AccountRoles { get; set; } = null!;
        public DbSet<UsersEntities.AdminPwCheck> AdminPwChecks { get; set; } = null!;
        public DbSet<UsersEntities.Permission> Permissions { get; set; } = null!;
        public DbSet<UsersEntities.PermissionDisposition> PermissionDispositions { get; set; } = null!;
        public DbSet<UsersEntities.Profile> Profiles { get; set; } = null!;
        public DbSet<UsersEntities.Role> Roles { get; set; } = null!;
        public DbSet<UsersEntities.RolePermission> RolePermissions { get; set; } = null!;

        //Pages schema (12 entities). CurrentPageEditor/FeatureTemplate/PageReference are unique names and need
        //no prefix; the rest collide with DeletedPages (and, for PageRevision/PageRevisionAttachment, also with
        //DeletedPageRevisions), hence the "Pages_" prefix.
        public DbSet<PagesEntities.CurrentPageEditor> CurrentPageEditors { get; set; } = null!;
        public DbSet<PagesEntities.FeatureTemplate> FeatureTemplates { get; set; } = null!;
        public DbSet<PagesEntities.PageReference> PageReferences { get; set; } = null!;
        public DbSet<PagesEntities.Page> Pages_Pages { get; set; } = null!;
        public DbSet<PagesEntities.PageComment> Pages_PageComments { get; set; } = null!;
        public DbSet<PagesEntities.PageFile> Pages_PageFiles { get; set; } = null!;
        public DbSet<PagesEntities.PageFileRevision> Pages_PageFileRevisions { get; set; } = null!;
        public DbSet<PagesEntities.PageProcessingInstruction> Pages_PageProcessingInstructions { get; set; } = null!;
        public DbSet<PagesEntities.PageRevision> Pages_PageRevisions { get; set; } = null!;
        public DbSet<PagesEntities.PageRevisionAttachment> Pages_PageRevisionAttachments { get; set; } = null!;
        public DbSet<PagesEntities.PageTag> Pages_PageTags { get; set; } = null!;
        public DbSet<PagesEntities.PageToken> Pages_PageTokens { get; set; } = null!;

        //DeletedPages schema (10 entities). Every entity name here collides with either Pages or
        //DeletedPageRevisions, so all are prefixed with "DeletedPages_".
        public DbSet<DeletedPagesEntities.DeletionMeta> DeletedPages_DeletionMetas { get; set; } = null!;
        public DbSet<DeletedPagesEntities.Page> DeletedPages_Pages { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageComment> DeletedPages_PageComments { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageFile> DeletedPages_PageFiles { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageFileRevision> DeletedPages_PageFileRevisions { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageProcessingInstruction> DeletedPages_PageProcessingInstructions { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageRevision> DeletedPages_PageRevisions { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageRevisionAttachment> DeletedPages_PageRevisionAttachments { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageTag> DeletedPages_PageTags { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageToken> DeletedPages_PageTokens { get; set; } = null!;

        //DeletedPageRevisions schema (3 entities). All three names collide with Pages/DeletedPages, so all are
        //prefixed with "DeletedPageRevisions_".
        public DbSet<DeletedPageRevisionsEntities.DeletionMeta> DeletedPageRevisions_DeletionMetas { get; set; } = null!;
        public DbSet<DeletedPageRevisionsEntities.PageRevision> DeletedPageRevisions_PageRevisions { get; set; } = null!;
        public DbSet<DeletedPageRevisionsEntities.PageRevisionAttachment> DeletedPageRevisions_PageRevisionAttachments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TightWikiDbContext).Assembly);
        }
    }
}
