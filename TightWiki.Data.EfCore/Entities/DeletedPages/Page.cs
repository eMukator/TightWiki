namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// A soft-deleted wiki page (DeletedPages.Page), moved here verbatim from Pages.Page by
    /// PageRepository.MovePageToDeletedById. <see cref="Id"/> is copied verbatim from the original page.
    /// </summary>
    public class Page
    {
        /// <summary>
        /// The identifier of the deleted page (copied verbatim from the original page - not database-generated).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The fully-qualified, case-insensitive name of the page at the time of deletion.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The namespace prefix of the page at the time of deletion.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The case-insensitive navigation path of the page at the time of deletion.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The description of the page at the time of deletion.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The revision number of the page at the time of deletion.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The identifier of the user who originally created the page.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// The date and time the page was originally created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The identifier of the user who last modified the page before deletion.
        /// </summary>
        public Guid ModifiedByUserId { get; set; }

        /// <summary>
        /// The date and time the page was last modified before deletion.
        /// </summary>
        public DateTime ModifiedDate { get; set; }
    }
}
