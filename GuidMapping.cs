using System;
using System.Collections.Generic;
using GuidMapping.Properties;

namespace GuidMapping
{
    public static class GuidMapping
    {
        private static readonly Dictionary<string, string> Guids;

        private static readonly List<string> Ignore;


        static GuidMapping()
        {
            //GUIDs to ignore
            Ignore = new List<string>
            {
                "20000000-0000-0000-0000-000000000000",
                "00000000-0000-0000-0000-000001000000",
                "00000000-0000-0000-0000-000000000000"
            };

            string[] stringSeparators = {"\r\n"};

            var lines = Resources.GuidToName.Split(stringSeparators, StringSplitOptions.None);

            Guids = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                var segs = line.Split('|');

                if (segs.Length != 2)
                {
                    continue;
                }

                var id = segs[0].Trim().ToLowerInvariant();
                var desc = segs[1].Trim();

                if (id.Length != 36)
                {
                    continue;
                }

                if (Guids.ContainsKey(id) == false)
                {
                    Guids.Add(id, desc);
                }
            }
        }

        public static string GetDescriptionFromGuid(string guid)
        {
            var tempValue = guid.ToLowerInvariant();

            if (tempValue.StartsWith("{"))
            {
                tempValue =     tempValue.Replace("{","").Replace("}","");
            }

            if (Guids.ContainsKey(tempValue))
            {
                return Guids[tempValue];
            }

            return $"Unmapped GUID: {guid}";
        }
    }
}