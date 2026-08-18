using TightWiki.Plugin.Models.Defaults;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    ///  Data access for default values that are used when creating a new wiki, such as default configuration entries, default themes, etc.
    /// </summary>
    public interface ITwDefaultsRepository
    {
        /// <summary>
        /// Returns the distinct set of default configuration groups (name and description) used to seed the database.
        /// </summary>
        Task<List<TwDefaultConfiguration>> GetDefaultConfigurationGroups();

        /// <summary>
        /// Returns all default configuration entries used to seed the database.
        /// </summary>
        Task<List<TwDefaultConfiguration>> GetDefaultConfigurations();

        /// <summary>
        /// Returns all default themes used to seed the database.
        /// </summary>
        Task<List<TwDefaultTheme>> GetDefaultThemes();

        /// <summary>
        /// Returns all default wiki pages belonging to the specified namespace, used to seed the database.
        /// </summary>
        Task<List<TwDefaultWikiPage>> GetDefaultWikiPages(string namespaceName);

        /// <summary>
        /// Returns all default feature templates used to seed the database.
        /// </summary>
        Task<List<TwDefaultFeatureTemplate>> GetDefaultFeatureTemplates();
    }
}
