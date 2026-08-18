using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageComment"/> (DeletedPages.PageComment).
    /// </summary>
    public class PageCommentConfiguration : IEntityTypeConfiguration<PageComment>
    {
        public void Configure(EntityTypeBuilder<PageComment> builder)
        {
            builder.ToTable("PageComment", schema: "DeletedPages");

            builder.HasKey(e => e.Id);

            //Id is copied verbatim from the source comment - not database-generated.
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.Body).IsRequired();
        }
    }
}
