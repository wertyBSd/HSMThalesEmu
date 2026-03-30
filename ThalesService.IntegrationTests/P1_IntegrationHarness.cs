using System;
using System.IO;
using NUnit.Framework;

namespace ThalesService.IntegrationTests
{
    public class P1_IntegrationHarness
    {
        [Test]
        public void P1_Smoke_Run()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            var dir = new DirectoryInfo(testDir);

            // Walk up until we find the repository root containing test_vectors
            DirectoryInfo root = dir;
            while (root != null && !Directory.Exists(Path.Combine(root.FullName, "test_vectors")))
            {
                root = root.Parent;
            }

            if (root == null)
            {
                Assert.Ignore("test_vectors directory not found in repository tree — skipping integration scaffold test.");
            }

            var keysPath = Path.Combine(root.FullName, "test_vectors", "keys.yaml");
            if (!File.Exists(keysPath))
            {
                Assert.Ignore($"keys.yaml not found at {keysPath} — populate test_vectors to run integration tests.");
            }

            // Minimal smoke assertion for integration harness scaffold
            Assert.Pass("Integration harness scaffold found test vectors: " + keysPath);
        }
    }
}