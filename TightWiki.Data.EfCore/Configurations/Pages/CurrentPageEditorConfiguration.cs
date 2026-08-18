using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="CurrentPageEditor"/> (Pages.CurrentPageEditors).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.38.0/^001^Pages^CurrentPageEditors.sql (guarded by
    /// "IF TABLE NOT EXISTS", so it only creates the table on installs that predate it, but it is the only real
    /// CREATE TABLE definition for this table). No COLLATE NOCASE is declared for any column there, and UserId
    /// is TEXT (application code always writes <c>Guid.ToString()</c> into it - see
    /// PageRepository.UpsertCurrentPageEditor). This deliberately does not match the local dev copy of
    /// Data/pages.db, whose CurrentPageEditors table predates that script (UserId is INTEGER there, AccountName
    /// carries COLLATE NOCASE) - the script is the authoritative, current schema per the task's source-of-truth
    /// priority order.
    /// </remarks>
    public class CurrentPageEditorConfiguration : IEntityTypeConfiguration<CurrentPageEditor>
    {
        public void Configure(EntityTypeBuilder<CurrentPageEditor> builder)
        {
            builder.ToTable("CurrentPageEditors", schema: "Pages");

            builder.HasKey(e => new { e.PageId, e.UserId });

            builder.Property(e => e.AccountName).IsRequired();

            builder.Property(e => e.UtcDate)
                .IsRequired()
                .HasColumnName("UTCDate");
        }
    }
}
