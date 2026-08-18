using NTDLS.SqliteDapperWrapper;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TightWiki.Plugin.Models.Defaults;

namespace GenerateSeedData.SeedPackage
{
    /// <summary>
    /// Builds "Seed\tightwiki.seed.zip" - the provider-neutral seed package described in
    /// Database-Providers-Plan.md chapter 4.6b. It is read from Data\*.db (the same live, populated SQLite
    /// databases that <see cref="Program.GenerateDefaultsDatabase"/> reads from), and is meant to be consumed by
    /// TightWiki.Data.EfCore's EfDefaultsRepository when seeding a freshly created MSSQL/Postgres database -
    /// never at SQLite runtime, and never by this repo's ASP.NET Core app.
    ///
    /// Layout inside the zip (one JSON manifest per table, System.Text.Json, indented for readability/diffability):
    ///
    ///   ConfigurationGroup.json           - config.db ConfigurationGroup, raw dump
    ///   ConfigurationEntry.json           - config.db ConfigurationEntry, raw dump
    ///   MenuItem.json                     - config.db MenuItem, raw dump
    ///   Theme.json                        - config.db Theme, raw dump
    ///   FeatureTemplate.json              - pages.db FeatureTemplate joined to Page.Name (PageId is not portable)
    ///   DefaultWikiPages/&lt;Namespace&gt;.json - pages.db Page+PageRevision, one file per namespace (Builtin,
    ///                                      Include, Wiki Help), matching the future
    ///                                      ITwDefaultsRepository.GetDefaultWikiPages(namespace) signature
    ///   Emoji.json                        - emoji.db Emoji metadata (Id, Name, MimeType) + a reference to the
    ///                                      corresponding Emoji/Images/* zip entry - no base64, see below
    ///   EmojiCategory.json                - emoji.db EmojiCategory, raw dump
    ///   Emoji/Images/&lt;Id&gt;&lt;ext&gt;      - the emoji image bytes, stored as their own zip entries rather than
    ///                                      base64-encoded inside Emoji.json (~18 MB of images would otherwise
    ///                                      bloat/slow down JSON parsing for no benefit)
    /// </summary>
    internal static class SeedPackageGenerator
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static void Generate(string dbPath, string seedZipPath)
        {
            File.Delete(seedZipPath);

            using var configDb = new SqliteManagedInstance(Path.Combine(dbPath, "config.db"));
            using var pagesDb = new SqliteManagedInstance(Path.Combine(dbPath, "pages.db"));
            using var emojiDb = new SqliteManagedInstance(Path.Combine(dbPath, "emoji.db"));

            using var zipStream = new FileStream(seedZipPath, FileMode.CreateNew, FileAccess.Write);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            Console.WriteLine("Generating: Seed\\tightwiki.seed.zip");

            Console.WriteLine("  Adding: ConfigurationGroup.json");
            var configurationGroups = configDb.Query<SeedConfigurationGroup>(@"Scripts\Seed\GetConfigurationGroups.sql");
            WriteJsonEntry(archive, "ConfigurationGroup.json", configurationGroups);

            Console.WriteLine("  Adding: ConfigurationEntry.json");
            var configurationEntries = configDb.Query<SeedConfigurationEntry>(@"Scripts\Seed\GetConfigurationEntries.sql");
            WriteJsonEntry(archive, "ConfigurationEntry.json", configurationEntries);

            Console.WriteLine("  Adding: MenuItem.json");
            var menuItems = configDb.Query<SeedMenuItem>(@"Scripts\Seed\GetMenuItems.sql");
            WriteJsonEntry(archive, "MenuItem.json", menuItems);

            Console.WriteLine("  Adding: Theme.json");
            var themes = configDb.Query<TwDefaultTheme>(@"Scripts\GetDefaultThemes.sql");
            WriteJsonEntry(archive, "Theme.json", themes);

            Console.WriteLine("  Adding: FeatureTemplate.json");
            var featureTemplates = pagesDb.Query<TwDefaultFeatureTemplate>(@"Scripts\GetFeatureTemplates.sql");
            WriteJsonEntry(archive, "FeatureTemplate.json", featureTemplates);

            var wikiPages = pagesDb.Query<TwDefaultWikiPage>(@"Scripts\GetDefaultDefaultWikiPages.sql");
            foreach (var namespaceGroup in wikiPages.GroupBy(p => p.Namespace).OrderBy(g => g.Key))
            {
                string entryName = $"DefaultWikiPages/{namespaceGroup.Key}.json";
                Console.WriteLine($"  Adding: {entryName}");
                WriteJsonEntry(archive, entryName, namespaceGroup.ToList());
            }

            Console.WriteLine("  Adding: EmojiCategory.json");
            var emojiCategories = emojiDb.Query<SeedEmojiCategory>(@"Scripts\Seed\GetEmojiCategories.sql");
            WriteJsonEntry(archive, "EmojiCategory.json", emojiCategories);

            Console.WriteLine("  Adding: Emoji.json + Emoji/Images/*");
            var sourceEmojis = emojiDb.Query<SourceEmoji>(@"Scripts\Seed\GetEmoji.sql");
            var emojiManifest = new List<SeedEmoji>();
            foreach (var emoji in sourceEmojis)
            {
                string imageEntryName = $"Emoji/Images/{emoji.Id}{GetExtensionForMimeType(emoji.MimeType)}";

                var imageEntry = archive.CreateEntry(imageEntryName, CompressionLevel.Optimal);
                using (var entryStream = imageEntry.Open())
                {
                    var imageData = emoji.ImageData ?? [];
                    entryStream.Write(imageData, 0, imageData.Length);
                }

                emojiManifest.Add(new SeedEmoji
                {
                    Id = emoji.Id,
                    Name = emoji.Name,
                    MimeType = emoji.MimeType,
                    ImageEntry = imageEntryName,
                });
            }
            WriteJsonEntry(archive, "Emoji.json", emojiManifest);

            Console.WriteLine($"  {emojiManifest.Count} emoji image(s) embedded.");
        }

        /// <summary>
        /// Serializes <paramref name="data"/> as indented JSON into a new zip entry named <paramref name="entryName"/>.
        /// </summary>
        private static void WriteJsonEntry<T>(ZipArchive archive, string entryName, T data)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(JsonSerializer.Serialize(data, JsonOptions));
        }

        /// <summary>
        /// Maps an emoji's stored MIME type to a file extension for its Emoji/Images/* zip entry. Falls back to
        /// ".bin" for anything unrecognized rather than failing the whole export over one odd row.
        /// </summary>
        private static string GetExtensionForMimeType(string mimeType) => mimeType.Trim().ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "image/bmp" => ".bmp",
            _ => ".bin",
        };
    }
}
