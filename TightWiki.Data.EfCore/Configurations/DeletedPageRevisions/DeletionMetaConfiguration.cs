using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPageRevisions;

namespace TightWiki.Data.EfCore.Configurations.DeletedPageRevisions
{
    /// <summary>
    /// Fluent configuration for <see cref="DeletionMeta"/> (DeletedPageRevisions.DeletionMeta).
    /// </summary>
    /// <remarks>
    /// No CREATE TABLE script exists for this table (see Database-Providers-Plan.md, chapter 2.1a) - schema
    /// verified directly against the live Data/deletedpagerevisions.db via PRAGMA table_info/index_list. The
    /// table is entirely empty in the local dev database, so the raw scaffold's typing is based purely on the
    /// declared SQLite column affinity, not on any real data. DeletedByUserId/DeletedDate are declared INTEGER
    /// in the DDL but application code always writes a Guid and a DateTime into them respectively
    /// (PageRepository.MovePageRevisionToDeletedById); modeled as Guid?/DateTime? accordingly, matching
    /// DeletedPages.DeletionMeta.
    /// </remarks>
    public class DeletionMetaConfiguration : IEntityTypeConfiguration<DeletionMeta>
    {
        public void Configure(EntityTypeBuilder<DeletionMeta> builder)
        {
            builder.ToTable("DeletionMeta", schema: "DeletedPageRevisions");

            builder.HasKey(e => new { e.PageId, e.Revision });

            //Composite integer key - EF's value-generation-on-add convention only applies to single-property
            //integer keys, so no explicit ValueGeneratedNever() is needed here (unlike the single-column
            //DeletedPages.DeletionMeta.PageId).
        }
    }
}
