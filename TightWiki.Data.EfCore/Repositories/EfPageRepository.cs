using DuoVia.FuzzyStrings;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using TightWiki.Library.Caching;
using TightWiki.Library.Security;
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
    /// Still a partial skeleton (Database-Providers-Plan.md phase 2b.1-2b.6) - 26 of 86 members still throw
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
    /// specific SQLite script it mirrors) landed in phase 2b.3. A further 10 bulk/paged-listing members
    /// (<see cref="GetMissingPagesPaged"/>, <see cref="GetAllPagesByInstructionPaged"/>,
    /// <see cref="GetAllNamespacePagesPaged"/>, <see cref="GetAllPagesPaged"/>, <see cref="GetAllDeletedPagesPaged"/>,
    /// <see cref="GetAllNamespacesPaged"/>, <see cref="GetAllNamespaces"/>, <see cref="GetAllPages"/>,
    /// <see cref="GetAllTemplatePages"/>, and <see cref="GetAllFeatureTemplates"/> - see
    /// <see cref="GetAllPagesPaged"/>'s remarks for the <c>TempPageIds</c>/<c>list.Contains(...)</c> temp-table
    /// replacement pattern introduced here and reused in later phases) landed in phase 2b.4. A further 15
    /// search/tags/tokens members - the most complex category in the interface -
    /// (<see cref="PageSearch"/>, <see cref="PageSearchPaged"/>, <see cref="GetSimilarPagesPaged"/>,
    /// <see cref="GetRelatedPagesPaged"/>, <see cref="GetBacklinkPagesPaged"/>, <see cref="GetDeletedPageIdsByTokens"/>,
    /// <see cref="GetPageIdsByTokens"/>, <see cref="GetSearchTokensByPageId"/>, <see cref="SavePageSearchTokens"/>,
    /// <see cref="ParsePageTokens"/>, <see cref="GetAssociatedTags"/>, <see cref="GetPageInfoByNamespaces"/>,
    /// <see cref="GetPageInfoByTags"/>, <see cref="GetPageInfoByTag"/>, and <see cref="UpdatePageTags"/> - see
    /// <see cref="GetFuzzyPageSearchTokens"/>'s remarks for the <c>TempSearchTerms</c>/fuzzy-fan-out substitution
    /// and <see cref="ComputeParsedPageTokens"/> for the DuoVia.FuzzyStrings Double Metaphone scoring, which - like
    /// the SQLite reference - runs entirely in C#, not SQL) landed in phase 2b.5. A further 5 CRUD/upsert members -
    /// one of the most complex categories in the interface, transactional page save/orchestration -
    /// (<see cref="UpsertPage"/>, <see cref="RefreshPageMetadata"/>, <see cref="UpdatePageProcessingInstructions"/>,
    /// <see cref="UpdateSinglePageReference"/>, and <see cref="UpdatePageReferences"/> - see <see cref="SavePage"/>'s
    /// remarks for the hash-based change detection/revision-bumping logic behind <see cref="UpsertPage"/>, and
    /// <see cref="UpdatePageReferences"/>'s remarks for the <c>TempReferences</c> replacement and a confirmed,
    /// deliberately-not-reproduced bug in the SQLite reference script) landed in phase 2b.6. Real LINQ-based
    /// implementations of the rest (including the <c>TempTags</c>/<c>TempNamespaces</c>/<c>TempInstructions</c>
    /// replacements discussed in chapter 4.4 that this phase's own tag/namespace/reference/instruction methods
    /// didn't already cover) land across phases 2b.7-2b.13.
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

        /// <summary>
        /// Mirrors GetFuzzyPageSearchTokens.sql: candidate Pages.PageToken rows whose
        /// <see cref="PagesEntities.PageToken.DoubleMetaphone"/> matches one of <paramref name="tokens"/>'s
        /// (deduplicated - <see cref="TwPageToken.Equals"/> compares <see cref="TwPageToken.Token"/>
        /// case-insensitively) Double Metaphone codes, fetched via <c>Contains(...)</c> (the same
        /// <c>CreateTempTableFrom</c>/temp-table replacement pattern as <see cref="GetAllPagesPaged"/>'s remarks -
        /// here against <see cref="PagesEntities.PageToken.DoubleMetaphone"/> rather than an id list), then joined
        /// against <paramref name="tokens"/> and aggregated client-side rather than in the database.
        /// </summary>
        /// <remarks>
        /// The reference script's own join predicate is <c>ST.Token != T.Token AND ST.DoubleMetaphone =
        /// T.DoubleMetaphone</c> - a single Pages.PageToken row can join to <i>multiple</i> distinct search terms
        /// that share its phonetic code but differ in literal spelling, and each such pairing contributes its own
        /// row to the SQL aggregate (so <c>SUM(T.Weight)</c> can add the same token's weight more than once, while
        /// <c>COUNT(DISTINCT T.DoubleMetaphone)</c> still counts each phonetic code only once for the match
        /// ratio). This fan-out is a genuine multi-way join against a client-side list (not just a membership
        /// test), which has no safe, guaranteed-translatable EF Core/SQL Server LINQ equivalent - unlike the
        /// simple <c>list.Contains(...)</c> substitution used everywhere else (Database-Providers-Plan.md chapter
        /// 4.4/8). So this method fetches only the DoubleMetaphone-filtered candidate rows from the database (a
        /// translatable, bounded <c>Contains</c> query) and reproduces the fan-out join and aggregation itself in
        /// memory, replicating the SQL script's arithmetic (including the weight double-counting) exactly.
        /// </remarks>
        private async Task<List<TwPageSearchToken>> GetFuzzyPageSearchTokens(List<TwPageToken> tokens, double minimumMatchScore)
        {
            var searchTerms = tokens.Distinct().ToList();
            var searchTermDoubleMetaphones = searchTerms.Select(t => t.DoubleMetaphone).Distinct().ToList();
            var tokenCount = tokens.Count;

            using var context = _createContext();

            var candidates = await context.Pages_PageTokens
                .Where(t => searchTermDoubleMetaphones.Contains(t.DoubleMetaphone))
                .Select(t => new { t.PageId, t.Token, t.DoubleMetaphone, t.Weight })
                .ToListAsync();

            var joinedRows =
                from pt in candidates
                from st in searchTerms
                where !string.Equals(st.Token, pt.Token, StringComparison.OrdinalIgnoreCase)
                      && string.Equals(st.DoubleMetaphone, pt.DoubleMetaphone, StringComparison.OrdinalIgnoreCase)
                select pt;

            return joinedRows
                .GroupBy(pt => pt.PageId)
                .Select(g => new TwPageSearchToken
                {
                    PageId = g.Key,
                    Match = g.Select(pt => pt.DoubleMetaphone).Distinct(StringComparer.OrdinalIgnoreCase).Count() / (tokenCount + 0.0),
                    Weight = g.Sum(pt => pt.Weight) * 1.0,
                    //No weight benefit on score for fuzzy matching, matching GetFuzzyPageSearchTokens.sql.
                    Score = g.Select(pt => pt.DoubleMetaphone).Distinct(StringComparer.OrdinalIgnoreCase).Count() / (tokenCount + 0.0),
                })
                .Where(t => t.Score >= minimumMatchScore)
                .OrderByDescending(t => t.Score)
                .Take(250)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetExactPageSearchTokens.sql: Pages.PageToken rows whose <see cref="PagesEntities.PageToken.Token"/>
        /// exactly matches one of <paramref name="tokens"/>'s (deduplicated) token strings, fetched via
        /// <c>Contains(...)</c> (the <c>TempSearchTerms</c> replacement, same pattern as
        /// <see cref="GetFuzzyPageSearchTokens"/>), grouped by page and scored server-side - unlike the fuzzy
        /// variant, an exact match join has no fan-out (Pages.PageToken's composite primary key is
        /// (PageId, Token), so at most one row per page can match a given token string), so the aggregation
        /// translates safely to a single grouped SQL query.
        /// </summary>
        private async Task<List<TwPageSearchToken>> GetExactPageSearchTokens(List<TwPageToken> tokens, double minimumMatchScore)
        {
            var searchTermTokens = tokens.Distinct().Select(t => t.Token).ToList();
            var tokenCount = tokens.Count;

            using var context = _createContext();

            return await context.Pages_PageTokens
                .Where(t => searchTermTokens.Contains(t.Token))
                .GroupBy(t => t.PageId)
                .Select(g => new TwPageSearchToken
                {
                    PageId = g.Key,
                    Match = g.Count() / (tokenCount + 0.0),
                    Weight = g.Sum(x => x.Weight) * 1.5,
                    //Extra weight on score for exact matches, matching GetExactPageSearchTokens.sql.
                    Score = (g.Sum(x => x.Weight) * 1.5) * (g.Count() / (tokenCount + 0.0)),
                })
                .Where(t => t.Score >= minimumMatchScore)
                .OrderByDescending(t => t.Score)
                .Take(250)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.GetMeteredPageSearchTokens</c>: combines <see cref="GetExactPageSearchTokens"/>
        /// (always) with <see cref="GetFuzzyPageSearchTokens"/> (only when <paramref name="allowFuzzyMatching"/>),
        /// each run against half the "Minimum Match Score" search setting, then - when both ran - merges the two
        /// result sets by page (taking the max Match/Weight/Score per page, same as the reference's
        /// <c>GroupBy(...).Select(...Max...)</c>) and keeps only pages whose combined score still clears the full
        /// threshold. Cached under <see cref="MemCache.Category.Search"/>, same cache key shape (joined search
        /// terms + fuzzy flag) as the SQLite reference.
        /// </summary>
        private async Task<List<TwPageSearchToken>> GetMeteredPageSearchTokens(List<string> searchTerms, bool allowFuzzyMatching)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Search, [string.Join(',', searchTerms), allowFuzzyMatching]);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                var minimumMatchScore = await _configurationRepository.Get<float>(TwConfigGroup.Search, "Minimum Match Score");

                var searchTokens = searchTerms.Select(o => new TwPageToken
                {
                    Token = o,
                    DoubleMetaphone = o.ToDoubleMetaphone(),
                }).ToList();

                if (allowFuzzyMatching)
                {
                    var allTokens = await GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                    var fuzzyTokens = await GetFuzzyPageSearchTokens(searchTokens, minimumMatchScore / 2.0);

                    allTokens.AddRange(fuzzyTokens);

                    return allTokens
                        .GroupBy(token => token.PageId)
                        .Where(group => group.Sum(g => g.Score) >= minimumMatchScore)
                        .Select(group => new TwPageSearchToken
                        {
                            PageId = group.Key,
                            Match = group.Max(g => g.Match),
                            Weight = group.Max(g => g.Weight),
                            Score = group.Max(g => g.Score),
                        }).ToList();
                }
                else
                {
                    return await GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                }
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors PageSearch.sql: every page matched by <see cref="GetMeteredPageSearchTokens"/> (the
        /// <c>TempSearchTerms</c> join replaced by fetching the matched pages via <c>Contains(...)</c> against
        /// their page IDs, then attaching each page's Match/Weight/Score client-side - the same substitution
        /// pattern as <see cref="GetAllPagesPaged"/>'s remarks), ordered by Score descending then Name then Id
        /// ascending, same as the reference. Returns an empty list immediately when <paramref name="searchTerms"/>
        /// is empty or no page clears the minimum match score, same short-circuits as the SQLite reference.
        /// </summary>
        public async Task<List<TwPage>> PageSearch(List<string> searchTerms)
        {
            if (searchTerms.Count == 0)
            {
                return new List<TwPage>();
            }

            bool allowFuzzyMatching = await _configurationRepository.Get<bool>(TwConfigGroup.Search, "Allow Fuzzy Matching");
            var meteredSearchTokens = await GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<TwPage>();
            }

            var scoreByPageId = meteredSearchTokens.ToDictionary(t => t.PageId);
            var pageIds = scoreByPageId.Keys.ToList();

            using var context = _createContext();

            var pages = await context.Pages_Pages
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                })
                .ToListAsync();

            foreach (var page in pages)
            {
                var token = scoreByPageId[page.Id];
                page.Match = (decimal)token.Match;
                page.Weight = (decimal)token.Weight;
                page.Score = (decimal)token.Score;
            }

            return pages
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ToList();
        }

        /// <summary>
        /// Mirrors PageSearchPaged.sql: the same match as <see cref="PageSearch"/>, but with Score rescaled to a
        /// percentage of the maximum score among matched pages (the reference's <c>(ST.Score / @MaximumScore) *
        /// 100.0</c>), paginated by <paramref name="pageSize"/> (defaulting to the "Pagination Size" customization
        /// setting) with <see cref="TwPage.PaginationPageCount"/> computed via the reference's own
        /// ceiling-division formula against the total (unpaginated) matched-page count. <paramref name="allowFuzzyMatching"/>
        /// defaults to the "Allow Fuzzy Matching" search setting, same as the reference.
        /// </summary>
        public async Task<List<TwPage>> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
        {
            if (searchTerms.Count == 0)
            {
                return new List<TwPage>();
            }

            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");
            allowFuzzyMatching ??= await _configurationRepository.Get<bool>(TwConfigGroup.Search, "Allow Fuzzy Matching");

            var meteredSearchTokens = await GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching == true);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<TwPage>();
            }

            var maximumScore = meteredSearchTokens.Max(t => t.Score);
            var scoreByPageId = meteredSearchTokens.ToDictionary(t => t.PageId);
            var pageIds = scoreByPageId.Keys.ToList();

            using var context = _createContext();

            var pages = await context.Pages_Pages
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                })
                .ToListAsync();

            var paginationPageCount = (pages.Count + (pageSize.Value - 1)) / pageSize.Value;

            foreach (var page in pages)
            {
                var token = scoreByPageId[page.Id];
                page.Match = (decimal)token.Match;
                page.Weight = (decimal)token.Weight;
                page.Score = (decimal)((token.Score / maximumScore) * 100.0);
                page.PaginationPageCount = paginationPageCount;
            }

            return pages
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetSimilarPagesPaged.sql: pages sharing at least <paramref name="similarity"/> percent of
        /// <paramref name="pageId"/>'s own tags (percentage = shared tag count / <paramref name="pageId"/>'s
        /// total tag count * 100), including <paramref name="pageId"/> itself (always 100% similar to its own
        /// tags) - a literal quirk of the reference script (no <c>P.Id &lt;&gt; @PageId</c> filter anywhere in
        /// it), preserved rather than "fixed" here. The reference's self-join against <c>PageTag</c> (via a
        /// <c>TempTags</c>-less <c>LEFT OUTER JOIN</c>) is replaced by a two-step query (candidate page IDs via
        /// <c>Contains(...)</c>, matching the shared <c>list.Contains(...)</c> temp-table substitution pattern)
        /// rather than a single join, since EF Core has no portable equivalent of the reference's own
        /// self-referencing join combined with a <c>HAVING</c> percentage threshold against a correlated-subquery
        /// denominator. No explicit <c>ORDER BY</c> exists in the reference script itself (rows come back in
        /// whatever order SQLite happens to produce for the <c>IN (...)</c> filter, typically primary-key/rowid
        /// order) - ordered here by <see cref="PagesEntities.Page.Id"/> ascending as the closest deterministic
        /// equivalent. Paginated by <paramref name="pageSize"/> (defaulting to the "Pagination Size" customization
        /// setting); <see cref="TwRelatedPage.PaginationPageCount"/> is computed via the reference's own
        /// ceiling-division formula against the total (unpaginated) matched-page count.
        /// </summary>
        public async Task<List<TwRelatedPage>> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var rootTags = await context.Pages_PageTags
                .Where(t => t.PageId == pageId)
                .Select(t => t.Tag)
                .ToListAsync();

            List<int> matchingPageIds;
            if (rootTags.Count == 0)
            {
                matchingPageIds = new List<int>();
            }
            else
            {
                var totalRootTagCount = rootTags.Count;

                matchingPageIds = await context.Pages_PageTags
                    .Where(t => rootTags.Contains(t.Tag))
                    .GroupBy(t => t.PageId)
                    .Where(g => (g.Count() / (double)totalRootTagCount) * 100.0 >= similarity)
                    .Select(g => g.Key)
                    .ToListAsync();
            }

            var query = context.Pages_Pages.Where(p => matchingPageIds.Contains(p.Id));

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            return await query
                .OrderBy(p => p.Id)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(p => new TwRelatedPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetRelatedPagesPaged.sql: every page that references (links to) <paramref name="pageId"/> -
        /// despite the method's name, this is the same "who links here" relationship as
        /// <see cref="GetBacklinkPagesPaged"/>'s own first branch, just without the outlink/second-order-link
        /// branches - excluding self-references, ordered by <see cref="PagesEntities.Page.Name"/> ascending.
        /// Paginated by <paramref name="pageSize"/> (defaulting to the "Pagination Size" customization setting);
        /// <see cref="TwRelatedPage.PaginationPageCount"/> is computed via the reference's own ceiling-division
        /// formula against the total (unpaginated) matched-page count.
        /// </summary>
        public async Task<List<TwRelatedPage>> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = from pr in context.PageReferences
                        join p in context.Pages_Pages on pr.PageId equals p.Id
                        where pr.ReferencesPageId == pageId && pr.PageId != pr.ReferencesPageId
                        select p;

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            return await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(p => new TwRelatedPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetBacklinkPagesPaged.sql: the union of three page sets related to <paramref name="pageId"/>
        /// via Pages.PageReference - pages that reference it (backlinks), pages it references (outlinks), and
        /// pages that reference the same targets it references (second-order links) - each excluding
        /// <paramref name="pageId"/> itself, deduplicated (the reference script's <c>UNION</c>, not <c>UNION
        /// ALL</c>), ordered by <see cref="PagesEntities.Page.Name"/> ascending. The reference script's single
        /// three-way <c>UNION</c> query (plus a <c>COUNT(*) OVER()</c> window function for pagination) has no safe
        /// single-query EF Core/SQL Server LINQ translation, so each branch is resolved to a page-ID list via its
        /// own translatable query (the same <c>Contains(...)</c> temp-table substitution pattern as
        /// <see cref="GetAllPagesPaged"/>'s remarks) and the three lists are combined and deduplicated client-side
        /// before the final paginated page query. Paginated by <paramref name="pageSize"/> (defaulting to the
        /// "Pagination Size" customization setting); <see cref="TwRelatedPage.PaginationPageCount"/> is computed
        /// via the reference's own ceiling-division formula (functionally equivalent to its window-function
        /// count) against the total (unpaginated) combined-page count.
        /// </summary>
        public async Task<List<TwRelatedPage>> GetBacklinkPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            //Backlinks: pages that reference pageId.
            var backlinkIds = await context.PageReferences
                .Where(pr => pr.ReferencesPageId == pageId && pr.PageId != pageId)
                .Select(pr => pr.PageId)
                .ToListAsync();

            //Outlinks: pages referenced by pageId.
            var outlinkIds = await context.PageReferences
                .Where(pr => pr.PageId == pageId && pr.ReferencesPageId != null && pr.ReferencesPageId != pageId)
                .Select(pr => pr.ReferencesPageId!.Value)
                .ToListAsync();

            //Second order links: pages that reference the same targets pageId references.
            var outgoingTargetIds = await context.PageReferences
                .Where(pr => pr.PageId == pageId && pr.ReferencesPageId != null)
                .Select(pr => pr.ReferencesPageId!.Value)
                .Distinct()
                .ToListAsync();

            List<int> secondOrderIds;
            if (outgoingTargetIds.Count == 0)
            {
                secondOrderIds = new List<int>();
            }
            else
            {
                secondOrderIds = await context.PageReferences
                    .Where(pr => pr.ReferencesPageId != null
                        && outgoingTargetIds.Contains(pr.ReferencesPageId!.Value)
                        && pr.PageId != pageId)
                    .Select(pr => pr.PageId)
                    .ToListAsync();
            }

            var combinedIds = backlinkIds.Concat(outlinkIds).Concat(secondOrderIds).Distinct().ToList();

            var query = context.Pages_Pages.Where(p => combinedIds.Contains(p.Id));

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            return await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(p => new TwRelatedPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

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

        /// <summary>
        /// Mirrors GetMissingPagesPaged.sql: every Pages.PageReference row whose target does not resolve to an
        /// existing page (<see cref="PagesEntities.PageReference.ReferencesPageId"/> is null), read via the
        /// existing, required <see cref="PagesEntities.PageReference.Page"/> navigation for the source page's
        /// fields rather than a manual join. Paginated by the "Pagination Size" customization setting, same as the
        /// reference, and <see cref="TwNonexistentPage.PaginationPageCount"/> is computed via the reference's own
        /// ceiling-division formula against the total (unpaginated) count of broken references. Ordering mirrors
        /// <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c> mapping ("SourcePage"/
        /// "TargetPage"): no <paramref name="orderBy"/> falls back to the script's own un-transposed "ORDER BY
        /// P.[Name]" (always ascending, ignoring <paramref name="orderByDirection"/> - a literal quirk of the
        /// reference script, which hardcodes no direction on its own default ORDER BY); an unrecognized
        /// <paramref name="orderBy"/> throws, same pattern as <c>RepositoryHelpers.TransposeOrderby</c>'s "No
        /// order by mapping..." exception (see <see cref="GetPageRevisionsInfoByNavigationPaged"/> for the
        /// existing convention this follows).
        /// </summary>
        public async Task<List<TwNonexistentPage>> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = context.PageReferences.Where(pr => pr.ReferencesPageId == null);

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(pr => pr.Page.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "SOURCEPAGE" => ascending ? query.OrderBy(pr => pr.Page.Name) : query.OrderByDescending(pr => pr.Page.Name),
                    "TARGETPAGE" => ascending ? query.OrderBy(pr => pr.ReferencesPageName) : query.OrderByDescending(pr => pr.ReferencesPageName),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetMissingPagesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(pr => new TwNonexistentPage
                {
                    SourcePageId = pr.PageId,
                    SourcePageName = pr.Page.Name,
                    SourcePageNavigation = pr.Page.Navigation,
                    TargetPageName = pr.ReferencesPageName,
                    TargetPageNavigation = pr.ReferencesPageNavigation,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors UpdateSinglePageReference.sql: resolves every existing Pages.PageReference row (across every
        /// page, not just <paramref name="pageId"/>'s own outgoing references) whose
        /// <see cref="PagesEntities.PageReference.ReferencesPageNavigation"/> matches <paramref name="pageNavigation"/>,
        /// setting its <see cref="PagesEntities.PageReference.ReferencesPageId"/> to <paramref name="pageId"/> -
        /// the "fix up orphaned references once the page they point to actually gets created" step
        /// <see cref="UpsertPage"/> runs for newly-created pages. No transaction, matching the SQLite reference (a
        /// bare single-statement <c>UPDATE</c>). Flushes this page's cache via <see cref="FlushPageCache"/>
        /// afterward, same as the SQLite reference.
        /// </summary>
        public async Task UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            using var context = _createContext();

            await context.PageReferences
                .Where(pr => pr.ReferencesPageNavigation == pageNavigation)
                .ExecuteUpdateAsync(setters => setters.SetProperty(pr => pr.ReferencesPageId, pageId));

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors UpdatePageReferences.sql: replaces all Pages.PageReference rows for <paramref name="pageId"/>
        /// (its own outgoing references) with <paramref name="referencesPageNavigations"/> (deduplicated -
        /// <see cref="TwPageReference.Equals"/> compares <see cref="TwPageReference.Navigation"/>
        /// case-insensitively, same as the reference's own <c>referencesPageNavigations.Distinct()</c> before
        /// building <c>TempReferences</c>), wrapped in a single transaction (the reference's own <c>BEGIN
        /// TRANSACTION</c>/<c>COMMIT TRANSACTION</c>) - delete-then-insert, not a diff/merge. Each inserted row's
        /// <see cref="PagesEntities.PageReference.ReferencesPageId"/> is resolved via a <c>Contains(...)</c>
        /// lookup against <see cref="PagesEntities.Page.Navigation"/> (the <c>TempReferences</c>
        /// left-outer-join replacement, same pattern as <see cref="GetAllPagesPaged"/>'s remarks), materialized
        /// into a case-insensitive dictionary since <see cref="PagesEntities.Page.Navigation"/> carries a
        /// case-insensitive collation (<see cref="TightWiki.Data.EfCore.Configurations.Pages.PageReferenceConfiguration"/>) - left null when no page with
        /// that navigation currently exists (an orphaned/broken reference, later resolved by
        /// <see cref="UpdateSinglePageReference"/> if a page with that navigation is subsequently created).
        /// <see cref="PagesEntities.PageReference.ReferencesPageName"/> is built as the reference script's own
        /// <c>Coalesce(Ref.[Namespace] || ' :: ', '') || Ref.Name</c> - since <see cref="TwPageReference.Namespace"/>
        /// is a non-nullable property that is always either empty or a real namespace (never literally null in
        /// any reachable code path), this <c>Coalesce</c>'s null-branch is unreachable, so the literal,
        /// faithfully-reproduced result always includes the <c>" :: "</c> separator, even for un-namespaced
        /// references (a pre-existing, user-visible quirk of the reference script - e.g. a reference to a page
        /// named "SandBox" with no namespace stores <c>" :: SandBox"</c>, not <c>"SandBox"</c> - preserved here
        /// rather than "fixed", per this class's established convention of not correcting merely-odd-looking but
        /// faithfully-reproducible reference behavior). An empty (post-dedup) <paramref name="referencesPageNavigations"/>
        /// list still deletes the page's existing references (matching the reference's unconditional <c>DELETE
        /// FROM PageReference WHERE PageId = @PageId</c>) and simply inserts nothing. Flushes this page's cache
        /// via <see cref="FlushPageCache"/> afterward, same as the SQLite reference.
        /// </summary>
        /// <remarks>
        /// <b>⚠ Confirmed bug in the SQLite reference, deliberately not reproduced here.</b> After the delete/insert
        /// above, the reference script runs a second statement - <c>UPDATE PageReference SET
        /// ReferencesPageNavigation = I.Navigation FROM (SELECT DISTINCT Id, P.Navigation FROM PageReference as PR
        /// INNER JOIN [Page] as P ON P.Id = PR.ReferencesPageId WHERE P.Id = 77) AS I WHERE I.Id =
        /// PageReference.ReferencesPageId</c> - a hardcoded, unparameterized literal <c>77</c> in place of what
        /// every surrounding line of the same script (and the join it drives) makes clear should have been
        /// <c>@PageId</c> (confirmed unchanged since the very first SQLite port of this script via <c>git log
        /// --follow</c>, and unique to this one script - no other script in <c>TightWiki.Repository/Scripts/</c>
        /// filters on a bare numeric literal this way, the same class of pre-existing bug documented on
        /// <see cref="EfEmojiRepository.UpsertEmoji"/>'s own remarks). As written, this second statement is inert
        /// for every real page save except the one-in-a-database-lifetime coincidence that a page with Id exactly
        /// 77 is being referenced. The "evidently intended" fix reads as syncing
        /// <see cref="PagesEntities.PageReference.ReferencesPageNavigation"/> on every <i>other</i> page's
        /// reference <i>to</i> <paramref name="pageId"/> (e.g. after a rename) - but since this phase's task did
        /// not ask for that behavior and it would be new, untested functionality beyond anything the reference
        /// actually does for any real page today, it is not speculatively implemented here. This method
        /// reproduces only the unambiguous delete/insert half of the script, which is a complete, faithful mirror
        /// of the reference's real (non-dead-code) behavior.
        /// </remarks>
        public async Task UpdatePageReferences(int pageId, List<TwPageReference> referencesPageNavigations)
        {
            var distinctReferences = referencesPageNavigations.Distinct().ToList();

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.PageReferences
                .Where(pr => pr.PageId == pageId)
                .ExecuteDeleteAsync();

            if (distinctReferences.Count > 0)
            {
                var referencedNavigations = distinctReferences.Select(r => r.Navigation).ToList();

                var resolvedPages = await context.Pages_Pages
                    .Where(p => referencedNavigations.Contains(p.Navigation))
                    .Select(p => new { p.Navigation, p.Id })
                    .ToListAsync();

                var resolvedPageIdByNavigation = resolvedPages
                    .ToDictionary(p => p.Navigation, p => p.Id, StringComparer.OrdinalIgnoreCase);

                context.PageReferences.AddRange(distinctReferences.Select(r => new PagesEntities.PageReference
                {
                    PageId = pageId,
                    ReferencesPageName = r.Namespace + " :: " + r.Name,
                    ReferencesPageNavigation = r.Navigation,
                    ReferencesPageId = resolvedPageIdByNavigation.TryGetValue(r.Navigation, out var resolvedPageId)
                        ? resolvedPageId
                        : null,
                }));

                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors GetAllPagesByInstructionPaged.sql: every Pages.Page row that has the given
        /// <paramref name="instruction"/> recorded in Pages.PageProcessingInstruction, read via the existing,
        /// required <see cref="PagesEntities.PageProcessingInstruction.Page"/> navigation rather than a manual
        /// join, with <see cref="TwPage.CreatedByUserName"/>/<see cref="TwPage.ModifiedByUserName"/> resolved via
        /// the existing <see cref="PagesEntities.Page.CreatedByUser"/>/<see cref="PagesEntities.Page.ModifiedByUser"/>
        /// navigations rather than the script's own cross-database <c>o.Attach("users.db", "users_db")</c> (both
        /// schemas already live in the same <see cref="TightWikiDbContext"/>). When <paramref name="instruction"/>
        /// is null, no rows match - EF Core rewrites the equality against a null parameter into a null-safe
        /// comparison, and <see cref="PagesEntities.PageProcessingInstruction.Instruction"/> is a <c>NOT NULL</c>
        /// column, same net effect as the reference script's literal <c>WHERE PPI.Instruction = @Instruction</c>
        /// with a null parameter. Ordered by <see cref="PagesEntities.Page.Name"/> then
        /// <see cref="PagesEntities.Page.Id"/> ascending (this method takes no <c>orderBy</c> parameter - the
        /// reference script has no <c>--CUSTOM_ORDER_BEGIN::</c> section), paginated by the "Pagination Size"
        /// customization setting. <see cref="TwPage.PaginationPageCount"/> is computed via the reference's own
        /// ceiling-division formula against the total (unpaginated) count of matching pages.
        /// </summary>
        public async Task<List<TwPage>> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = context.Pages_PageProcessingInstructions
                .Where(pi => pi.Instruction == instruction)
                .Select(pi => pi.Page);

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            return await query
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetDeletedPageIdsByTokens.sql: the IDs of every soft-deleted page whose DeletedPages.PageToken
        /// rows cover every distinct non-empty token in <paramref name="tokens"/> - an AND-style "does this page
        /// contain all of these tokens" match (unlike <see cref="PageSearch"/>'s score-based ranking). The
        /// reference script's own arithmetic (<c>HAVING Count(0) = @TokenCount</c>, where <c>@TokenCount</c> is
        /// <paramref name="tokens"/>'s raw, non-deduplicated length) is algebraically equivalent to "every element
        /// of <paramref name="tokens"/> - counting duplicates - is a non-empty token the page actually has": if
        /// <paramref name="tokens"/> contains any null/empty entry, no page can ever satisfy the count (the
        /// script's own <c>WHERE Coalesce(TT.[value], '') &lt;&gt; ''</c> guarantees an empty entry can never join
        /// to a matching Pages.PageToken row, so it can never contribute to the required total), so that case is
        /// short-circuited to an empty result here rather than issuing a query that could never match anything.
        /// Otherwise, this reduces to "the page has every distinct token" - resolved via <c>Contains(...)</c> (the
        /// <c>TempTokens</c> replacement, same pattern as <see cref="GetAllPagesPaged"/>'s remarks) plus a grouped
        /// count check that relies on DeletedPages.PageToken's composite primary key (PageId, Token) to guarantee
        /// at most one matching row per page per distinct token.
        /// </summary>
        public async Task<List<int>> GetDeletedPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            if (tokens.Any(string.IsNullOrEmpty))
            {
                return new List<int>();
            }

            var distinctTokens = tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            using var context = _createContext();

            return await context.DeletedPages_PageTokens
                .Where(t => distinctTokens.Contains(t.Token))
                .GroupBy(t => t.PageId)
                .Where(g => g.Count() == distinctTokens.Count)
                .Select(g => g.Key)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageIdsByTokens.sql: the same "page contains every distinct non-empty token" match as
        /// <see cref="GetDeletedPageIdsByTokens"/>, against active Pages.PageToken rows instead of soft-deleted
        /// DeletedPages.PageToken rows - see that method's remarks for the full explanation of the
        /// <c>HAVING Count(0) = @TokenCount</c> equivalence and the empty-token short-circuit.
        /// </summary>
        public async Task<List<int>> GetPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            if (tokens.Any(string.IsNullOrEmpty))
            {
                return new List<int>();
            }

            var distinctTokens = tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            using var context = _createContext();

            return await context.Pages_PageTokens
                .Where(t => distinctTokens.Contains(t.Token))
                .GroupBy(t => t.PageId)
                .Where(g => g.Count() == distinctTokens.Count)
                .Select(g => g.Key)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllNamespacePagesPaged.sql: every Pages.Page row whose <see cref="PagesEntities.Page.Namespace"/>
        /// matches <paramref name="namespaceName"/>, with <see cref="TwPage.CreatedByUserName"/>/
        /// <see cref="TwPage.ModifiedByUserName"/> resolved via the existing <see cref="PagesEntities.Page.CreatedByUser"/>/
        /// <see cref="PagesEntities.Page.ModifiedByUser"/> navigations rather than a raw cross-database
        /// <c>ATTACH</c>, same substitution as <see cref="GetAllPagesByInstructionPaged"/>. Paginated by the
        /// "Pagination Size" customization setting; <see cref="TwPage.PaginationPageCount"/> is computed via the
        /// reference's own ceiling-division formula against the total (unpaginated) count of pages in the
        /// namespace. Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the script's
        /// <c>--CONFIG::</c> mapping ("Name"/"Revision"/"ModifiedBy"/"ModifiedDate"): no <paramref name="orderBy"/>
        /// falls back to the script's own un-transposed "ORDER BY P.[Name]" (always ascending, same quirk as
        /// <see cref="GetMissingPagesPaged"/>); an unrecognized <paramref name="orderBy"/> throws, same pattern as
        /// <see cref="GetPageRevisionsInfoByNavigationPaged"/>.
        /// </summary>
        public async Task<List<TwPage>> GetAllNamespacePagesPaged(int pageNumber, string namespaceName, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = context.Pages_Pages.Where(p => p.Namespace == namespaceName);

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(p => p.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "NAME" => ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                    "REVISION" => ascending ? query.OrderBy(p => p.Revision) : query.OrderByDescending(p => p.Revision),
                    "MODIFIEDBY" => ascending
                        ? query.OrderBy(p => p.ModifiedByUser != null ? p.ModifiedByUser.AccountName : null)
                        : query.OrderByDescending(p => p.ModifiedByUser != null ? p.ModifiedByUser.AccountName : null),
                    "MODIFIEDDATE" => ascending ? query.OrderBy(p => p.ModifiedDate) : query.OrderByDescending(p => p.ModifiedDate),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllNamespacePagesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllPagesPaged.sql (and, when <paramref name="searchTerms"/> is non-empty,
        /// GetAllPagesByPageIdPaged.sql): every Pages.Page row, with <see cref="TwPage.CreatedByUserName"/>/
        /// <see cref="TwPage.ModifiedByUserName"/> resolved via the existing <see cref="PagesEntities.Page.CreatedByUser"/>/
        /// <see cref="PagesEntities.Page.ModifiedByUser"/> navigations rather than the scripts' own cross-database
        /// <c>o.Attach("users.db", "users_db")</c> (both schemas already live in the same
        /// <see cref="TightWikiDbContext"/>). <see cref="TwPage.DeletedRevisionCount"/> is computed the same way
        /// as both scripts' own correlated subquery against DeletedPageRevisions.PageRevision, resolved here via
        /// <see cref="TightWikiDbContext.DeletedPageRevisions_PageRevisions"/> rather than the scripts' own
        /// <c>o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db")</c> - same cross-schema-navigation
        /// substitution, just against a schema with no navigation property defined for this particular
        /// relationship.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>TempPageIds replacement (Database-Providers-Plan.md chapter 4.4/8):</b> the reference implementation
        /// resolves <paramref name="searchTerms"/> to a list of matching page IDs via
        /// <see cref="GetPageIdsByTokens"/> (itself backed by a <c>TempTokens</c> temp table - out of scope for
        /// this method, landing in phase 2b.5), then feeds that ID list into GetAllPagesByPageIdPaged.sql via a
        /// second temp table, <c>CreateTempTableFrom("TempPageIds", pageIds)</c>, so the SQL can do
        /// <c>WHERE P.Id IN (SELECT PID.Value FROM TempPageIds as PID)</c>. EF Core/SQL Server has no equivalent
        /// concept of an ad-hoc session-scoped temp table reachable from LINQ, so this is replaced with the
        /// simplest portable equivalent: keep <c>pageIds</c> as a plain in-memory <see cref="List{T}"/> of
        /// <see cref="int"/> and filter with <c>pageIds.Contains(p.Id)</c> directly in the LINQ query. EF Core
        /// translates this to a parameterized <c>WHERE p.Id IN (...)</c> (or an equivalent translation for large
        /// lists), which is functionally equivalent to the temp table's own <c>IN (SELECT ... FROM TempPageIds)</c>
        /// for a plain "is this ID in the set" filter - just without a physical table backing the set. This same
        /// <c>list.Contains(...)</c> pattern is the one to reuse for every other <c>CreateTempTableFrom</c> call in
        /// the reference (<c>TempTokens</c>, <c>TempTags</c>, <c>TempNamespaces</c>, <c>TempReferences</c>,
        /// <c>TempInstructions</c> - phases 2b.5/2b.6).
        /// </para>
        /// <para>
        /// Since <see cref="GetPageIdsByTokens"/> is still a <see cref="NotImplementedException"/> stub as of this
        /// phase, calling it here means the <paramref name="searchTerms"/>-filtered path of this method currently
        /// throws rather than returning filtered results - a known, documented limitation of phase 2b.4. The
        /// no-<paramref name="searchTerms"/> path (the common case, and the one exercised by every existing
        /// caller/test as of this phase) is fully functional. Once <see cref="GetPageIdsByTokens"/> is implemented
        /// (phase 2b.5), this method's <paramref name="searchTerms"/> path starts working with no further changes
        /// needed here.
        /// </para>
        /// <para>
        /// Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the scripts' shared <c>--CONFIG::</c>
        /// mapping ("DeletedRevisions"/"Name"/"Revision"/"ModifiedBy"/"ModifiedDate"): no <paramref name="orderBy"/>
        /// falls back to the scripts' own un-transposed "ORDER BY P.[Name]" (always ascending, same quirk as
        /// <see cref="GetMissingPagesPaged"/>); an unrecognized <paramref name="orderBy"/> throws, same pattern as
        /// <see cref="GetPageRevisionsInfoByNavigationPaged"/>. Paginated by the "Pagination Size" customization
        /// setting; <see cref="TwPage.PaginationPageCount"/> is computed via the scripts' own ceiling-division
        /// formula against the total (unpaginated, but already ID-filtered when <paramref name="searchTerms"/> is
        /// given) page count.
        /// </para>
        /// </remarks>
        public async Task<List<TwPage>> GetAllPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            List<int>? pageIds = null;
            if (searchTerms?.Count > 0)
            {
                pageIds = await GetPageIdsByTokens(searchTerms);
            }

            using var context = _createContext();

            IQueryable<PagesEntities.Page> query = context.Pages_Pages;

            if (pageIds != null)
            {
                query = query.Where(p => pageIds.Contains(p.Id));
            }

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(p => p.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "DELETEDREVISIONS" => ascending
                        ? query.OrderBy(p => context.DeletedPageRevisions_PageRevisions.Count(pr => pr.PageId == p.Id))
                        : query.OrderByDescending(p => context.DeletedPageRevisions_PageRevisions.Count(pr => pr.PageId == p.Id)),
                    "NAME" => ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                    "REVISION" => ascending ? query.OrderBy(p => p.Revision) : query.OrderByDescending(p => p.Revision),
                    "MODIFIEDBY" => ascending
                        ? query.OrderBy(p => p.ModifiedByUser != null ? p.ModifiedByUser.AccountName : null)
                        : query.OrderByDescending(p => p.ModifiedByUser != null ? p.ModifiedByUser.AccountName : null),
                    "MODIFIEDDATE" => ascending ? query.OrderBy(p => p.ModifiedDate) : query.OrderByDescending(p => p.ModifiedDate),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllPagesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    DeletedRevisionCount = context.DeletedPageRevisions_PageRevisions.Count(pr => pr.PageId == p.Id),
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllDeletedPagesPaged.sql (and, when <paramref name="searchTerms"/> is non-empty,
        /// GetAllDeletedPagesByPageIdPaged.sql): every DeletedPages.Page row, inner-joined to its
        /// DeletedPages.DeletionMeta row - a manual join (no navigation property exists between the two entities),
        /// matching the scripts' own <c>INNER JOIN DeletionMeta as DM ON DM.PageId = P.Id</c> - LEFT OUTER JOINed
        /// to Users.Profile three times (creator, modifier, deleter) via the existing
        /// <see cref="DeletedPagesEntities.Page.CreatedByUser"/>/<see cref="DeletedPagesEntities.Page.ModifiedByUser"/>/
        /// <see cref="DeletedPagesEntities.DeletionMeta.DeletedByUser"/> navigations rather than the scripts' own
        /// cross-database <c>ATTACH</c>. <paramref name="searchTerms"/> is resolved to a page-ID filter the same
        /// way, and with the same <c>TempPageIds</c>-replacement caveat (delegates to the still-unimplemented
        /// <see cref="GetDeletedPageIdsByTokens"/>), as documented on <see cref="GetAllPagesPaged"/> - see that
        /// method's remarks for the full explanation of the <c>list.Contains(...)</c> substitution pattern.
        /// Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the scripts' shared <c>--CONFIG::</c>
        /// mapping ("Page"): no <paramref name="orderBy"/> falls back to the scripts' own un-transposed "ORDER BY
        /// P.[Name]" (always ascending, same quirk as <see cref="GetMissingPagesPaged"/>); an unrecognized
        /// <paramref name="orderBy"/> throws, same pattern as <see cref="GetPageRevisionsInfoByNavigationPaged"/>.
        /// Paginated by the "Pagination Size" customization setting; <see cref="TwPage.PaginationPageCount"/> is
        /// computed via the scripts' own ceiling-division formula against the total (unpaginated, but already
        /// ID-filtered when <paramref name="searchTerms"/> is given) deleted-page count.
        /// <see cref="TwPage.DeletedByUserId"/> is deliberately left unset here - a literal quirk of both
        /// reference scripts, which select <c>DeletedUser.AccountName as DeletedByUserName</c> but never the raw
        /// <c>DM.DeletedByUserID</c> column itself.
        /// </summary>
        public async Task<List<TwPage>> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            List<int>? pageIds = null;
            if (searchTerms?.Count > 0)
            {
                pageIds = await GetDeletedPageIdsByTokens(searchTerms);
            }

            using var context = _createContext();

            var joined = from p in context.DeletedPages_Pages
                         join dm in context.DeletedPages_DeletionMetas on p.Id equals dm.PageId
                         select new { p, dm };

            if (pageIds != null)
            {
                joined = joined.Where(x => pageIds.Contains(x.p.Id));
            }

            var totalCount = await joined.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? joined.OrderBy(x => x.p.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "PAGE" => ascending ? joined.OrderBy(x => x.p.Name) : joined.OrderByDescending(x => x.p.Name),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllDeletedPagesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(x => new TwPage
                {
                    Id = x.p.Id,
                    Name = x.p.Name,
                    Navigation = x.p.Navigation,
                    Description = x.p.Description,
                    Revision = x.p.Revision,
                    CreatedByUserId = x.p.CreatedByUserId,
                    CreatedDate = x.p.CreatedDate,
                    ModifiedByUserId = x.p.ModifiedByUserId,
                    ModifiedDate = x.p.ModifiedDate,
                    CreatedByUserName = x.p.CreatedByUser != null ? (x.p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = x.p.ModifiedByUser != null ? (x.p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    DeletedByUserName = x.dm.DeletedByUser != null ? (x.dm.DeletedByUser.AccountName ?? string.Empty) : string.Empty,
                    DeletedDate = x.dm.DeletedDate ?? default,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllNamespacesPaged.sql: every distinct <see cref="PagesEntities.Page.Namespace"/> value
        /// grouped with a count of pages in that namespace. Paginated by the "Pagination Size" customization
        /// setting; <see cref="TwNamespaceStat.PaginationPageCount"/> is computed via the reference's own
        /// ceiling-division formula against the total count of distinct namespaces (not the total page count).
        /// Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c>
        /// mapping ("Name"/"Pages"): no <paramref name="orderBy"/> falls back to the script's own un-transposed
        /// "ORDER BY P.[Namespace]" (always ascending, same quirk as <see cref="GetMissingPagesPaged"/>); an
        /// unrecognized <paramref name="orderBy"/> throws, same pattern as
        /// <see cref="GetPageRevisionsInfoByNavigationPaged"/>.
        /// </summary>
        public async Task<List<TwNamespaceStat>> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var grouped = context.Pages_Pages
                .GroupBy(p => p.Namespace)
                .Select(g => new { Namespace = g.Key, CountOfPages = g.Count() });

            var distinctNamespaceCount = await context.Pages_Pages.Select(p => p.Namespace).Distinct().CountAsync();
            var paginationPageCount = (distinctNamespaceCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? grouped.OrderBy(g => g.Namespace)
                : orderBy.ToUpperInvariant() switch
                {
                    "NAME" => ascending ? grouped.OrderBy(g => g.Namespace) : grouped.OrderByDescending(g => g.Namespace),
                    "PAGES" => ascending ? grouped.OrderBy(g => g.CountOfPages) : grouped.OrderByDescending(g => g.CountOfPages),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllNamespacesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(g => new TwNamespaceStat
                {
                    Namespace = g.Namespace,
                    CountOfPages = g.CountOfPages,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllNamespaces.sql: every distinct <see cref="PagesEntities.Page.Namespace"/> value, no
        /// ordering (matching the reference's plain "SELECT DISTINCT [Namespace] FROM [Page]").
        /// </summary>
        public async Task<List<string>> GetAllNamespaces()
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Select(p => p.Namespace)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllPages.sql: every Pages.Page row inner-joined to the Pages.PageRevision row matching its
        /// own current <see cref="PagesEntities.Page.Revision"/> (same join shape as
        /// <see cref="GetTopRecentlyModifiedPagesInfo"/>/<see cref="GetLatestPageRevisionById"/>), including the
        /// revision <see cref="PagesEntities.PageRevision.Body"/>. No ordering, matching the reference.
        /// </summary>
        public async Task<List<TwPage>> GetAllPages()
        {
            using var context = _createContext();

            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
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
                           }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllTemplatePages.sql: the same join as <see cref="GetAllPages"/>, additionally filtered to
        /// pages whose <see cref="PagesEntities.Page.Namespace"/> is exactly "Templates" - a literal, hardcoded
        /// string in the reference script, preserved verbatim here.
        /// </summary>
        public async Task<List<TwPage>> GetAllTemplatePages()
        {
            using var context = _createContext();

            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
                           where p.Namespace == "Templates"
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
                           }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllFeatureTemplates.sql: every Pages.FeatureTemplate row, LEFT OUTER JOINed to its
        /// associated help Pages.Page via the existing <see cref="PagesEntities.FeatureTemplate.Page"/> navigation
        /// rather than a manual join, for <see cref="TwFeatureTemplate.HelpPageNavigation"/>. Cached under
        /// <see cref="MemCache.Category.Configuration"/> with no extra key segments, same as the SQLite reference.
        /// <see cref="TwFeatureTemplate.PageId"/> defaults to 0 when
        /// <see cref="PagesEntities.FeatureTemplate.PageId"/> is null (the reference script selects the nullable
        /// column directly into this non-nullable model property; this is the closest portable equivalent).
        /// </summary>
        public async Task<List<TwFeatureTemplate>> GetAllFeatureTemplates()
        {
            return (await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Configuration), async () =>
            {
                using var context = _createContext();

                return await context.FeatureTemplates
                    .Select(ft => new TwFeatureTemplate
                    {
                        Name = ft.Name,
                        Type = ft.Type,
                        PageId = ft.PageId ?? 0,
                        Description = ft.Description ?? string.Empty,
                        TemplateText = ft.TemplateText ?? string.Empty,
                        HelpPageNavigation = ft.Page != null ? ft.Page.Navigation : string.Empty,
                    }).ToListAsync();
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors UpdatePageProcessingInstructions.sql: replaces all Pages.PageProcessingInstruction rows for
        /// <paramref name="pageId"/> with <paramref name="instructions"/> (each lower-invarianted then
        /// deduplicated - the reference's own <c>instructions.Select(o => o.ToLowerInvariant()).Distinct()</c>
        /// before building <c>TempInstructions</c> - then dropping any entry that is null/empty, the reference
        /// script's own <c>WHERE Coalesce(TI.[value], '') &lt;&gt; ''</c> insert-time filter), wrapped in a
        /// single transaction (the reference's own <c>BEGIN TRANSACTION</c>/<c>COMMIT TRANSACTION</c>) -
        /// delete-then-insert, not a diff/merge, same pattern as <see cref="UpdatePageTags"/>. An empty
        /// (post-filtering) <paramref name="instructions"/> list still deletes the page's existing instructions
        /// (matching the reference's unconditional <c>DELETE FROM PageProcessingInstruction WHERE PageId =
        /// @PageId</c>) and simply inserts nothing. Unlike <see cref="UpdatePageTags"/>, flushes this page's
        /// cache via <see cref="FlushPageCache"/> afterward - matching the SQLite reference, whose
        /// <c>PageRepository.UpdatePageProcessingInstructions</c> (unlike <c>UpdatePageTags</c>'s) does call it
        /// here.
        /// </summary>
        public async Task UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            var distinctInstructions = instructions
                .Select(i => i.ToLowerInvariant())
                .Distinct()
                .Where(i => !string.IsNullOrEmpty(i))
                .ToList();

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.Pages_PageProcessingInstructions
                .Where(pi => pi.PageId == pageId)
                .ExecuteDeleteAsync();

            if (distinctInstructions.Count > 0)
            {
                context.Pages_PageProcessingInstructions.AddRange(distinctInstructions.Select(i => new PagesEntities.PageProcessingInstruction
                {
                    PageId = pageId,
                    Instruction = i,
                }));

                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            await FlushPageCache(pageId);
        }

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

        /// <summary>
        /// Mirrors GetSearchTokensByPageId.sql: every Pages.PageToken row for <paramref name="pageId"/>.
        /// </summary>
        public async Task<List<TwPageToken>> GetSearchTokensByPageId(int pageId)
        {
            using var context = _createContext();

            return await context.Pages_PageTokens
                .Where(t => t.PageId == pageId)
                .Select(t => new TwPageToken
                {
                    PageId = t.PageId,
                    Token = t.Token,
                    DoubleMetaphone = t.DoubleMetaphone,
                    Weight = t.Weight,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors SavePageSearchTokens.sql: replaces the Pages.PageToken rows for every page represented in
        /// <paramref name="items"/> (deduplicated - <see cref="TwPageToken.Equals"/> compares PageId and Token
        /// case-insensitively, same as the reference's <c>items.Distinct()</c> before building <c>TempTokens</c>)
        /// with exactly the given rows, wrapped in a single transaction (the reference script's own
        /// <c>BEGIN TRANSACTION</c>/<c>COMMIT TRANSACTION</c>) - delete-then-insert per affected page, not a
        /// diff/merge. An empty <paramref name="items"/> list is a no-op, matching the reference (an empty
        /// <c>TempTokens</c> deletes nothing and inserts nothing).
        /// </summary>
        public async Task SavePageSearchTokens(List<TwPageToken> items)
        {
            var distinctItems = items.Distinct().ToList();

            if (distinctItems.Count == 0)
            {
                return;
            }

            var pageIds = distinctItems.Select(i => i.PageId).Distinct().ToList();

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.Pages_PageTokens
                .Where(t => pageIds.Contains(t.PageId))
                .ExecuteDeleteAsync();

            context.Pages_PageTokens.AddRange(distinctItems.Select(i => new PagesEntities.PageToken
            {
                PageId = i.PageId,
                Token = i.Token,
                DoubleMetaphone = i.DoubleMetaphone,
                Weight = i.Weight,
            }));

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

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

        /// <summary>
        /// Mirrors GetAssociatedTags.sql: every distinct tag applied to any page that itself carries the tag
        /// matching <paramref name="tag"/>'s navigation (a "tags that co-occur with this tag" query, used for
        /// tag-cloud/related-tag browsing) - resolved in two steps (the pages carrying <paramref name="tag"/>,
        /// then every Pages.PageTag row belonging to those pages) rather than the reference script's own
        /// self-join, since the self-join's <c>Interm</c> alias is provably redundant (it only re-selects
        /// <paramref name="tag"/>'s own Pages.PageTag row, already implied by <c>Root</c>). <see cref="TwTagAssociation.Tag"/>
        /// is the alphabetically-last literal spelling among all pages sharing that tag's navigation (the
        /// reference's own <c>MAX(Extent.Tag)</c>); <see cref="TwTagAssociation.PageCount"/> is the count of
        /// distinct pages carrying it (the reference's own <c>COUNT(DISTINCT Extent.PageId)</c> - defensive
        /// against a page somehow carrying the same tag Navigation twice, which the table's own composite primary
        /// key (PageId, Tag) already prevents in practice for any single literal spelling, but not necessarily
        /// across two different-cased spellings that both clean to the same Navigation). No ordering, capped at
        /// 100 rows, matching the reference's own un-ordered <c>LIMIT 100</c>. Grouping/aggregation is done
        /// client-side over the (page-count-bounded) candidate rows rather than via a database
        /// <c>GROUP BY</c>/<c>COUNT(DISTINCT ...)</c>, to sidestep any provider-translation risk for a nested
        /// distinct-count inside a grouped projection.
        /// </summary>
        public async Task<List<TwTagAssociation>> GetAssociatedTags(string tag)
        {
            using var context = _createContext();

            var matchingPageIds = await context.Pages_PageTags
                .Where(t => t.Navigation == tag)
                .Select(t => t.PageId)
                .Distinct()
                .ToListAsync();

            if (matchingPageIds.Count == 0)
            {
                return new List<TwTagAssociation>();
            }

            var candidateTags = await context.Pages_PageTags
                .Where(t => matchingPageIds.Contains(t.PageId))
                .Select(t => new { t.PageId, t.Tag, t.Navigation })
                .ToListAsync();

            return candidateTags
                .GroupBy(t => t.Navigation)
                .Select(g => new TwTagAssociation
                {
                    Tag = g.Max(x => x.Tag)!,
                    PageCount = g.Select(x => x.PageId).Distinct().Count(),
                })
                .Take(100)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetPageInfoByNamespaces.sql: page metadata (excluding content) for every page whose
        /// <see cref="PagesEntities.Page.Namespace"/> is one of <paramref name="namespaces"/>, resolved via
        /// <c>Contains(...)</c> (the <c>TempNamespaces</c> replacement, same pattern as
        /// <see cref="GetAllPagesPaged"/>'s remarks). The reference script's own <c>SELECT DISTINCT</c> is not
        /// reproduced as an explicit <c>Distinct()</c> call - <see cref="PagesEntities.Page.Id"/> is already
        /// unique, so a <c>Contains(...)</c> filter (unlike the reference's own join against a temp table that
        /// could contain duplicate namespace values) can never itself produce duplicate page rows.
        /// </summary>
        public async Task<List<TwPage>> GetPageInfoByNamespaces(List<string> namespaces)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Where(p => namespaces.Contains(p.Namespace))
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageInfoByTags.sql: page metadata (excluding content) for every page carrying at least one
        /// tag whose <see cref="TwNavigation.Clean"/>-cleaned navigation matches one of <paramref name="tags"/>
        /// (each cleaned the same way, matching the reference's own <c>cleanedTags = tags.Select(TwNavigation.Clean)</c>).
        /// Resolved in two steps - matching page IDs via <c>Contains(...)</c> against
        /// <see cref="PagesEntities.PageTag.Navigation"/> (the <c>TempTags</c> replacement, same pattern as
        /// <see cref="GetAllPagesPaged"/>'s remarks), then the page rows themselves - rather than a single joined
        /// query, so the reference script's own <c>SELECT DISTINCT</c> (needed there because a page with multiple
        /// matching tags would otherwise join multiple times) has a natural equivalent: an intermediate
        /// <c>Distinct()</c> on page IDs.
        /// </summary>
        public async Task<List<TwPage>> GetPageInfoByTags(IEnumerable<string> tags)
        {
            var cleanedTags = tags.Select(t => TwNavigation.Clean(t)).ToList();

            using var context = _createContext();

            var pageIds = await context.Pages_PageTags
                .Where(t => cleanedTags.Contains(t.Navigation))
                .Select(t => t.PageId)
                .Distinct()
                .ToListAsync();

            return await context.Pages_Pages
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.GetPageInfoByTag</c>: delegates to <see cref="GetPageInfoByTags"/> with a
        /// single-element list - both the reference implementation and this one clean <paramref name="tag"/> the
        /// same way (<see cref="TwNavigation.Clean"/>) and run the exact same underlying query
        /// (GetPageInfoByTags.sql, despite the reference building its own single-entry <c>TempTags</c> table
        /// separately rather than calling its own <c>GetPageInfoByTags</c> method), so delegating here reduces
        /// duplication without changing behavior.
        /// </summary>
        public async Task<List<TwPage>> GetPageInfoByTag(string tag)
            => await GetPageInfoByTags([tag]);

        /// <summary>
        /// Mirrors UpdatePageTags.sql: replaces all Pages.PageTag rows for <paramref name="pageId"/> with
        /// <paramref name="tags"/> (deduplicated by cleaned <see cref="TwNavigation.Clean"/> navigation, keeping
        /// the first literal spelling per navigation - the reference's own <c>DistinctBy(o => o.Navigation)</c> -
        /// then dropping any entry whose literal tag text is null/empty, the reference script's own <c>WHERE
        /// Coalesce(T.[Tag], '') &lt;&gt; ''</c> insert-time filter), wrapped in a single transaction (the
        /// reference's own <c>BEGIN TRANSACTION</c>/<c>COMMIT TRANSACTION</c>) - delete-then-insert, not a
        /// diff/merge. An empty (post-filtering) <paramref name="tags"/> list still deletes the page's existing
        /// tags (matching the reference's unconditional <c>DELETE FROM PageTag WHERE PageId = @PageId</c>) and
        /// simply inserts nothing.
        /// </summary>
        public async Task UpdatePageTags(int pageId, List<string> tags)
        {
            var paramTags = tags
                .Select(t => new { Tag = t, Navigation = TwNavigation.Clean(t) })
                .DistinctBy(t => t.Navigation)
                .Where(t => !string.IsNullOrEmpty(t.Tag))
                .ToList();

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.Pages_PageTags
                .Where(t => t.PageId == pageId)
                .ExecuteDeleteAsync();

            if (paramTags.Count > 0)
            {
                context.Pages_PageTags.AddRange(paramTags.Select(t => new PagesEntities.PageTag
                {
                    PageId = pageId,
                    Tag = t.Tag,
                    Navigation = t.Navigation,
                }));

                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.UpsertPage</c>: saves <paramref name="page"/> (and its revision history) via
        /// <see cref="SavePage"/>, refreshes its tags/processing-instructions/search-tokens/outgoing-references
        /// via <see cref="RefreshPageMetadata"/>, and - only when <paramref name="page"/>.Id was 0 on entry (a
        /// brand-new page) - resolves any pre-existing orphaned Pages.PageReference rows that pointed at this
        /// page's navigation before it existed, via <see cref="UpdateSinglePageReference"/>. Same three-step
        /// orchestration, same order, as the SQLite reference.
        /// </summary>
        public async Task<int> UpsertPage(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = await SavePage(page);

            await RefreshPageMetadata(wikifier, localizer, page, sessionState);

            if (isNewlyCreated)
            {
                //This will update the PageId of references that have been saved to the navigation link.
                await UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        /// <summary>
        /// Mirrors <c>PageRepository.RefreshPageMetadata</c>: re-transforms <paramref name="page"/> through
        /// <paramref name="wikifier"/> (omitting <see cref="TwMatchType.StandardFunction"/> matches from
        /// tokenization, same as the reference - function calls are too dynamic for static searching), then
        /// rewrites this page's tags (<see cref="UpdatePageTags"/>), processing instructions
        /// (<see cref="UpdatePageProcessingInstructions"/>), search tokens (<see cref="ParsePageTokens"/> +
        /// <see cref="SavePageSearchTokens"/>), and outgoing references (<see cref="UpdatePageReferences"/>) from
        /// the resulting <see cref="ITwEngineState"/>, then clears this page's cache under both its id and its
        /// navigation - the same two <see cref="MemCache.ClearCategory(MemCacheKey)"/> calls as the SQLite
        /// reference (in addition to whatever caches the individual Update*/Save* calls above already flush on
        /// their own).
        /// </summary>
        public async Task RefreshPageMetadata(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null)
        {
            //We omit function calls from the tokenization process because they are too dynamic for static searching.
            var state = await wikifier.Transform(localizer, sessionState, page, null, [TwMatchType.StandardFunction]);

            await UpdatePageTags(page.Id, state.Tags);
            await UpdatePageProcessingInstructions(page.Id, state.ProcessingInstructions);

            var pageTokens = (await ParsePageTokens(state)).Select(o =>
                      new TwPageToken
                      {
                          PageId = page.Id,
                          Token = o.Token,
                          DoubleMetaphone = o.DoubleMetaphone,
                          Weight = o.Weight
                      }).ToList();

            await SavePageSearchTokens(pageTokens);
            await UpdatePageReferences(page.Id, state.OutgoingLinks);

            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [page.Id]));
            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [page.Navigation]));
        }

        /// <summary>
        /// Mirrors <c>PageRepository.ParsePageTokens</c>: tokenizes <paramref name="state"/>'s rendered HTML,
        /// page description, tags, and page name (each with its own weight multiplier - 1/1.2/1.4/1.6
        /// respectively, same as the reference), then aggregates by token text into one row per distinct token
        /// with its <see cref="TwAggregatedSearchToken.DoubleMetaphone"/> code and summed weight, via
        /// <see cref="ComputeParsedPageTokens"/>.
        /// </summary>
        /// <remarks>
        /// This method - and the private <see cref="ComputeParsedPageTokens"/> helper it calls - have no
        /// dependency on any of the still-<see cref="NotImplementedException"/> members of this class (in
        /// particular, unlike <c>PageRepository.RefreshPageMetadata</c> - phase 2b.6, still a stub here - this
        /// method never calls <see cref="UpsertPage"/>/<see cref="RefreshPageMetadata"/> itself, only the other
        /// way around), so it is fully functional as of this phase even though its only real caller
        /// (<see cref="RefreshPageMetadata"/>) is not yet implemented.
        /// </remarks>
        public async Task<List<TwAggregatedSearchToken>> ParsePageTokens(ITwEngineState state)
        {
            var parsedTokens = new List<WeightedSearchToken>();

            parsedTokens.AddRange(await ComputeParsedPageTokens(state.HtmlResult, 1));
            parsedTokens.AddRange(await ComputeParsedPageTokens(state.Page.Description, 1.2));
            parsedTokens.AddRange(await ComputeParsedPageTokens(string.Join(" ", state.Tags), 1.4));
            parsedTokens.AddRange(await ComputeParsedPageTokens(state.Page.Name, 1.6));

            return parsedTokens
                .GroupBy(o => o.Token)
                .Select(o => new TwAggregatedSearchToken
                {
                    Token = o.Key,
                    DoubleMetaphone = o.Key.ToDoubleMetaphone(),
                    Weight = o.Sum(g => g.Weight),
                }).ToList();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.ComputeParsedPageTokens</c>: strips HTML from <paramref name="content"/>
        /// (<c>NTDLS.Helpers.Html.StripHtml</c>), splits on whitespace/hyphen/underscore, optionally also splits
        /// camel-cased tokens into their component words (<c>NTDLS.Helpers.Text.SplitCamelCase</c>, gated by the
        /// "Split Camel Case" search setting - split words are added alongside the original tokens, not in place
        /// of them, same as the reference), lower-invariants everything, drops any token listed in the
        /// "Word Exclusions" search setting (comma/semicolon-separated), then groups by token text into a
        /// per-token count-based weight (occurrence count * <paramref name="weightMultiplier"/>), dropping
        /// whitespace-only tokens.
        /// </summary>
        private async Task<List<WeightedSearchToken>> ComputeParsedPageTokens(string content, double weightMultiplier)
        {
            var searchConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Search);

            var exclusionWords = searchConfig?.Value<string>("Word Exclusions")?
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Distinct() ?? new List<string>();
            var strippedContent = Html.StripHtml(content);

            var tokens = strippedContent.Split([' ', '\n', '\t', '-', '_']).ToList();

            if (searchConfig?.Value<bool>("Split Camel Case") == true)
            {
                var allSplitTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var splitTokens = Text.SplitCamelCase(token);
                    if (splitTokens.Count > 1)
                    {
                        splitTokens.ForEach(t => allSplitTokens.Add(t));
                    }
                }

                tokens.AddRange(allSplitTokens);
            }

            tokens = tokens.ConvertAll(d => d.ToLowerInvariant());

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                 group w by w into g
                                 select new WeightedSearchToken
                                 {
                                     Token = g.Key,
                                     Weight = g.Count() * weightMultiplier
                                 }).ToList();

            return searchTokens.Where(o => string.IsNullOrWhiteSpace(o.Token) == false).ToList();
        }

        /// <summary>
        /// Creates a new page or updates an existing page and its revision history in the data store.
        ///
        /// DO NOT USE DIRECTLY: Use <see cref="UpsertPage"/> instead.
        /// </summary>
        /// <remarks>
        /// Mirrors <c>PageRepository.SavePage</c>'s hash-based change detection and revision-bumping, one
        /// transaction (the reference's own <c>o.BeginTransaction()</c>/<c>Commit()</c>/<c>Rollback()</c>) per
        /// call:
        /// <list type="number">
        /// <item>A brand-new page (<paramref name="page"/>.Id == 0) is simply inserted (Revision hardcoded to 1,
        /// matching CreatePage.sql), and is therefore always treated as "changed".</item>
        /// <item>An existing page has its current, cached <see cref="TwPage.Revision"/>/<see cref="TwPage.DataHash"/>
        /// read via <see cref="GetLimitedPageInfoByIdAndRevision"/> (throwing if the page can no longer be found,
        /// same as the reference) <i>before</i> Pages.Page's mutable columns (Description/Name/Namespace/
        /// Navigation/ModifiedByUserId/ModifiedDate - matching UpdatePage.sql's own column list exactly, and
        /// deliberately excluding Revision, which is only ever bumped in step 3 below) are overwritten - "changed"
        /// is then whichever of Name/Namespace/Description/ChangeSummary/DataHash (a CRC32 of
        /// <paramref name="page"/>.Body, <see cref="TightWiki.Library.Security.SecurityUtility.Crc32(string)"/>,
        /// same algorithm as the reference) differs from what was just read, all compared the same
        /// ordinal/case-sensitive way in C# both here and in the reference (Dapper materializes the "current"
        /// row into a <see cref="TwPage"/> first on that side too, so there is no SQL-collation involved in this
        /// comparison on either side).</item>
        /// <item>Only when "changed": the page's <see cref="PagesEntities.Page.Revision"/> is bumped
        /// (UpdatePageRevisionNumber.sql), a new Pages.PageRevision snapshot row is inserted at that revision
        /// number (InsertPageRevision.sql - its own reference script's <c>FROM [Page] WHERE Id = @PageId</c>
        /// guard is not reproduced as a separate check, since by this point in the method the page is always
        /// known to exist, having either just been inserted or already been read back above), and every
        /// Pages.PageRevisionAttachment row still attached at the <i>previous</i> revision - only those whose
        /// <see cref="PagesEntities.PageRevisionAttachment.FileRevision"/> still matches the file's own current
        /// <see cref="PagesEntities.PageFile.Revision"/>, i.e. the file has not itself been separately replaced
        /// since - is carried forward to the new revision (ReassociateAllPageAttachments.sql, via the existing
        /// <see cref="PagesEntities.PageRevisionAttachment.PageFile"/> navigation rather than a manual join; a
        /// no-op for a brand-new page, which has no attachment rows yet at "revision 0").</item>
        /// </list>
        /// Unlike <paramref name="page"/>.Navigation - which callers are expected to have already resolved via
        /// <see cref="TwNamespaceNavigation.CleanAndValidate"/> before calling <see cref="UpsertPage"/>, same as
        /// the reference (<paramref name="page"/>.Navigation is read here but never written back onto
        /// <paramref name="page"/> itself) - the value actually written to
        /// <see cref="PagesEntities.Page.Navigation"/>/<see cref="PagesEntities.PageRevision.Namespace"/> is
        /// (re)computed from <paramref name="page"/>.Name via <see cref="TwNamespaceNavigation.CleanAndValidate"/>
        /// on every save, matching the reference's own <c>pageUpsertParam.Navigation</c>.
        /// </remarks>
        private async Task<int> SavePage(TwPage page)
        {
            var navigation = TwNamespaceNavigation.CleanAndValidate(page.Name);
            var newDataHash = SecurityUtility.Crc32(page.Body ?? string.Empty);
            var now = DateTime.UtcNow;

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                int currentPageRevision = 0;
                bool hasPageChanged;

                if (page.Id == 0)
                {
                    //This is a new page, just insert it.
                    var newPage = new PagesEntities.Page
                    {
                        Name = page.Name,
                        Namespace = page.Namespace,
                        Description = page.Description,
                        Navigation = navigation,
                        Revision = 1,
                        CreatedByUserId = page.CreatedByUserId,
                        CreatedDate = page.CreatedDate,
                        ModifiedByUserId = page.ModifiedByUserId,
                        ModifiedDate = now,
                    };

                    context.Pages_Pages.Add(newPage);
                    await context.SaveChangesAsync();

                    page.Id = newPage.Id;
                    hasPageChanged = true;
                }
                else
                {
                    //Get current page so we can determine if anything has changed.
                    var currentRevisionInfo = await GetLimitedPageInfoByIdAndRevision(page.Id)
                        ?? throw new Exception("The page could not be found.");

                    currentPageRevision = currentRevisionInfo.Revision;

                    //Update the existing page.
                    await context.Pages_Pages
                        .Where(p => p.Id == page.Id)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Description, page.Description)
                            .SetProperty(p => p.Name, page.Name)
                            .SetProperty(p => p.Namespace, page.Namespace)
                            .SetProperty(p => p.Navigation, navigation)
                            .SetProperty(p => p.ModifiedByUserId, page.ModifiedByUserId)
                            .SetProperty(p => p.ModifiedDate, now));

                    //Determine if anything has actually changed.
                    hasPageChanged = currentRevisionInfo.Name != page.Name
                        || currentRevisionInfo.Namespace != page.Namespace
                        || currentRevisionInfo.Description != page.Description
                        || currentRevisionInfo.ChangeSummary != page.ChangeSummary
                        || currentRevisionInfo.DataHash != newDataHash;
                }

                if (hasPageChanged)
                {
                    var previousPageRevision = currentPageRevision;
                    currentPageRevision++;

                    //The page content has actually changed (according to the checksum), so we will bump the page revision.
                    await context.Pages_Pages
                        .Where(p => p.Id == page.Id)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Revision, currentPageRevision));

                    //Insert the new actual page revision entry (this is the data).
                    context.Pages_PageRevisions.Add(new PagesEntities.PageRevision
                    {
                        PageId = page.Id,
                        Name = page.Name,
                        Namespace = page.Namespace,
                        Description = page.Description,
                        Body = page.Body ?? string.Empty,
                        DataHash = newDataHash,
                        Revision = currentPageRevision,
                        ChangeSummary = page.ChangeSummary ?? string.Empty,
                        ModifiedByUserId = page.ModifiedByUserId,
                        ModifiedDate = now,
                    });

                    await context.SaveChangesAsync();

                    //Associate all page attachments that are still current with the latest revision.
                    var carriedAttachments = await context.Pages_PageRevisionAttachments
                        .Where(pra => pra.PageId == page.Id
                            && pra.PageRevision == previousPageRevision
                            && pra.PageFile.Revision == pra.FileRevision)
                        .Select(pra => new { pra.PageFileId, pra.FileRevision })
                        .ToListAsync();

                    if (carriedAttachments.Count > 0)
                    {
                        context.Pages_PageRevisionAttachments.AddRange(carriedAttachments.Select(a => new PagesEntities.PageRevisionAttachment
                        {
                            PageId = page.Id,
                            PageFileId = a.PageFileId,
                            FileRevision = a.FileRevision,
                            PageRevision = currentPageRevision,
                        }));

                        await context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return page.Id;
        }

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
