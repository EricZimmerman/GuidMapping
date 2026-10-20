using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GuidMapping
{
    public static class GuidMapping
    {
        internal const string ResourceName = "GuidMapping.GuidToName.txt";
        internal const string PrefixResourceName = "GuidMapping.GuidPrefixToName.txt";

        private static readonly Dictionary<Guid, string> Guids = Load();

        /// <summary>
        /// GUIDs of a family (one per account or install, such as OneDrive's and Creative Cloud's sync roots) by the part they
        /// share, longest first. Only used when the exact GUID is not mapped.
        /// </summary>
        private static readonly List<KeyValuePair<string, string>> Prefixes = LoadPrefixes();

        private static Dictionary<Guid, string> Load()
        {
            var guids = new Dictionary<Guid, string>();

            foreach (var line in ReadResource(ResourceName))
            {
                if (TryParseLine(line, out var id, out var desc) && guids.ContainsKey(id) == false)
                {
                    //first entry wins
                    guids.Add(id, desc);
                }
            }

            return guids;
        }

        private static List<KeyValuePair<string, string>> LoadPrefixes()
        {
            var prefixes = new List<KeyValuePair<string, string>>();

            foreach (var line in ReadResource(PrefixResourceName))
            {
                if (TryParsePrefixLine(line, out var prefix, out var desc))
                {
                    prefixes.Add(new KeyValuePair<string, string>(prefix, desc));
                }
            }

            prefixes.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));

            return prefixes;
        }

        private static IEnumerable<string> ReadResource(string name)
        {
            using (var stream = typeof(GuidMapping).Assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Embedded resource '{name}' not found");
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }

        /// <summary>
        /// Parses a 'guid|description' line. Returns false for blank or malformed lines.
        /// </summary>
        internal static bool TryParseLine(string line, out Guid id, out string desc)
        {
            id = Guid.Empty;
            desc = null;

            var segs = line.Split('|');

            if (segs.Length != 2 || Guid.TryParseExact(segs[0].Trim(), "D", out id) == false)
            {
                return false;
            }

            desc = segs[1].Trim();

            return desc.Length > 0;
        }

        /// <summary>
        /// Parses a 'prefix|description' line: the start of a GUID as 'D' formats it (lowercase, with its hyphens), at least
        /// 8 characters and not a whole GUID. Returns false for blank or malformed lines.
        /// </summary>
        internal static bool TryParsePrefixLine(string line, out string prefix, out string desc)
        {
            prefix = null;
            desc = null;

            var segs = line.Split('|');
            if (segs.Length != 2)
            {
                return false;
            }

            var candidate = segs[0].Trim();
            if (candidate.Length < 8 || candidate.Length >= 36)
            {
                return false;
            }

            for (var i = 0; i < candidate.Length; i++)
            {
                var c = candidate[i];
                var isHyphen = i == 8 || i == 13 || i == 18 || i == 23;

                if (isHyphen ? c != '-' : (c >= '0' && c <= '9' || c >= 'a' && c <= 'f') == false)
                {
                    return false;
                }
            }

            prefix = candidate;
            desc = segs[1].Trim();

            return desc.Length > 0;
        }

        /// <summary>
        /// Looks up the description for a GUID in any format Guid.TryParse accepts (with or without braces, any case).
        /// </summary>
        public static bool TryGetDescription(string guid, out string description)
        {
            description = null;

            return Guid.TryParse(guid?.Trim(), out var id) && TryGetDescription(id, out description);
        }

        /// <summary>
        /// Looks up the description for a GUID: the GUID itself, else the family it belongs to by its prefix.
        /// </summary>
        public static bool TryGetDescription(Guid guid, out string description)
        {
            if (Guids.TryGetValue(guid, out description))
            {
                return true;
            }

            var text = guid.ToString("D");
            foreach (var prefix in Prefixes)
            {
                if (text.StartsWith(prefix.Key, StringComparison.Ordinal))
                {
                    description = prefix.Value;
                    return true;
                }
            }

            return false;
        }

        public static string GetDescriptionFromGuid(string guid)
        {
            return TryGetDescription(guid, out var desc) ? desc : $"Unmapped GUID: {guid}";
        }

        public static string GetDescriptionFromGuid(Guid guid)
        {
            return TryGetDescription(guid, out var desc) ? desc : $"Unmapped GUID: {guid}";
        }
    }
}
