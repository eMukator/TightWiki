namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// Tracks a user who is currently editing a page, for the "someone else is editing this page" UI
    /// (Pages.CurrentPageEditors). Rows older than a sliding window are periodically purged by application code.
    /// </summary>
    public class CurrentPageEditor
    {
        /// <summary>
        /// The identifier of the page being edited. Part of the composite primary key together with
        /// <see cref="UserId"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The identifier of the user editing the page. Part of the composite primary key together with
        /// <see cref="PageId"/>.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The account name of the editing user, denormalized here to avoid a join.
        /// </summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// The UTC date/time this editing session was last refreshed. Column name in the real schema is
        /// "UTCDate".
        /// </summary>
        public DateTime UtcDate { get; set; }
    }
}
