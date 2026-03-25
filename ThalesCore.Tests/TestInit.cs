using System;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace ThalesCore.Tests
{
    [SetUpFixture]
    public class TestInit
    {
        [OneTimeSetUp]
        public void AssemblyInit()
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
