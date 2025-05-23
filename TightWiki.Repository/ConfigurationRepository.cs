﻿using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Caching;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class ConfigurationRepository
    {
        #region Upgrade Database.

        public static string GetVersionStateVersion()
        {
            var entries = ManagedDataStorage.Config.ExecuteScalar<string>(@"Scripts\Initialization\GetVersionStateVersion.sql");
            return entries ?? "0.0.0";
        }

        public static void SetVersionStateVersion()
        {
            var version = string.Join('.',
                (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3));
            ManagedDataStorage.Config.Execute(@"Scripts\Initialization\SetVersionStateVersion.sql", new { Version = version });
        }

        /// <summary>
        /// See @Initialization.Versions.md
        /// </summary>
        public static void UpgradeDatabase()
        {
            try
            {
                var versionString = GetVersionStateVersion();
                int storedPaddedVersion = Utility.PadVersionString(versionString);

                var assembly = Assembly.GetExecutingAssembly();

                int currentPaddedVersion = Utility.PadVersionString(
                    string.Join('.', (assembly.GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)));

                if (currentPaddedVersion == storedPaddedVersion)
                {
                    return; //The database version is already at the latest version.
                }

                Console.WriteLine($"Starting database upgrade.");

                var updateScriptNames = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                    .Where(o => o.Contains("Repository.Scripts.Initialization.Versions", StringComparison.InvariantCultureIgnoreCase)).OrderBy(o => o);

                string startVersionTag = ".Initialization.Versions.";
                string endVersionTag = ".^";

                foreach (var updateScriptName in updateScriptNames)
                {
                    int startIndex = updateScriptName.IndexOf(startVersionTag, StringComparison.InvariantCultureIgnoreCase);
                    if (startIndex >= 0)
                    {
                        startIndex += startVersionTag.Length;

                        int endIndex = updateScriptName.IndexOf(endVersionTag, startIndex, StringComparison.InvariantCultureIgnoreCase);
                        if (endIndex > startIndex)
                        {
                            //The name of the script file without the namespaces, version numbers etc.
                            var fullScriptName = updateScriptName.Substring(endIndex + endVersionTag.Length).Trim().Replace("_", "");

                            int filesFolderVersion = Utility.PadVersionString(updateScriptName.Substring(startIndex, endIndex - startIndex).Trim().Replace("_", ""));
                            if (filesFolderVersion > storedPaddedVersion)
                            {
                                Console.WriteLine($"Executing upgrade script: \"{updateScriptName}\"");

                                //Get the script text.
                                using var stream = assembly.GetManifestResourceStream(updateScriptName);
                                using var reader = new StreamReader(stream.EnsureNotNull());
                                var scriptText = reader.ReadToEnd();

                                //Get the script "metadata" from the file name.
                                var scriptNameParts = fullScriptName.Split('^');
                                //string executionOrder = scriptNameParts[0];
                                string databaseName = scriptNameParts[1];
                                //string scriptName = scriptNameParts[2];

                                var databaseFactory = ManagedDataStorage.Collection.Single(o => o.Name == databaseName).Factory;

                                databaseFactory.Execute(scriptText);
                            }
                        }
                    }
                }

                SetVersionStateVersion();
            }
            catch (Exception ex)
            {
                ExceptionRepository.InsertException(ex, "Database upgrade failed.");
            }
        }

        #endregion

        public static ConfigurationEntries GetConfigurationEntryValuesByGroupName(string groupName, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName]);
                if (!WikiCache.TryGet<ConfigurationEntries>(cacheKey, out var result))
                {
                    result = GetConfigurationEntryValuesByGroupName(groupName, false);
                    WikiCache.Put(cacheKey, result);
                }

                return result;
            }

            var entries = ManagedDataStorage.Config.Query<ConfigurationEntry>
                ("GetConfigurationEntryValuesByGroupName.sql", new { GroupName = groupName }).ToList();

            foreach (var entry in entries)
            {
                if (entry.IsEncrypted)
                {
                    try
                    {
                        entry.Value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, entry.Value);
                    }
                    catch
                    {
                        entry.Value = "";
                    }
                }
            }

            return new ConfigurationEntries(entries);
        }

        public static List<Theme> GetAllThemes()
        {
            var collection = ManagedDataStorage.Config.Query<Theme>("GetAllThemes.sql").ToList();

            foreach (var theme in collection)
            {
                theme.Files = theme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return collection;
        }

        public static WikiDatabaseStatistics GetWikiDatabaseMetrics()
        {
            return ManagedDataStorage.Config.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var pages_db = o.Attach("pages.db", "pages_db");

                var result = o.QuerySingle<WikiDatabaseStatistics>("GetWikiDatabaseStatistics.sql");
                result.Exceptions = ExceptionRepository.GetExceptionCount();

                return result;
            });
        }

        /// <summary>
        /// Determines if this is the first time the wiki has run. Returns true if it is the first time.
        /// </summary>
        public static bool IsFirstRun()
        {
            bool isEncryptionValid = GetCryptoCheck();
            if (isEncryptionValid == false)
            {
                SetCryptoCheck();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reads an encrypted value from the database so we can determine if encryption is setup.
        /// If the value is missing then we are NOT setup.
        /// If the value is present but we cant decrypt it, then we are NOT setup.
        /// If the value is present and we can decrypt it, then we are setup and good to go!
        /// </summary>
        public static bool GetCryptoCheck()
        {
            var value = ManagedDataStorage.Config.QueryFirstOrDefault<string>("GetCryptoCheck.sql") ?? string.Empty;

            try
            {
                value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, value);
                if (value == Constants.CRYPTOCHECK)
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Writes an encrypted value to the database so we can test at a later time to ensure that encryption is setup.
        /// </summary>
        public static void SetCryptoCheck()
        {
            var param = new
            {
                Content = Security.Helpers.EncryptString(Security.Helpers.MachineKey, Constants.CRYPTOCHECK)
            };

            ManagedDataStorage.Config.QueryFirstOrDefault<string>("SetCryptoCheck.sql", param);
        }

        public static void SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            var param = new
            {
                GroupName = groupName,
                EntryName = entryName,
                Value = value
            };

            ManagedDataStorage.Config.Execute("SaveConfigurationEntryValueByGroupAndEntry.sql", param);

            ReloadEverything();
        }

        public static List<ConfigurationNest> GetConfigurationNest()
        {
            var result = new List<ConfigurationNest>();
            var flatConfig = GetFlatConfiguration();

            var groups = flatConfig.GroupBy(o => o.GroupId).ToList();
            foreach (var group in groups)
            {
                var nest = new ConfigurationNest
                {
                    Id = group.Key,
                    Name = group.Select(o => o.GroupName).First(),
                    Description = group.Select(o => o.GroupDescription).First()
                };

                foreach (var value in group.OrderBy(o => o.EntryName))
                {
                    string entryValue;
                    if (value.IsEncrypted)
                    {
                        try
                        {
                            entryValue = Security.Helpers.DecryptString(Security.Helpers.MachineKey, value.EntryValue);
                        }
                        catch
                        {
                            entryValue = "";
                        }
                    }
                    else
                    {
                        entryValue = value.EntryValue;
                    }

                    nest.Entries.Add(new ConfigurationEntry()
                    {
                        Id = value.EntryId,
                        Value = entryValue,
                        Description = value.EntryDescription,
                        Name = value.EntryName,
                        DataType = value.DataType.ToLowerInvariant(),
                        IsEncrypted = value.IsEncrypted,
                        ConfigurationGroupId = group.Key,
                    });
                }
                result.Add(nest);
            }

            return result;
        }

        public static List<ConfigurationFlat> GetFlatConfiguration()
            => ManagedDataStorage.Config.Query<ConfigurationFlat>("GetFlatConfiguration.sql").ToList();

        public static string? GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName, entryName]);
                if (!WikiCache.TryGet<string>(cacheKey, out var result))
                {
                    if ((result = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName, false)) != null)
                    {
                        WikiCache.Put(cacheKey, result);
                    }
                }

                return result;
            }

            var param = new
            {
                GroupName = groupName,
                EntryName = entryName
            };

            var configEntry = ManagedDataStorage.Config.QuerySingle<ConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName.sql", param);
            if (configEntry?.IsEncrypted == true)
            {
                try
                {
                    configEntry.Value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, configEntry.Value);
                }
                catch
                {
                    configEntry.Value = "";
                }
            }

            return configEntry?.Value?.ToString();
        }

        public static T? Get<T>(string groupName, string entryName)
        {
            var value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);
            return Converters.ConvertTo<T>(value.EnsureNotNull());
        }

        public static T? Get<T>(string groupName, string entryName, T defaultValue)
        {
            var value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

            if (value == null)
            {
                return defaultValue;
            }

            return Converters.ConvertTo<T>(value);
        }

        #region Menu Items.

        public static List<MenuItem> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null)
        {
            var query = RepositoryHelper.TransposeOrderby("GetAllMenuItems.sql", orderBy, orderByDirection);
            return ManagedDataStorage.Config.Query<MenuItem>(query).ToList();
        }

        public static MenuItem GetMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            return ManagedDataStorage.Config.QuerySingle<MenuItem>("GetMenuItemById.sql", param);
        }

        public static void DeleteMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            ManagedDataStorage.Config.Execute("DeleteMenuItemById.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = GetAllMenuItems();
        }

        public static int UpdateMenuItemById(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Id,
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = ManagedDataStorage.Config.ExecuteScalar<int>("UpdateMenuItemById.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = GetAllMenuItems();

            return menuItemId;
        }

        public static int InsertMenuItem(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = ManagedDataStorage.Config.ExecuteScalar<int>("InsertMenuItem.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = GetAllMenuItems();

            return menuItemId;
        }

        #endregion

        public static void ReloadEmojis()
        {
            WikiCache.ClearCategory(WikiCache.Category.Emoji);
            GlobalConfiguration.Emojis = EmojiRepository.GetAllEmojis();

            if (GlobalConfiguration.PreLoadAnimatedEmojis)
            {
                new Thread(() =>
                {
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount / 2 < 2 ? 2 : Environment.ProcessorCount / 2
                    };

                    Parallel.ForEach(GlobalConfiguration.Emojis, parallelOptions, emoji =>
                    {
                        if (emoji.MimeType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var imageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [emoji.Shortcut]);
                            emoji.ImageData = EmojiRepository.GetEmojiByName(emoji.Name)?.ImageData;

                            if (emoji.ImageData != null)
                            {
                                var scaledImageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [emoji.Shortcut, "100"]);
                                var decompressedImageBytes = Utility.Decompress(emoji.ImageData);
                                var img = Image.Load(new MemoryStream(decompressedImageBytes));

                                int customScalePercent = 100;

                                var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, GlobalConfiguration.DefaultEmojiHeight);

                                //Adjust to any specified scaling.
                                Height = (int)(Height * (customScalePercent / 100.0));
                                Width = (int)(Width * (customScalePercent / 100.0));

                                //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                                //  dimension to become very small (or even negative). So here we will check the height and width
                                //  to ensure they are both at least n pixels and adjust both dimensions.
                                if (Height < 16)
                                {
                                    Height += 16 - Height;
                                    Width += 16 - Height;
                                }
                                if (Width < 16)
                                {
                                    Height += 16 - Width;
                                    Width += 16 - Width;
                                }

                                //These are hard to generate, so just keep it forever.
                                var resized = Images.ResizeGifImage(decompressedImageBytes, Width, Height);
                                var itemCache = new ImageCacheItem(resized, "image/gif");
                                WikiCache.Put(scaledImageCacheKey, itemCache, new CacheItemPolicy());
                            }
                        }
                    });
                }).Start();
            }
        }

        public static void ReloadEverything()
        {
            WikiCache.Clear();

            GlobalConfiguration.IsDebug = Debugger.IsAttached;

            var performanceConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Performance, false);
            GlobalConfiguration.PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            GlobalConfiguration.RecordCompilationMetrics = performanceConfig.Value<bool>("Record Compilation Metrics");
            GlobalConfiguration.CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            WikiCache.Initialize(GlobalConfiguration.CacheMemoryLimitMB, GlobalConfiguration.PageCacheSeconds);

            var basicConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Basic);
            var customizationConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Customization);
            var htmlConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.HTMLLayout);
            var functionalityConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Functionality);
            var membershipConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Membership);
            var searchConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Search);
            var filesAndAttachmentsConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.FilesAndAttachments);

            GlobalConfiguration.Address = basicConfig?.Value<string>("Address") ?? string.Empty;
            GlobalConfiguration.Name = basicConfig?.Value<string>("Name") ?? string.Empty;
            GlobalConfiguration.Copyright = basicConfig?.Value<string>("Copyright") ?? string.Empty;

            var themeName = customizationConfig.Value("Theme", "Light");

            GlobalConfiguration.FixedMenuPosition = customizationConfig.Value("Fixed Header Menu Position", false);
            GlobalConfiguration.AllowSignup = membershipConfig.Value("Allow Signup", false);
            GlobalConfiguration.DefaultProfileRecentlyModifiedCount = performanceConfig.Value<int>("Default Profile Recently Modified Count");
            GlobalConfiguration.PreLoadAnimatedEmojis = performanceConfig.Value<bool>("Pre-Load Animated Emojis");
            GlobalConfiguration.SystemTheme = GetAllThemes().Single(o => o.Name == themeName);
            GlobalConfiguration.DefaultEmojiHeight = customizationConfig.Value<int>("Default Emoji Height");
            GlobalConfiguration.AllowGoogleAuthentication = membershipConfig.Value<bool>("Allow Google Authentication");
            GlobalConfiguration.DefaultTimeZone = customizationConfig?.Value<string>("Default TimeZone") ?? string.Empty;
            GlobalConfiguration.IncludeWikiDescriptionInMeta = functionalityConfig.Value<bool>("Include wiki Description in Meta");
            GlobalConfiguration.IncludeWikiTagsInMeta = functionalityConfig.Value<bool>("Include wiki Tags in Meta");
            GlobalConfiguration.EnablePageComments = functionalityConfig.Value<bool>("Enable Page Comments");
            GlobalConfiguration.EnablePublicProfiles = functionalityConfig.Value<bool>("Enable Public Profiles");
            GlobalConfiguration.ShowCommentsOnPageFooter = functionalityConfig.Value<bool>("Show Comments on Page Footer");
            GlobalConfiguration.ShowLastModifiedOnPageFooter = functionalityConfig.Value<bool>("Show Last Modified on Page Footer");
            GlobalConfiguration.IncludeSearchOnNavbar = searchConfig.Value<bool>("Include Search on Navbar");
            GlobalConfiguration.HTMLHeader = htmlConfig?.Value<string>("Header") ?? string.Empty;
            GlobalConfiguration.HTMLFooter = htmlConfig?.Value<string>("Footer") ?? string.Empty;
            GlobalConfiguration.HTMLPreBody = htmlConfig?.Value<string>("Pre-Body") ?? string.Empty;
            GlobalConfiguration.HTMLPostBody = htmlConfig?.Value<string>("Post-Body") ?? string.Empty;
            GlobalConfiguration.BrandImageSmall = customizationConfig?.Value<string>("Brand Image (Small)") ?? string.Empty;
            GlobalConfiguration.FooterBlurb = customizationConfig?.Value<string>("FooterBlurb") ?? string.Empty;
            GlobalConfiguration.MaxAvatarFileSize = filesAndAttachmentsConfig.Value<int>("Max Avatar File Size");
            GlobalConfiguration.MaxAttachmentFileSize = filesAndAttachmentsConfig.Value<int>("Max Attachment File Size");
            GlobalConfiguration.MaxEmojiFileSize = filesAndAttachmentsConfig.Value<int>("Max Emoji File Size");

            GlobalConfiguration.MenuItems = GetAllMenuItems();

            ReloadEmojis();
        }
    }
}
