using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models.Defaults;

namespace TightWiki.Repository
{
    public partial class DefaultsRepository
        : ITwDefaultsRepository
    {
        public SqliteManagedFactory DefaultsFactory { get; private set; }

        public DefaultsRepository(string connectionString)
        {
            DefaultsFactory = new SqliteManagedFactory(connectionString);
        }

        public async Task<List<TwDefaultConfiguration>> GetDefaultConfigurationGroups()
            => await DefaultsFactory.QueryAsync<TwDefaultConfiguration>(@"Scripts\Defaults\GetDefaultConfigurationGroups.sql");

        public async Task<List<TwDefaultConfiguration>> GetDefaultConfigurations()
            => await DefaultsFactory.QueryAsync<TwDefaultConfiguration>(@"Scripts\Defaults\GetDefaultConfigurations.sql");

        public async Task<List<TwDefaultTheme>> GetDefaultThemes()
            => await DefaultsFactory.QueryAsync<TwDefaultTheme>(@"Scripts\Defaults\GetDefaultThemes.sql");

        public async Task<List<TwDefaultWikiPage>> GetDefaultWikiPages(string namespaceName)
            => await DefaultsFactory.QueryAsync<TwDefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql",
                new { Namespace = namespaceName });

        public async Task<List<TwDefaultFeatureTemplate>> GetDefaultFeatureTemplates()
            => await DefaultsFactory.QueryAsync<TwDefaultFeatureTemplate>(@"Scripts\Defaults\GetDefaultFeatureTemplates.sql");
    }
}
