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
    public class PrefixFileTests
    {
        private static readonly string DataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "GuidPrefixToName.txt");

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
            var bad = Lines()
                .Where(l => l.Length > 0)
                .Where(l => Map.TryParsePrefixLine(l, out var prefix, out var desc) == false
                            || l != $"{prefix}|{desc}"
                            || desc.Any(char.IsControl))
                .ToList();

            Assert.That(bad, Is.Empty);
        }

        [Test]
        public void NoDuplicatePrefixes()
        {
            var dupes = Lines()
                .Where(l => l.Length > 0)
                .GroupBy(l => l.Split('|')[0])
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            Assert.That(dupes, Is.Empty);
        }

        [TestCase("04271989-|OneDrive", true)]
        [TestCase("0e270daa-1be6-48f2-ac49-|Creative Cloud Files", true)]
        [TestCase("0e270daa-1be6-48f2-ac49-0|x", true)]
        [TestCase("0E270DAA-|uppercase", false)]
        [TestCase("0427198|too short", false)]
        [TestCase("042719890c4d2|no hyphen", false)]
        [TestCase("{04271989-|braces", false)]
        [TestCase("04271989-c4d2-b8f6-2d7a-c6fffc2420dd|whole guid", false)]
        [TestCase("04271989-|", false)]
        [TestCase("04271989-", false)]
        public void PrefixLines(string line, bool valid)
        {
            Assert.That(Map.TryParsePrefixLine(line, out _, out _), Is.EqualTo(valid));
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

        [TestCase("04271989-c4d2-d886-6546-56755f9d6a55", "OneDrive (per-account sync root)")]
        [TestCase("04271989-4a69-cd1d-35cd-c48f54d9c98c", "OneDrive (per-account sync root)")]
        [TestCase("{04271989-C4D2-1641-FD8E-F477126E208E}", "OneDrive (per-account sync root)")]
        [TestCase("0e270daa-1be6-48f2-ac49-063c719685ff", "Creative Cloud Files")]
        [TestCase("0E270DAA1BE648F2AC495771D3C50873", "Creative Cloud Files")]
        public void FamiliesAreMappedByPrefix(string input, string expected)
        {
            Assert.That(Map.TryGetDescription(input, out var desc), Is.True);
            Assert.That(desc, Is.EqualTo(expected));
            Assert.That(Map.GetDescriptionFromGuid(new Guid(input)), Is.EqualTo(expected));
        }

        [Test]
        public void ExactGuidWinsOverItsPrefix()
        {
            Assert.That(Map.GetDescriptionFromGuid("04271989-c4d2-b8f6-2d7a-c6fffc2420dd"), Is.EqualTo("OneDrive Business"));
        }

        [TestCase("0e270daa-1be6-48f2-ac48-063c719685ff")]
        [TestCase("04271988-c4d2-d886-6546-56755f9d6a55")]
        public void NearMissesOfAPrefixAreUnmapped(string input)
        {
            Assert.That(Map.TryGetDescription(input, out _), Is.False);
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
