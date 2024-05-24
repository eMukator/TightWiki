﻿using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageRevisionsViewModel : ViewModelBase
    {
        public List<PageRevision> Revisions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
