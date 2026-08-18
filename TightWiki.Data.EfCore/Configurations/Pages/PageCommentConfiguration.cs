using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageComment"/> (Pages.PageComment).
    /// </summary>
    public class PageCommentConfiguration : IEntityTypeConfiguration<PageComment>
    {
        public void Configure(EntityTypeBuilder<PageComment> builder)
        {
            builder.ToTable("PageComment", schema: "Pages");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Body).IsRequired();

            builder.HasIndex(e => e.PageId, "IX_PageComment_PageId");

            builder.HasOne(e => e.Page)
                .WithMany(e => e.PageComments)
                .HasForeignKey(e => e.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
