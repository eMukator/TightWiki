﻿using NTDLS.SqliteDapperWrapper;

namespace TightWiki.Repository
{
    internal static class RepositoryHelper
    {
        /// <summary>
        /// Fills in a custom orderby on a given sql script.
        /// </summary>
        public static string TransposeOrderby(string filename, string? orderBy, string? orderByDirection)
        {
            var script = SqliteManagedInstance.TranslateSqlScript(filename);

            if (string.IsNullOrEmpty(orderBy))
            {
                return script;
            }

            string beginParentTag = "--CUSTOM_ORDER_BEGIN::";
            string endParentTag = "--::CUSTOM_ORDER_BEGIN";

            string beginConfigTag = "--CONFIG::";
            string endConfigTag = "--::CONFIG";

            while (true)
            {
                int beginParentIndex = script.IndexOf(beginParentTag, StringComparison.InvariantCultureIgnoreCase);
                int endParentIndex = script.IndexOf(endParentTag, StringComparison.InvariantCultureIgnoreCase);

                if (beginParentIndex > 0 && endParentIndex > beginParentIndex)
                {
                    var sectionText = script.Substring(beginParentIndex + beginParentTag.Length, (endParentIndex - beginParentIndex) - endParentTag.Length).Trim();

                    int beginConfigIndex = sectionText.IndexOf(beginConfigTag, StringComparison.InvariantCultureIgnoreCase);
                    int endConfigIndex = sectionText.IndexOf(endConfigTag, StringComparison.InvariantCultureIgnoreCase);

                    if (beginConfigIndex >= 0 && endConfigIndex > beginConfigIndex)
                    {
                        var configText = sectionText.Substring(beginConfigIndex + beginConfigTag.Length, (endConfigIndex - beginConfigIndex) - endConfigTag.Length).Trim();

                        var configs = configText.Split("\n").Select(o => o.Trim())
                            .Where(o => o.Contains('='))
                            .Select(o => (Name: o.Split("=")[0], Field: o.Split("=")[1]));

                        var selectedConfig = configs.SingleOrDefault(o => string.Equals(o.Name, orderBy, StringComparison.InvariantCultureIgnoreCase));

                        if (selectedConfig == default)
                        {
                            throw new Exception($"No order by mapping was found in '{filename}' for the field '{orderBy}'.");
                        }

                        script = script.Substring(0, beginParentIndex)
                            + $"ORDER BY\r\n\t{selectedConfig.Field} "
                            + (string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase) ? "asc" : "desc")
                            + script.Substring(endParentIndex + endParentTag.Length);
                    }
                    else
                    {
                        throw new Exception($"No order configuration was found in '{filename}'.");
                    }
                }
                else
                {
                    break;
                }
            }

            return script;
        }
    }
}
