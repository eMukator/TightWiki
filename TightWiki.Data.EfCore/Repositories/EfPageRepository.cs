using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using TightWiki.Library.Caching;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using PagesEntities = TightWiki.Data.EfCore.Entities.Pages;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Data.EfCore.Repositories
{
    /// <summary>
    /// Provider-agnostic (SQL Server/Postgres, per Database-Providers-Plan.md chapter 3) LINQ-over-EF-Core
    /// implementation of <see cref="ITwPageRepository"/>. Lives in the shared <c>TightWiki.Data.EfCore</c>
    /// project rather than a per-provider driver project, for the same reason as <see cref="EfConfigurationRepository"/>/
    /// <see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/<see cref="EfStatisticsRepository"/> (see
    /// those classes' doc comments): plain LINQ against <see cref="TightWikiDbContext"/> needs no provider-specific
    /// code here at all. Originally landed as a <c>SqlServerPageRepository</c> stub under
    /// <c>TightWiki.Data.EfCore.SqlServer/Repositories/</c> in phase 2a.1; moved here (still a stub) in phase 2b.1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Still a partial skeleton (Database-Providers-Plan.md phase 2b.1/2b.2) - 75 of 86 members still throw
    /// <see cref="NotImplementedException"/>; the remaining 11 (autocomplete, page-cache flushing, page comments,
    /// current-page-editors - see each method's own doc comment for the specific SQLite script it mirrors) were
    /// implemented for real in phase 2b.2. Real LINQ-based implementations of the rest (including the
    /// <c>TempSearchTerms</c> replacement discussed in chapter 4.4) land across phases 2b.3-2b.13.
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/> rather than an injected context instance, mirroring
    /// <see cref="EfConfigurationRepository"/>/<see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/
    /// <see cref="EfStatisticsRepository"/> (see <see cref="EfConfigurationRepository"/>'s doc comment) -
    /// <see cref="SqlServer.SqlServerDatabaseManager"/> passes its own <c>CreateDbContext</c> method group in as
    /// that delegate. Also takes an <see cref="ITwConfigurationRepository"/> instance directly (not another
    /// <see cref="Func{TResult}"/>), mirroring the SQLite reference constructor's own
    /// <c>ConfigurationRepository configurationRepository</c> parameter and the same pattern already used by
    /// <see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/<see cref="EfStatisticsRepository"/> -
    /// <see cref="GetPageCommentsPaged"/> is the only phase-2b.2 member that needs it, to read the "Pagination
    /// Size" customization setting.
    /// </para>
    /// </remarks>
    public sealed class EfPageRepository : ITwPageRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly ITwConfigurationRepository _configurationRepository;

        public EfPageRepository(Func<TightWikiDbContext> createContext, ITwConfigurationRepository configurationRepository)
        {
            _createContext = createContext;
            _configurationRepository = configurationRepository;
        }

        /// <summary>
        /// Mirrors AutoCompletePage.sql: pages whose <see cref="TwPage.Name"/> contains
        /// <paramref name="searchText"/> (an empty string, matching everything, if null - same as the reference's
        /// <c>searchText ?? string.Empty</c>), ordered by Name, capped at 25 rows. No caching, matching the SQLite
        /// reference. <see cref="TwPage.Namespace"/> is deliberately left unset here even though the reference SQL
        /// also selects the (persisted, redundant) Namespace column - <see cref="TwPage.Namespace"/> is a get-only
        /// property computed from <see cref="TwPage.Name"/>, so Dapper silently drops that column too when
        /// mapping the SQLite reference's result set onto the same type.
        /// </summary>
        public async Task<List<TwPage>> AutoCompletePage(string? searchText)
        {
            using var context = _createContext();

            var text = searchText ?? string.Empty;

            return await context.Pages_Pages
                .Where(p => p.Name.Contains(text))
                .OrderBy(p => p.Name)
                .Take(25)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Navigation = p.Navigation,
                    Name = p.Name,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors AutoCompleteNamespace.sql: distinct <see cref="PagesEntities.Page.Namespace"/> values
        /// containing <paramref name="searchText"/> (an empty string, matching everything, if null), capped at 25
        /// rows. No caching, matching the SQLite reference. Ordered by the namespace value itself, rather than
        /// literally reproducing the reference script's "SELECT DISTINCT [Namespace] ... ORDER BY [Name]" - SQLite
        /// tolerates ordering by a column outside a DISTINCT projection, but that construct has no portable
        /// LINQ/SQL Server equivalent, and once only Namespace is projected there is no well-defined per-namespace
        /// "Name" left to order by anyway (a namespace groups many pages/Names).
        /// </summary>
        public async Task<List<string>> AutoCompleteNamespace(string? searchText)
        {
            using var context = _createContext();

            var text = searchText ?? string.Empty;

            return await context.Pages_Pages
                .Where(p => p.Namespace.Contains(text))
                .Select(p => p.Namespace)
                .Distinct()
                .OrderBy(n => n)
                .Take(25)
                .ToListAsync();
        }

        public Task<TwPage?> GetPageRevisionInfoById(int pageId, int? revision = null)
            => throw new NotImplementedException();

        public Task<TwProcessingInstructionCollection> GetPageProcessingInstructionsByPageId(int pageId)
            => throw new NotImplementedException();

        public Task<List<TwPageTag>> GetPageTagsById(int pageId)
            => throw new NotImplementedException();

        public Task<List<TwPageRevision>> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => throw new NotImplementedException();

        public Task<List<TwPageRevision>> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
            => throw new NotImplementedException();

        public Task<string?> GetPageNavigationByPageId(int pageId)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetTopRecentlyModifiedPagesInfo(int topCount)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetTopRecentlyCreatedPagesInfo(int topCount)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetTopViewedPagesInfo(int topCount)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetTopEditedPagesInfo(int topCount)
            => throw new NotImplementedException();

        public Task<List<TwPage>> PageSearch(List<string> searchTerms)
            => throw new NotImplementedException();

        public Task<List<TwPage>> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
            => throw new NotImplementedException();

        public Task<List<TwRelatedPage>> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
            => throw new NotImplementedException();

        public Task<List<TwRelatedPage>> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
            => throw new NotImplementedException();

        public Task<List<TwRelatedPage>> GetBacklinkPagesPaged(int pageId, int pageNumber, int? pageSize = null)
            => throw new NotImplementedException();

        /// <summary>
        /// Mirrors <c>PageRepository.FlushPageCache</c>: clears every <see cref="MemCache.Category.Page"/> cache
        /// entry whose key starts with the page's navigation, and every one whose key starts with its id - the
        /// same two <see cref="MemCache.ClearCategory(MemCacheKey)"/> calls as the SQLite reference. Unlike the
        /// reference, this resolves the page's navigation with a direct, local query (mirroring
        /// GetPageNavigationByPageId.sql itself) rather than calling the public
        /// <see cref="GetPageNavigationByPageId"/> interface member, which - like the 74 other members of this
        /// class not covered by phase 2b.2 - is still a <see cref="NotImplementedException"/> stub; several of
        /// the methods implemented in phase 2b.2 (<see cref="InsertPageComment"/>, <see cref="DeletePageCommentById"/>,
        /// <see cref="DeletePageCommentByUserAndId"/>) call this method and would otherwise always fail.
        /// </summary>
        public async Task FlushPageCache(int pageId)
        {
            using var context = _createContext();

            var pageNavigation = await context.Pages_Pages
                .Where(p => p.Id == pageId)
                .Select(p => (string?)p.Navigation)
                .FirstOrDefaultAsync();

            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [pageNavigation]));
            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [pageId]));
        }

        /// <summary>
        /// Mirrors InsertPageComment.sql: inserts a new Pages.PageComment row (<see cref="PagesEntities.PageComment.CreatedDate"/>
        /// stamped to now, in UTC), then flushes this page's cache via <see cref="FlushPageCache"/> - same as the
        /// SQLite reference.
        /// </summary>
        public async Task InsertPageComment(int pageId, Guid userId, string body)
        {
            using var context = _createContext();

            context.Pages_PageComments.Add(new PagesEntities.PageComment
            {
                PageId = pageId,
                UserId = userId,
                Body = body,
                CreatedDate = DateTime.UtcNow,
            });

            await context.SaveChangesAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors DeletePageCommentById.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> (fully
        /// provider-portable, no raw SQL needed), then flushes this page's cache via <see cref="FlushPageCache"/> -
        /// same as the SQLite reference.
        /// </summary>
        public async Task DeletePageCommentById(int pageId, int commentId)
        {
            using var context = _createContext();

            await context.Pages_PageComments
                .Where(c => c.PageId == pageId && c.Id == commentId)
                .ExecuteDeleteAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors DeletePageCommentByUserAndId.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - like the
        /// reference, only deletes the comment if <paramref name="userId"/> also matches its author, then flushes
        /// this page's cache via <see cref="FlushPageCache"/> regardless of whether a row was actually deleted -
        /// same as the SQLite reference.
        /// </summary>
        public async Task DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId)
        {
            using var context = _createContext();

            await context.Pages_PageComments
                .Where(c => c.PageId == pageId && c.UserId == userId && c.Id == commentId)
                .ExecuteDeleteAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors GetTotalPageCommentCount.sql: the count of Pages.PageComment rows for <paramref name="pageId"/>.
        /// </summary>
        public async Task<int> GetTotalPageCommentCount(int pageId)
        {
            using var context = _createContext();

            return await context.Pages_PageComments.CountAsync(c => c.PageId == pageId);
        }

        /// <summary>
        /// Mirrors GetPageCommentsPaged.sql: an inner join of Pages.PageComment to its owning Pages.Page (filtered
        /// by <paramref name="navigation"/>) LEFT OUTER JOINed to Users.Profile for the comment author - via the
        /// existing <see cref="PagesEntities.PageComment.Page"/>/<see cref="PagesEntities.PageComment.User"/>
        /// navigations rather than a raw cross-database <c>ATTACH</c> (the SQLite reference's
        /// <c>o.Attach("users.db", "users_db")</c>), since both schemas already live in the same
        /// <see cref="TightWikiDbContext"/> - ordered by <see cref="PagesEntities.PageComment.CreatedDate"/>
        /// descending, paginated by the "Pagination Size" customization setting.
        /// <see cref="TwPageComment.PaginationPageCount"/> is computed via the reference's own ceiling-division
        /// formula (<c>(Count(0) + (@PageSize - 1)) / @PageSize</c>) against the total (unpaginated) comment count
        /// for the page. <see cref="TwPageComment.UserNavigation"/> is deliberately populated from
        /// <c>Profile.AccountName</c>, not <c>Profile.Navigation</c> - a literal quirk of the reference script
        /// (<c>U.AccountName as UserNavigation</c>), preserved rather than "fixed" here. Cached under
        /// <see cref="MemCache.Category.Page"/>, same cache key shape (navigation + page number + page size) as
        /// the SQLite reference.
        /// </summary>
        public async Task<List<TwPageComment>> GetPageCommentsPaged(string navigation, int pageNumber)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [navigation, pageNumber, paginationSize]);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                var commentsForPage = context.Pages_PageComments.Where(c => c.Page.Navigation == navigation);

                var totalCommentCount = await commentsForPage.CountAsync();
                var paginationPageCount = (totalCommentCount + (paginationSize - 1)) / paginationSize;

                return await commentsForPage
                    .OrderByDescending(c => c.CreatedDate)
                    .Skip((pageNumber - 1) * paginationSize)
                    .Take(paginationSize)
                    .Select(c => new TwPageComment
                    {
                        Id = c.Id,
                        PageId = c.PageId,
                        CreatedDate = c.CreatedDate,
                        UserId = c.UserId,
                        Body = c.Body,
                        UserName = c.User != null ? (c.User.AccountName ?? string.Empty) : string.Empty,
                        UserNavigation = c.User != null ? (c.User.AccountName ?? string.Empty) : string.Empty,
                        PageName = c.Page.Name,
                        PaginationPageCount = paginationPageCount,
                    })
                    .ToListAsync();
            })).EnsureNotNull();
        }

        public Task<List<TwNonexistentPage>> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
            => throw new NotImplementedException();

        public Task UpdateSinglePageReference(string pageNavigation, int pageId)
            => throw new NotImplementedException();

        public Task UpdatePageReferences(int pageId, List<TwPageReference> referencesPageNavigations)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
            => throw new NotImplementedException();

        public Task<List<int>> GetDeletedPageIdsByTokens(List<string>? tokens)
            => throw new NotImplementedException();

        public Task<List<int>> GetPageIdsByTokens(List<string>? tokens)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetAllNamespacePagesPaged(int pageNumber, string namespaceName, string? orderBy = null, string? orderByDirection = null)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetAllPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
            => throw new NotImplementedException();

        public Task<List<TwNamespaceStat>> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
            => throw new NotImplementedException();

        public Task<List<string>> GetAllNamespaces()
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetAllPages()
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetAllTemplatePages()
            => throw new NotImplementedException();

        public Task<List<TwFeatureTemplate>> GetAllFeatureTemplates()
            => throw new NotImplementedException();

        public Task UpdatePageProcessingInstructions(int pageId, List<string> instructions)
            => throw new NotImplementedException();

        public Task<TwPage?> GetPageRevisionById(int pageId, int? revision = null)
            => throw new NotImplementedException();

        public Task<List<TwPageToken>> GetSearchTokensByPageId(int pageId)
            => throw new NotImplementedException();

        public Task SavePageSearchTokens(List<TwPageToken> items)
            => throw new NotImplementedException();

        public Task TruncateAllPageRevisions(string confirm)
            => throw new NotImplementedException();

        public Task<int> GetCurrentPageRevision(int pageId)
            => throw new NotImplementedException();

        public Task<TwPage?> GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null)
            => throw new NotImplementedException();

        public Task<TwPage?> GetPageInfoByNavigation(string navigation)
            => throw new NotImplementedException();

        public Task<int> GetPageRevisionCountByPageId(int pageId)
            => throw new NotImplementedException();

        public Task RestoreDeletedPageByPageId(int pageId)
            => throw new NotImplementedException();

        public Task MovePageRevisionToDeletedById(int pageId, int revision, Guid userId)
            => throw new NotImplementedException();

        public Task MovePageToDeletedById(int pageId, Guid userId)
            => throw new NotImplementedException();

        public Task PurgeDeletedPageByPageId(int pageId)
            => throw new NotImplementedException();

        public Task PurgeDeletedPages()
            => throw new NotImplementedException();

        public Task<int> GetCountOfPageAttachmentsById(int pageId)
            => throw new NotImplementedException();

        public Task<TwPage?> GetDeletedPageById(int pageId)
            => throw new NotImplementedException();

        public Task<TwPage?> GetLatestPageRevisionById(int pageId)
            => throw new NotImplementedException();

        public Task<int> GetPageNextRevision(int pageId, int revision)
            => throw new NotImplementedException();

        public Task<int> GetPagePreviousRevision(int pageId, int revision)
            => throw new NotImplementedException();

        public Task<List<TwDeletedPageRevision>> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber, string? orderBy = null, string? orderByDirection = null)
            => throw new NotImplementedException();

        public Task PurgeDeletedPageRevisions()
            => throw new NotImplementedException();

        public Task PurgeDeletedPageRevisionsByPageId(int pageId)
            => throw new NotImplementedException();

        public Task PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
            => throw new NotImplementedException();

        public Task RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
            => throw new NotImplementedException();

        public Task<TwDeletedPageRevision?> GetDeletedPageRevisionById(int pageId, int revision)
            => throw new NotImplementedException();

        public Task<TwPage?> GetPageRevisionByNavigation(TwNamespaceNavigation navigation, int? revision = null)
            => throw new NotImplementedException();

        public Task<TwPage?> GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false)
            => throw new NotImplementedException();

        public Task<List<TwTagAssociation>> GetAssociatedTags(string tag)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetPageInfoByNamespaces(List<string> namespaces)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetPageInfoByTags(IEnumerable<string> tags)
            => throw new NotImplementedException();

        public Task<List<TwPage>> GetPageInfoByTag(string tag)
            => throw new NotImplementedException();

        public Task UpdatePageTags(int pageId, List<string> tags)
            => throw new NotImplementedException();

        public Task<int> UpsertPage(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null)
            => throw new NotImplementedException();

        public Task RefreshPageMetadata(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null)
            => throw new NotImplementedException();

        public Task<List<TwAggregatedSearchToken>> ParsePageTokens(ITwEngineState state)
            => throw new NotImplementedException();

        #region Page File.

        public Task DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision)
            => throw new NotImplementedException();

        public Task<List<TwOrphanedPageAttachment>> GetOrphanedPageAttachmentsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
            => throw new NotImplementedException();

        public Task PurgeOrphanedPageAttachments()
            => throw new NotImplementedException();

        public Task PurgeOrphanedPageAttachment(int pageFileId, int revision)
            => throw new NotImplementedException();

        public Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
            => throw new NotImplementedException();

        public Task<TwPageFileAttachmentInfo?> GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
            => throw new NotImplementedException();

        public Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null)
            => throw new NotImplementedException();

        public Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
            => throw new NotImplementedException();

        public Task<List<TwPageFileAttachmentInfo>> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber)
            => throw new NotImplementedException();

        public Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageId(int pageId)
            => throw new NotImplementedException();

        public Task UpsertPageFile(TwPageFileAttachment item, Guid userId)
            => throw new NotImplementedException();

        #endregion

        #region Current page editors.

        /// <summary>
        /// Mirrors UpsertCurrentPageEditor.sql's two statements: an "INSERT ... ON CONFLICT(PageId, UserId) DO
        /// UPDATE" upsert of the Pages.CurrentPageEditors row for (<paramref name="pageId"/>,
        /// <paramref name="userId"/>) - refreshing <see cref="PagesEntities.CurrentPageEditor.AccountName"/>/
        /// <see cref="PagesEntities.CurrentPageEditor.UtcDate"/> to now if the row already exists - followed by an
        /// unconditional delete of every row older than one day. EF Core's LINQ surface has no portable equivalent
        /// of that single atomic "ON CONFLICT" statement, so the upsert is implemented as "try an
        /// <see cref="Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync{TSource}"/>
        /// first, insert only if it affected zero rows" - the same non-atomic pattern (and narrow race-condition
        /// caveat) as <see cref="EfStatisticsRepository.IncrementPageViewCount"/>/
        /// <see cref="EfStatisticsRepository.MergePageCompilationStatistics"/>. Does not call
        /// <see cref="FlushPageCache"/> - matching the SQLite reference, which touches no cache at all here.
        /// </summary>
        public async Task UpsertCurrentPageEditor(int pageId, Guid userId, string accountName)
        {
            using var context = _createContext();

            var utcNow = DateTime.UtcNow;

            var updated = await context.CurrentPageEditors
                .Where(e => e.PageId == pageId && e.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.AccountName, accountName)
                    .SetProperty(e => e.UtcDate, utcNow));

            if (updated == 0)
            {
                context.CurrentPageEditors.Add(new PagesEntities.CurrentPageEditor
                {
                    PageId = pageId,
                    UserId = userId,
                    AccountName = accountName,
                    UtcDate = utcNow,
                });

                await context.SaveChangesAsync();
            }

            var deleteThresholdDate = utcNow.AddDays(-1);

            await context.CurrentPageEditors
                .Where(e => e.UtcDate < deleteThresholdDate)
                .ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors DeleteCurrentPageEditor.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - fully
        /// provider-portable, no raw SQL needed.
        /// </summary>
        public async Task DeleteCurrentPageEditor(int pageId, Guid userId)
        {
            using var context = _createContext();

            await context.CurrentPageEditors
                .Where(e => e.PageId == pageId && e.UserId == userId)
                .ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors GetCurrentPageEditors.sql: the account names of every Pages.CurrentPageEditors row for
        /// <paramref name="pageId"/> whose <see cref="PagesEntities.CurrentPageEditor.UtcDate"/> is within the
        /// last <paramref name="windowMinutes"/> minutes.
        /// </summary>
        public async Task<List<string>> GetCurrentPageEditors(int pageId, int windowMinutes = 5)
        {
            using var context = _createContext();

            var thresholdDate = DateTime.UtcNow.AddMinutes(-windowMinutes);

            return await context.CurrentPageEditors
                .Where(e => e.PageId == pageId && e.UtcDate >= thresholdDate)
                .Select(e => e.AccountName)
                .ToListAsync();
        }

        #endregion
    }
}
