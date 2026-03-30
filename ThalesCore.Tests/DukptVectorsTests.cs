using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ThalesCore.Tests
{
    [TestFixture]
    public class DukptVectorsTests
    {
        [Test]
        public void DukptVectors_Yaml_IsWellFormed()
        {
            // locate vectors file by walking up from current directory (tests run from project/bin folders)
            string path = null;
            var cur = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (cur != null)
            {
                var cand = Path.Combine(cur.FullName, "test_vectors", "dukpt_vectors.yaml");
                if (File.Exists(cand)) { path = cand; break; }
                cur = cur.Parent;
            }
            Assert.IsNotNull(path, "Vectors file not found: test_vectors/dukpt_vectors.yaml (searched upward from CWD)");

            var text = File.ReadAllText(path);
            // split on top-level dash entries
            var rawEntries = Regex.Split(text, "(?m)^- id:").Select(s => s.Trim()).Where(s => s.Length>0);
            // skip preamble/comment block which may appear before first '- id:'
            var entries = rawEntries.Where(s => Regex.IsMatch(s, @"^\s*[A-Za-z0-9_-]+")).ToArray();
            Assert.IsTrue(entries.Length >= 1, "No vector entries found in dukpt_vectors.yaml");

            foreach (var e in entries)
            {
                // simple presence checks
                Assert.IsTrue(Regex.IsMatch(e, @"\bbdk:\s*[0-9A-Fa-f]+"), "BDK missing or invalid hex");
                Assert.IsTrue(Regex.IsMatch(e, @"\bksn:\s*[0-9A-Fa-f]+"), "KSN missing or invalid hex");
                Assert.IsTrue(Regex.IsMatch(e, @"\bexpected_ipek:\s*[0-9A-Fa-f]+"), "expected_ipek missing or invalid hex");
                Assert.IsTrue(Regex.IsMatch(e, @"\bsample_pin_block:\s*[0-9A-Fa-f]+"), "sample_pin_block missing or invalid hex");
                Assert.IsTrue(Regex.IsMatch(e, @"\bexpected_clear_pin:\s*[0-9A-Fa-f]*"), "expected_clear_pin missing");

                // validate lengths for common fields (basic)
                var bdk = Regex.Match(e, @"\bbdk:\s*([0-9A-Fa-f]+)").Groups[1].Value;
                Assert.IsTrue(bdk.Length == 32 || bdk.Length == 48, "BDK should be 16 or 24 bytes hex (32 or 48 hex chars)");

                var ksn = Regex.Match(e, @"\bksn:\s*([0-9A-Fa-f]+)").Groups[1].Value;
                Assert.IsTrue(ksn.Length >= 10 && ksn.Length <= 40, "KSN length looks unusual");
            }
        }
    }
}
