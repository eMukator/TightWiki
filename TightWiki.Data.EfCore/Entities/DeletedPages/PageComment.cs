namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// A comment that was posted on a page before it was soft-deleted (DeletedPages.PageComment), moved here
    /// verbatim from Pages.PageComment.
    /// </summary>
    public class PageComment
    {
        /// <summary>
        /// The identifier of this comment (copied verbatim from the original comment - not database-generated).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the deleted page this comment was posted on.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The date and time this comment was posted.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The identifier of the user who posted this comment.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The text content of this comment.
        /// </summary>
        public string Body { get; set; } = string.Empty;
    }
}
