using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageFileRevision"/> (DeletedPages.PageFileRevision).
    /// </summary>
    public class PageFileRevisionConfiguration : IEntityTypeConfiguration<PageFileRevision>
    {
        public void Configure(EntityTypeBuilder<PageFileRevision> builder)
        {
            builder.ToTable("PageFileRevision", schema: "DeletedPages");

            builder.HasKey(e => new { e.PageFileId, e.Revision });

            builder.Property(e => e.ContentType)
                .IsRequired()
                .UseCollation("NOCASE");

            //The real schema does declare COLLATE NOCASE for CreatedByUserId here too (matching
            //Pages.PageFileRevision), but it is dropped since the column is modeled as Guid, not string - see
            //Pages.PageFileRevisionConfiguration for the full rationale.

            builder.Property(e => e.Data).IsRequired();
        }
    }
}
