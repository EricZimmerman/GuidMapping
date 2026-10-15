using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Map = GuidMapping.GuidMapping;

namespace GuidMapping.Test
{
    [TestFixture]
    public class DataFileTests
    {
        private static readonly string DataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "GuidToName.txt");

        private static string[] Lines()
        {
            return File.ReadAllText(DataPath, Encoding.UTF8).Split(new[] { "\r\n" }, StringSplitOptions.None);
        }

        [Test]
        public void FileIsUtf8WithBomAndCrlfOnly()
        {
            var bytes = File.ReadAllBytes(DataPath);
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }), "missing UTF-8 BOM");

            var text = Encoding.UTF8.GetString(bytes);
            Assert.That(text.Replace("\r\n", "").IndexOfAny(new[] { '\r', '\n' }), Is.EqualTo(-1), "bare CR or LF line ending");
        }

        [Test]
        public void EveryLineIsWellFormed()
        {
            var bad = new List<string>();
            var lines = Lines();

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length == 0)
                {
                    continue;
                }

                var ok = Map.TryParseLine(line, out var id, out var desc)
                         && line == $"{id:D}|{desc}" // canonical: lowercase, no braces, no padding
                         && desc.All(c => char.IsControl(c) == false);

                if (ok == false)
                {
                    bad.Add($"{i + 1}: {line}");
                }
            }

            Assert.That(bad, Is.Empty);
        }

        [Test]
        public void NoDuplicateGuids()
        {
            var dupes = Lines()
                .Where(l => l.Length > 0)
                .GroupBy(l => l.Split('|')[0])
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            Assert.That(dupes, Is.Empty);
        }
    }

    [TestFixture]
    public class LookupTests
    {
        private const string Known = "3da494e4-0fe2-415c-b895-fb5265c5c83b";
        private const string KnownDesc = "Microsoft-Windows-DeviceManagement-Enterprise-Diagnostics-Provider";

        [TestCase(Known)]
        [TestCase("3DA494E4-0FE2-415C-B895-FB5265C5C83B")]
        [TestCase("{3da494e4-0fe2-415c-b895-fb5265c5c83b}")]
        [TestCase("{3DA494E4-0FE2-415C-B895-FB5265C5C83B}")]
        [TestCase(" 3da494e4-0fe2-415c-b895-fb5265c5c83b ")]
        [TestCase("3da494e40fe2415cb895fb5265c5c83b")]
        public void FindsKnownGuidInAnyFormat(string input)
        {
            Assert.That(Map.GetDescriptionFromGuid(input), Is.EqualTo(KnownDesc));
        }

        [Test]
        public void GuidOverload()
        {
            Assert.That(Map.GetDescriptionFromGuid(new Guid(Known)), Is.EqualTo(KnownDesc));
            Assert.That(Map.TryGetDescription(new Guid(Known), out var desc), Is.True);
            Assert.That(desc, Is.EqualTo(KnownDesc));
        }

        [TestCase("00000000-0000-0000-0000-000000000000")]
        [TestCase("not a guid")]
        [TestCase("")]
        [TestCase(null)]
        public void UnmappedInputs(string input)
        {
            Assert.That(Map.TryGetDescription(input, out var desc), Is.False);
            Assert.That(desc, Is.Null);
            Assert.That(Map.GetDescriptionFromGuid(input), Is.EqualTo($"Unmapped GUID: {input}"));
        }

        [Test]
        public void NonAsciiDescriptionsDecodeCorrectly()
        {
            Assert.That(Map.GetDescriptionFromGuid("cc2553fe-0c12-40bb-8d08-969eacd8c686"), Is.EqualTo("Intel® Software Installer"));
            Assert.That(Map.GetDescriptionFromGuid("c5c91aa6-3e83-430e-8b7a-6b790083f28d"), Is.EqualTo("Microsoft Azure Libraries for .NET – v2.9"));
        }

        [Test]
        public void AllWellFormedLinesAreLoaded()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "GuidToName.txt");
            var count = 0;

            foreach (var line in File.ReadLines(path))
            {
                if (Map.TryParseLine(line, out var id, out var desc))
                {
                    count++;
                    Assert.That(Map.GetDescriptionFromGuid(id), Is.EqualTo(desc));
                }
            }

            Assert.That(count, Is.GreaterThan(14000));
        }
    }
}
