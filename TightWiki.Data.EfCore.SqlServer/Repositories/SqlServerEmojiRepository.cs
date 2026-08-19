using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Data.EfCore.SqlServer.Repositories
{
    /// <summary>
    /// MSSQL/EF Core implementation of <see cref="ITwEmojiRepository"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton only (Database-Providers-Plan.md phase 2a.1) - every member throws
    /// <see cref="NotImplementedException"/> for now. Real LINQ-based implementations land in phase 2a.8.
    /// See <see cref="SqlServerConfigurationRepository"/> for why this is a concrete class rather than typing
    /// <see cref="SqlServerDatabaseManager.EmojiRepository"/> directly as <see cref="ITwEmojiRepository"/>.
    /// </remarks>
    public class SqlServerEmojiRepository : ITwEmojiRepository
    {
        public Task<List<TwEmoji>> GetAllEmojis()
            => throw new NotImplementedException();

        public Task<List<string>> AutoCompleteEmoji(string term)
            => throw new NotImplementedException();

        public Task<List<TwEmoji>> GetEmojisByCategory(string category)
            => throw new NotImplementedException();

        public Task<List<TwEmojiCategory>> GetEmojiCategoriesGrouped()
            => throw new NotImplementedException();

        public Task<List<int>> SearchEmojiCategoryIds(List<string> categories)
            => throw new NotImplementedException();

        public Task<List<TwEmojiCategory>> GetEmojiCategoriesByName(string name)
            => throw new NotImplementedException();

        public Task DeleteById(int id)
            => throw new NotImplementedException();

        public Task<TwEmoji?> GetEmojiByName(string name)
            => throw new NotImplementedException();

        public Task<int> UpsertEmoji(TwUpsertEmoji emoji)
            => throw new NotImplementedException();

        public Task<List<TwEmoji>> GetAllEmojisPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? categories = null)
            => throw new NotImplementedException();

        public Task<List<TwEmoji>> ReloadEmojis(bool preloadAnimatedEmojis, int defaultEmojiHeight)
            => throw new NotImplementedException();
    }
}
