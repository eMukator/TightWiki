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
    /// Still a partial skeleton (Database-Providers-Plan.md phase 2b.1-2b.3) - 56 of 86 members still throw
    /// <see cref="NotImplementedException"/>. The first 11 (autocomplete, page-cache flushing, page comments,
    /// current-page-editors) were implemented for real in phase 2b.2; a further 19 page/revision metadata-read
    /// members (<see cref="GetPageRevisionInfoById"/>, <see cref="GetPageNavigationByPageId"/>,
    /// <see cref="GetTopRecentlyModifiedPagesInfoByUserId"/>, <see cref="GetTopRecentlyModifiedPagesInfo"/>,
    /// <see cref="GetTopRecentlyCreatedPagesInfo"/>, <see cref="GetTopViewedPagesInfo"/>,
    /// <see cref="GetTopEditedPagesInfo"/>, <see cref="GetPageRevisionsInfoByNavigationPaged"/>,
    /// <see cref="GetCurrentPageRevision"/>, <see cref="GetLimitedPageInfoByIdAndRevision"/>,
    /// <see cref="GetPageInfoByNavigation"/>, <see cref="GetPageRevisionCountByPageId"/>,
    /// <see cref="GetPageNextRevision"/>, <see cref="GetPagePreviousRevision"/>, <see cref="GetPageRevisionById"/>,
    /// <see cref="GetLatestPageRevisionById"/>, both <see cref="GetPageRevisionByNavigation(TwNamespaceNavigation, int?)"/>
    /// overloads, and <see cref="GetCountOfPageAttachmentsById"/> - see each method's own doc comment for the
    /// specific SQLite script it mirrors) landed in phase 2b.3. Real LINQ-based implementations of the rest
    /// (including the <c>TempSearchTerms</c> replacement discussed in chapter 4.4) land across phases 2b.4-2b.13.
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

        /// <summary>
        /// Mirrors GetPageRevisionInfoById.sql: an inner join of Pages.Page to Pages.PageRevision for
        /// <paramref name="pageId"/>, at <paramref name="revision"/> (falling back to the page's current
        /// <see cref="PagesEntities.Page.Revision"/> when null - the script's own <c>COALESCE(@Revision, P.Revision)</c>).
        /// <see cref="TwPage.ModifiedByUserId"/>/<see cref="TwPage.ModifiedDate"/> are populated from the
        /// <see cref="PagesEntities.PageRevision"/> row (<c>PR.ModifiedByUserId</c>/<c>PR.ModifiedDate</c>), not
        /// from <see cref="PagesEntities.Page"/> itself - matching the script exactly (contrast with
        /// <see cref="GetPageRevisionById"/>, whose reference script pulls those two columns from <c>P</c>
        /// instead). No caching, matching the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            using var context = _createContext();

            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                           where p.Id == pageId && pr.Revision == (revision ?? p.Revision)
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = p.Description,
                               Revision = pr.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = pr.ModifiedByUserId,
                               ModifiedDate = pr.ModifiedDate,
                           }).SingleOrDefaultAsync();
        }

        public Task<TwProcessingInstructionCollection> GetPageProcessingInstructionsByPageId(int pageId)
            => throw new NotImplementedException();

        public Task<List<TwPageTag>> GetPageTagsById(int pageId)
            => throw new NotImplementedException();

        /// <summary>
        /// Mirrors GetPageRevisionsInfoByNavigationPaged.sql: every Pages.PageRevision row for the page matching
        /// <paramref name="navigation"/>, inner-joined to its owning Pages.Page, LEFT OUTER JOINed to Users.Profile
        /// for both the revision's own modifier (<c>ModifiedUser</c>) and the page's original creator
        /// (<c>Createduser</c>) - via the existing <see cref="PagesEntities.PageRevision.ModifiedByUser"/>/
        /// <see cref="PagesEntities.Page.CreatedByUser"/> navigations rather than a raw cross-database
        /// <c>ATTACH</c> (the script's own <c>o.Attach("users.db", "users_db")</c>), since both schemas already
        /// live in the same <see cref="TightWikiDbContext"/>. <see cref="TwPageRevision.HigherRevisionCount"/> is
        /// computed the same way as the script's own correlated subquery (count of sibling revisions with a
        /// higher <see cref="PagesEntities.PageRevision.Revision"/>). <paramref name="pageSize"/> defaults to the
        /// "Pagination Size" customization setting, same as the reference, and
        /// <see cref="TwPageRevision.PaginationPageCount"/> is computed via the reference's own ceiling-division
        /// formula against the total (unpaginated) revision count for the page. Ordering mirrors
        /// <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c> mapping
        /// ("Revision"/"ModifiedBy"/"ModifiedDate"/"Page"): no <paramref name="orderBy"/> falls back to the
        /// script's own un-transposed "ORDER BY PR.Revision DESC"; an unrecognized <paramref name="orderBy"/>
        /// throws, same as <c>RepositoryHelpers.TransposeOrderby</c>'s "No order by mapping..." exception (see
        /// <see cref="EfStatisticsRepository.GetPageStatisticsPaged"/> for the same pattern); direction is
        /// ascending only when <paramref name="orderByDirection"/> is exactly "asc" (case-insensitively),
        /// descending for anything else including null.
        /// </summary>
        public async Task<List<TwPageRevision>> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var joined = from p in context.Pages_Pages
                         join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                         where p.Navigation == navigation
                         select new { p, pr };

            var totalCount = await joined.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? joined.OrderByDescending(x => x.pr.Revision)
                : orderBy.ToUpperInvariant() switch
                {
                    "REVISION" => ascending ? joined.OrderBy(x => x.pr.Revision) : joined.OrderByDescending(x => x.pr.Revision),
                    "MODIFIEDBY" => ascending
                        ? joined.OrderBy(x => x.pr.ModifiedByUser != null ? x.pr.ModifiedByUser.AccountName : null)
                        : joined.OrderByDescending(x => x.pr.ModifiedByUser != null ? x.pr.ModifiedByUser.AccountName : null),
                    "MODIFIEDDATE" => ascending ? joined.OrderBy(x => x.pr.ModifiedDate) : joined.OrderByDescending(x => x.pr.ModifiedDate),
                    "PAGE" => ascending ? joined.OrderBy(x => x.pr.Name) : joined.OrderByDescending(x => x.pr.Name),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetPageRevisionsInfoByNavigationPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(x => new TwPageRevision
                {
                    PageId = x.p.Id,
                    Name = x.pr.Name,
                    Description = x.pr.Description,
                    Revision = x.pr.Revision,
                    HighestRevision = x.p.Revision,
                    ChangeSummary = x.pr.ChangeSummary ?? string.Empty,
                    Navigation = x.p.Navigation,
                    CreatedByUserId = x.p.CreatedByUserId,
                    CreatedByUserName = x.p.CreatedByUser != null ? (x.p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    CreatedDate = x.p.CreatedDate,
                    ModifiedByUserId = x.pr.ModifiedByUserId,
                    ModifiedByUserName = x.pr.ModifiedByUser != null ? (x.pr.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedDate = x.pr.ModifiedDate,
                    HigherRevisionCount = context.Pages_PageRevisions.Count(ipr => ipr.PageId == x.p.Id && ipr.Revision > x.pr.Revision),
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetTopRecentlyModifiedPagesInfoByUserId.sql: every Pages.Page row modified by
        /// <paramref name="userId"/>, ordered by <see cref="PagesEntities.Page.ModifiedDate"/> descending then
        /// <see cref="PagesEntities.Page.Name"/> ascending, capped at <paramref name="topCount"/>.
        /// <see cref="TwPageRevision.PageId"/> is deliberately left unset (0) here - a literal quirk of the
        /// reference script, which selects <c>P.Id</c> but <see cref="TwPageRevision"/> has no "Id" property (only
        /// "PageId"), so Dapper silently drops that column when mapping the reference's result set onto the same
        /// type (the one caller, <c>ProfileController</c>, only reads <see cref="TwPageRevision.Navigation"/>/
        /// <see cref="TwPageRevision.Revision"/> off the result, never <see cref="TwPageRevision.PageId"/>, so this
        /// has no observable behavioral impact).
        /// </summary>
        public async Task<List<TwPageRevision>> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Where(p => p.ModifiedByUserId == userId)
                .OrderByDescending(p => p.ModifiedDate)
                .ThenBy(p => p.Name)
                .Take(topCount)
                .Select(p => new TwPageRevision
                {
                    Name = p.Name,
                    Description = p.Description,
                    Revision = p.Revision,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageNavigationByPageId.sql: the <see cref="PagesEntities.Page.Navigation"/> of the page
        /// matching <paramref name="pageId"/>, or null if no such page exists (the script's
        /// <c>ExecuteScalarAsync&lt;string&gt;</c> over a query with no matching row). Same query
        /// <see cref="FlushPageCache"/> runs inline for the same reason (see that method's doc comment).
        /// </summary>
        public async Task<string?> GetPageNavigationByPageId(int pageId)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Where(p => p.Id == pageId)
                .Select(p => (string?)p.Navigation)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetTopRecentlyModifiedPagesInfo.sql: every Pages.Page row inner-joined to the
        /// Pages.PageRevision row matching its own current <see cref="PagesEntities.Page.Revision"/> (so
        /// <see cref="TwPage.ModifiedByUserId"/>/<see cref="TwPage.ModifiedDate"/> come from that current
        /// revision row, not from <see cref="PagesEntities.Page"/> itself), ordered by
        /// <see cref="PagesEntities.Page.ModifiedDate"/> descending then <see cref="PagesEntities.Page.Name"/>
        /// ascending, capped at <paramref name="topCount"/>.
        /// </summary>
        public async Task<List<TwPage>> GetTopRecentlyModifiedPagesInfo(int topCount)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Join(context.Pages_PageRevisions,
                    p => new { p.Id, p.Revision },
                    pr => new { Id = pr.PageId, pr.Revision },
                    (p, pr) => new { p, pr })
                .OrderByDescending(x => x.p.ModifiedDate)
                .ThenBy(x => x.p.Name)
                .Take(topCount)
                .Select(x => new TwPage
                {
                    Id = x.p.Id,
                    Name = x.p.Name,
                    Description = x.p.Description,
                    Revision = x.p.Revision,
                    Navigation = x.p.Navigation,
                    CreatedByUserId = x.p.CreatedByUserId,
                    CreatedDate = x.p.CreatedDate,
                    ModifiedByUserId = x.pr.ModifiedByUserId,
                    ModifiedDate = x.pr.ModifiedDate,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetTopRecentlyCreatedPagesInfo.sql: every Pages.Page row ordered by
        /// <see cref="PagesEntities.Page.CreatedDate"/> descending then <see cref="PagesEntities.Page.Name"/>
        /// ascending, capped at <paramref name="topCount"/>. The script's own <c>IFNULL(P.ModifiedDate,
        /// P.CreatedDate)</c> is not reproduced as a client-side fallback - <see cref="PagesEntities.Page.ModifiedDate"/>
        /// is a <c>NOT NULL</c> column in the real schema (verified against the live SQLite schema), so the
        /// column value itself is used directly.
        /// </summary>
        public async Task<List<TwPage>> GetTopRecentlyCreatedPagesInfo(int topCount)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .OrderByDescending(p => p.CreatedDate)
                .ThenBy(p => p.Name)
                .Take(topCount)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Revision = p.Revision,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetTopViewedPagesInfo.sql: every Statistics.PageStatistics row inner-joined to its owning
        /// Pages.Page (via the existing, required <see cref="Statistics.PageStatistic.Page"/> navigation rather
        /// than the script's own cross-database <c>o.Attach("statistics.db", "statistics_db")</c>, since both
        /// schemas already live in the same <see cref="TightWikiDbContext"/>), ordered by
        /// <see cref="Statistics.PageStatistic.TotalViewCount"/> descending then <see cref="PagesEntities.Page.Name"/>
        /// ascending, capped at <paramref name="topCount"/>.
        /// </summary>
        public async Task<List<TwPage>> GetTopViewedPagesInfo(int topCount)
        {
            using var context = _createContext();

            return await context.PageStatistics
                .OrderByDescending(ps => ps.TotalViewCount)
                .ThenBy(ps => ps.Page.Name)
                .Take(topCount)
                .Select(ps => new TwPage
                {
                    Id = ps.Page.Id,
                    Name = ps.Page.Name,
                    Description = ps.Page.Description,
                    Revision = ps.Page.Revision,
                    Navigation = ps.Page.Navigation,
                    CreatedByUserId = ps.Page.CreatedByUserId,
                    CreatedDate = ps.Page.CreatedDate,
                    ModifiedByUserId = ps.Page.ModifiedByUserId,
                    ModifiedDate = ps.Page.ModifiedDate,
                    TotalViewCount = ps.TotalViewCount,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetTopEditedPagesInfo.sql: every Pages.Page row ordered by
        /// <see cref="PagesEntities.Page.Revision"/> descending then <see cref="PagesEntities.Page.Name"/>
        /// ascending, capped at <paramref name="topCount"/> - "most edited" meaning "highest revision number".
        /// See <see cref="GetTopRecentlyCreatedPagesInfo"/>'s doc comment for why the script's own
        /// <c>IFNULL(P.ModifiedDate, P.CreatedDate)</c> is not reproduced.
        /// </summary>
        public async Task<List<TwPage>> GetTopEditedPagesInfo(int topCount)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .OrderByDescending(p => p.Revision)
                .ThenBy(p => p.Name)
                .Take(topCount)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Revision = p.Revision,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                })
                .ToListAsync();
        }

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

        /// <summary>
        /// Mirrors GetPageRevisionById.sql: an inner join of Pages.Page to Pages.PageRevision for
        /// <paramref name="pageId"/>, at <paramref name="revision"/> (falling back to the page's current
        /// <see cref="PagesEntities.Page.Revision"/> when null), including the revision <see cref="PagesEntities.PageRevision.Body"/>.
        /// <see cref="TwPage.ModifiedByUserId"/>/<see cref="TwPage.ModifiedDate"/> are populated from
        /// <see cref="PagesEntities.Page"/> itself (<c>P.ModifiedByUserId</c>/<c>P.ModifiedDate</c>), not from the
        /// joined <see cref="PagesEntities.PageRevision"/> row - matching the script exactly (contrast with
        /// <see cref="GetPageRevisionInfoById"/>, whose reference script pulls those two columns from <c>PR</c>
        /// instead). Cached under <see cref="MemCache.Category.Page"/>, same cache key shape (pageId + revision)
        /// as the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetPageRevisionById(int pageId, int? revision = null)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId, revision]), async () =>
            {
                using var context = _createContext();

                return await (from p in context.Pages_Pages
                               join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                               where p.Id == pageId && pr.Revision == (revision ?? p.Revision)
                               select new TwPage
                               {
                                   Id = p.Id,
                                   Name = p.Name,
                                   Description = p.Description,
                                   Body = pr.Body,
                                   Revision = pr.Revision,
                                   Navigation = p.Navigation,
                                   CreatedByUserId = p.CreatedByUserId,
                                   CreatedDate = p.CreatedDate,
                                   ModifiedByUserId = p.ModifiedByUserId,
                                   ModifiedDate = p.ModifiedDate,
                               }).SingleOrDefaultAsync();
            });
        }

        public Task<List<TwPageToken>> GetSearchTokensByPageId(int pageId)
            => throw new NotImplementedException();

        public Task SavePageSearchTokens(List<TwPageToken> items)
            => throw new NotImplementedException();

        public Task TruncateAllPageRevisions(string confirm)
            => throw new NotImplementedException();

        /// <summary>
        /// Mirrors GetCurrentPageRevision.sql: the <see cref="PagesEntities.Page.Revision"/> of the page matching
        /// <paramref name="pageId"/>, or 0 if no such page exists (the script's <c>ExecuteScalarAsync&lt;int&gt;</c>
        /// over a query with no matching row). Cached under <see cref="MemCache.Category.Page"/>, same cache key
        /// shape (pageId) as the SQLite reference.
        /// </summary>
        public async Task<int> GetCurrentPageRevision(int pageId)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId]), async () =>
            {
                using var context = _createContext();

                return await context.Pages_Pages
                    .Where(p => p.Id == pageId)
                    .Select(p => p.Revision)
                    .FirstOrDefaultAsync();
            });
        }

        /// <summary>
        /// Mirrors GetLimitedPageInfoByIdAndRevision.sql: an inner join of Pages.Page to Pages.PageRevision for
        /// <paramref name="pageId"/>, at <paramref name="revision"/> (falling back to the page's current
        /// <see cref="PagesEntities.Page.Revision"/> when null), excluding the revision body -
        /// <see cref="TwPage.MostCurrentRevision"/> is populated from <see cref="PagesEntities.Page.Revision"/>
        /// and <see cref="TwPage.Revision"/>/<see cref="TwPage.DataHash"/> from the joined
        /// <see cref="PagesEntities.PageRevision"/> row. <see cref="TwPage.Namespace"/> is deliberately left
        /// unset here even though the reference script also selects the (persisted, redundant) Namespace column -
        /// see <see cref="AutoCompletePage"/>'s doc comment for why. Cached under
        /// <see cref="MemCache.Category.Page"/>, same cache key shape (pageId + revision) as the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId, revision]), async () =>
            {
                using var context = _createContext();

                return await (from p in context.Pages_Pages
                               join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                               where p.Id == pageId && pr.Revision == (revision ?? p.Revision)
                               select new TwPage
                               {
                                   Id = p.Id,
                                   Name = p.Name,
                                   Description = p.Description,
                                   Navigation = p.Navigation,
                                   Revision = pr.Revision,
                                   DataHash = pr.DataHash,
                                   MostCurrentRevision = p.Revision,
                                   CreatedByUserId = p.CreatedByUserId,
                                   CreatedDate = p.CreatedDate,
                                   ModifiedByUserId = p.ModifiedByUserId,
                                   ModifiedDate = p.ModifiedDate,
                               }).SingleOrDefaultAsync();
            });
        }

        /// <summary>
        /// Mirrors GetPageInfoByNavigation.sql: page metadata (excluding content) for the page matching
        /// <paramref name="navigation"/>, or null if no such page exists. Cached under
        /// <see cref="MemCache.Category.Page"/>, same cache key shape (navigation) as the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetPageInfoByNavigation(string navigation)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [navigation]), async () =>
            {
                using var context = _createContext();

                return await context.Pages_Pages
                    .Where(p => p.Navigation == navigation)
                    .Select(p => new TwPage
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Navigation = p.Navigation,
                        Revision = p.Revision,
                        CreatedByUserId = p.CreatedByUserId,
                        CreatedDate = p.CreatedDate,
                        ModifiedByUserId = p.ModifiedByUserId,
                        ModifiedDate = p.ModifiedDate,
                    }).SingleOrDefaultAsync();
            });
        }

        /// <summary>
        /// Mirrors GetPageRevisionCountByNavigation.sql (the script backing this method despite the mismatched
        /// filename - a pre-existing quirk of the SQLite reference, preserved here only in the sense that the
        /// query itself, "count of Pages.PageRevision rows for <paramref name="pageId"/>", is replicated exactly):
        /// the total number of revisions for the page. Cached under <see cref="MemCache.Category.Page"/>, same
        /// cache key shape (pageId) as the SQLite reference.
        /// </summary>
        public async Task<int> GetPageRevisionCountByPageId(int pageId)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId]), async () =>
            {
                using var context = _createContext();

                return await context.Pages_PageRevisions.CountAsync(pr => pr.PageId == pageId);
            });
        }

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

        /// <summary>
        /// Mirrors GetCountOfPageAttachmentsById.sql: the count of Pages.PageFile rows for
        /// <paramref name="pageId"/>.
        /// </summary>
        public async Task<int> GetCountOfPageAttachmentsById(int pageId)
        {
            using var context = _createContext();

            return await context.Pages_PageFiles.CountAsync(f => f.PageId == pageId);
        }

        public Task<TwPage?> GetDeletedPageById(int pageId)
            => throw new NotImplementedException();

        /// <summary>
        /// Mirrors GetLatestPageRevisionById.sql: an inner join of Pages.Page to the Pages.PageRevision row
        /// matching its own current <see cref="PagesEntities.Page.Revision"/>, for <paramref name="pageId"/>.
        /// <see cref="TwPage.ModifiedByUserId"/>/<see cref="TwPage.ModifiedDate"/> are populated from the joined
        /// <see cref="PagesEntities.PageRevision"/> row, but <see cref="TwPage.ModifiedByUserName"/> is resolved
        /// via <see cref="PagesEntities.Page.ModifiedByUser"/> - i.e. keyed off <c>P.ModifiedByUserId</c>, not
        /// <c>PR.ModifiedByUserId</c> - matching the script's own <c>LEFT OUTER JOIN users_db.Profile as MBU ON
        /// MBU.UserId = P.ModifiedByUserId</c> literally (the two are normally in sync for a page's current
        /// revision, but this preserves the exact join predicate rather than "fixing" it).
        /// </summary>
        public async Task<TwPage?> GetLatestPageRevisionById(int pageId)
        {
            using var context = _createContext();

            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
                           where p.Id == pageId
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = pr.Description,
                               Body = pr.Body,
                               Revision = pr.Revision,
                               MostCurrentRevision = p.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = pr.ModifiedByUserId,
                               ModifiedDate = pr.ModifiedDate,
                               ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                           }).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetPageNextRevision.sql: the lowest Pages.PageRevision <see cref="PagesEntities.PageRevision.Revision"/>
        /// for <paramref name="pageId"/> that is greater than <paramref name="revision"/>, or 0 if none exists
        /// (the script's <c>ExecuteScalarAsync&lt;int&gt;</c> over a query with no matching row).
        /// </summary>
        public async Task<int> GetPageNextRevision(int pageId, int revision)
        {
            using var context = _createContext();

            return await context.Pages_PageRevisions
                .Where(pr => pr.PageId == pageId && pr.Revision > revision)
                .OrderBy(pr => pr.Revision)
                .Select(pr => pr.Revision)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetPagePreviousRevision.sql: the highest Pages.PageRevision <see cref="PagesEntities.PageRevision.Revision"/>
        /// for <paramref name="pageId"/> that is less than <paramref name="revision"/>, or 0 if none exists (the
        /// script's <c>ExecuteScalarAsync&lt;int&gt;</c> over a query with no matching row).
        /// </summary>
        public async Task<int> GetPagePreviousRevision(int pageId, int revision)
        {
            using var context = _createContext();

            return await context.Pages_PageRevisions
                .Where(pr => pr.PageId == pageId && pr.Revision < revision)
                .OrderByDescending(pr => pr.Revision)
                .Select(pr => pr.Revision)
                .FirstOrDefaultAsync();
        }

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

        /// <summary>
        /// Shared query behind both <see cref="GetPageRevisionByNavigation(TwNamespaceNavigation, int?)"/> and
        /// <see cref="GetPageRevisionByNavigation(string, int?, bool)"/> - mirrors GetPageRevisionByNavigation.sql:
        /// an inner join of Pages.Page to Pages.PageRevision for <paramref name="canonicalNavigation"/>, at
        /// <paramref name="revision"/> (falling back to the page's current <see cref="PagesEntities.Page.Revision"/>
        /// when null). <see cref="TwPage.ModifiedByUserName"/> is resolved via
        /// <see cref="PagesEntities.Page.ModifiedByUser"/> (keyed off <c>P.ModifiedByUserId</c>, not
        /// <c>PR.ModifiedByUserId</c> - matching the script's own <c>LEFT OUTER JOIN users_db.Profile as MBU ON
        /// MBU.UserId = P.ModifiedByUserId</c> literally, same quirk as <see cref="GetLatestPageRevisionById"/>),
        /// while <see cref="TwPage.CreatedByUserName"/> is resolved via <see cref="PagesEntities.Page.CreatedByUser"/>.
        /// <see cref="TwPage.HigherRevisionCount"/> is computed the same way as the script's own correlated
        /// subquery (count of sibling revisions with a higher <see cref="PagesEntities.PageRevision.Revision"/>).
        /// </summary>
        private static async Task<TwPage?> QueryPageRevisionByNavigation(TightWikiDbContext context, string canonicalNavigation, int? revision)
        {
            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                           where p.Navigation == canonicalNavigation && pr.Revision == (revision ?? p.Revision)
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = pr.Description,
                               Body = pr.Body,
                               Revision = pr.Revision,
                               ChangeSummary = pr.ChangeSummary ?? string.Empty,
                               MostCurrentRevision = p.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = pr.ModifiedByUserId,
                               ModifiedDate = pr.ModifiedDate,
                               ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                               CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                               HigherRevisionCount = context.Pages_PageRevisions.Count(ipr => ipr.PageId == p.Id && ipr.Revision > pr.Revision),
                           }).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.GetPageRevisionByNavigation(TwNamespaceNavigation, int?)</c>: looks up via
        /// <see cref="QueryPageRevisionByNavigation"/> using <paramref name="navigation"/>'s
        /// <see cref="TwNamespaceNavigation.Canonical"/> form. No caching, matching the SQLite reference (unlike
        /// the string overload below).
        /// </summary>
        public async Task<TwPage?> GetPageRevisionByNavigation(TwNamespaceNavigation navigation, int? revision = null)
        {
            using var context = _createContext();

            return await QueryPageRevisionByNavigation(context, navigation.Canonical, revision);
        }

        /// <summary>
        /// Mirrors <c>PageRepository.GetPageRevisionByNavigation(string, int?, bool)</c>: normalizes
        /// <paramref name="givenNavigation"/> via <see cref="TwNamespaceNavigation"/> and looks up via
        /// <see cref="QueryPageRevisionByNavigation"/>. Cached under <see cref="MemCache.Category.Page"/>, same
        /// cache key shape (canonical navigation + revision) as the SQLite reference; if
        /// <paramref name="refreshCache"/> is true, the cache entry is evicted first via <see cref="MemCache.Remove"/>,
        /// same as the reference.
        /// </summary>
        public async Task<TwPage?> GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false)
        {
            var navigation = new TwNamespaceNavigation(givenNavigation);

            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [navigation.Canonical, revision]);

            if (refreshCache)
            {
                MemCache.Remove(cacheKey);
            }

            return await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                return await QueryPageRevisionByNavigation(context, navigation.Canonical, revision);
            });
        }

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
