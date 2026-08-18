using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="DeletionMeta"/> (DeletedPages.DeletionMeta).
    /// </summary>
    /// <remarks>
    /// No CREATE TABLE script exists for this table (see Database-Providers-Plan.md, chapter 2.1a) - schema
    /// verified directly against the live Data/deletedpages.db via PRAGMA table_info/index_list. The table is
    /// entirely empty in the local dev database (no page has ever been soft-deleted there), so the raw scaffold's
    /// typing is based purely on the declared SQLite column affinity, not on any real data. DeletedByUserId/
    /// DeletedDate are declared INTEGER in the DDL but application code always writes a Guid and a DateTime into
    /// them respectively (PageRepository.MovePageToDeletedById/MovePageRevisionToDeletedById); SQLite's dynamic
    /// typing stores the actual TEXT values regardless of the column's declared INTEGER affinity. Modeled as
    /// Guid?/DateTime? accordingly, matching the domain model and the *ByUserId/*Date columns elsewhere in this
    /// schema, rather than the raw scaffold's naive int?/int?.
    /// </remarks>
    public class DeletionMetaConfiguration : IEntityTypeConfiguration<DeletionMeta>
    {
        public void Configure(EntityTypeBuilder<DeletionMeta> builder)
        {
            builder.ToTable("DeletionMeta", schema: "DeletedPages");

            builder.HasKey(e => e.PageId);

            //PageId is copied verbatim from the source page - not database-generated.
            builder.Property(e => e.PageId).ValueGeneratedNever();
        }
    }
}
