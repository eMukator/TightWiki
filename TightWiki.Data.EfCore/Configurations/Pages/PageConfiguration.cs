using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="Page"/> (Pages.Page).
    /// </summary>
    /// <remarks>
    /// The one-to-many relationships to the other Pages-schema tables (all backed by real FOREIGN KEY
    /// constraints, intra-schema) are configured from the dependent (child) entity's own configuration - see
    /// e.g. <see cref="PageCommentConfiguration"/> - matching the Log/Severity precedent from the previous task.
    /// </remarks>
    public class PageConfiguration : IEntityTypeConfiguration<Page>
    {
        public void Configure(EntityTypeBuilder<Page> builder)
        {
            builder.ToTable("Page", schema: "Pages");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Namespace)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Navigation)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Description).IsRequired();

            //CreatedByUserId/ModifiedByUserId carry no COLLATE NOCASE in the real schema, and are plain user
            //identifiers, not foreign keys (the Users schema is out of scope for this task).

            //The real schema declares both a standalone UNIQUE(Name) index (IX_Page_Name) and a composite
            //UNIQUE(Namespace, Name) constraint (UK_Page). Since Name already includes the namespace prefix
            //(e.g. "Wiki Help :: Markup") and is unique on its own, the composite constraint is functionally
            //redundant - any two rows that would violate UNIQUE(Namespace, Name) would already violate
            //UNIQUE(Name) alone. Consolidated to the two genuinely distinct unique indexes (Name, Navigation)
            //rather than reproducing all three, matching the Emoji precedent from the previous task.
            builder.HasIndex(e => e.Name, "IX_Page_Name").IsUnique();
            builder.HasIndex(e => e.Navigation, "IX_Page_Navigation").IsUnique();

            //PageRevision.PageId carries no FOREIGN KEY constraint in the real schema - intentionally not
            //modeled as a navigation/relationship here.
        }
    }
}
