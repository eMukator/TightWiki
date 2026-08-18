namespace TightWiki.Data.EfCore.Entities.DeletedPageRevisions
{
    /// <summary>
    /// Deletion metadata (who/when) for a single soft-deleted page revision (DeletedPageRevisions.DeletionMeta).
    /// One row per deleted <see cref="PageRevision"/>.
    /// </summary>
    public class DeletionMeta
    {
        /// <summary>
        /// The identifier of the page the deleted revision belonged to. Part of the composite primary key
        /// together with <see cref="Revision"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The revision number that was deleted. Part of the composite primary key together with
        /// <see cref="PageId"/>.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The identifier of the user who deleted the revision. Nullable in the real schema. Modeled as Guid
        /// rather than the raw scaffold's int? - see the Fluent configuration.
        /// </summary>
        public Guid? DeletedByUserId { get; set; }

        /// <summary>
        /// The date and time the revision was deleted. Nullable in the real schema. Modeled as DateTime rather
        /// than the raw scaffold's int? - see the Fluent configuration.
        /// </summary>
        public DateTime? DeletedDate { get; set; }
    }
}
