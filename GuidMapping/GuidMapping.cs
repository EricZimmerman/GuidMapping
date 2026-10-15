using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GuidMapping
{
    public static class GuidMapping
    {
        internal const string ResourceName = "GuidMapping.GuidToName.txt";

        private static readonly Dictionary<Guid, string> Guids = Load();

        private static Dictionary<Guid, string> Load()
        {
            var guids = new Dictionary<Guid, string>();

            using (var stream = typeof(GuidMapping).Assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found");
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (TryParseLine(line, out var id, out var desc) && guids.ContainsKey(id) == false)
                        {
                            //first entry wins
                            guids.Add(id, desc);
                        }
                    }
                }
            }

            return guids;
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
        /// Looks up the description for a GUID in any format Guid.TryParse accepts (with or without braces, any case).
        /// </summary>
        public static bool TryGetDescription(string guid, out string description)
        {
            description = null;

            return Guid.TryParse(guid?.Trim(), out var id) && Guids.TryGetValue(id, out description);
        }

        public static bool TryGetDescription(Guid guid, out string description)
        {
            return Guids.TryGetValue(guid, out description);
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
