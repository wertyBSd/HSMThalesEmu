using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThalesCore.Tests
{
    [TestClass]
    public static class TestInit
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            // Enable codepage encodings (e.g., 1252) used by the original code
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Ensure ThalesParameters.xml is available in the test directory
            string configPath = Path.Combine(AppContext.BaseDirectory, "ThalesParameters.xml");

            // Initialize core configuration and crypto (generates test LMKs if needed)
            var tm = new ThalesCore.ThalesMain();
            tm.StartUpWithoutTCP(configPath);
        }
    }
}
