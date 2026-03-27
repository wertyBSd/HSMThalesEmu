using System;
using System.IO;

namespace ThalesCore.Storage
{
    public static class StoreFactory
    {
        // Environment variable: THALES_STORE = json|sqlite
        // THALES_STORE_PATH = path for storage (dir for json, file for sqlite)
        public static IKeyStore CreateFromEnvironment()
        {
            var type = Environment.GetEnvironmentVariable("THALES_STORE")?.ToLowerInvariant() ?? "json";
            var path = Environment.GetEnvironmentVariable("THALES_STORE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "thales_store");
            if (type == "sqlite")
            {
                // if path is a directory, use default filename
                if (Directory.Exists(path)) path = Path.Combine(path, "thales_store.db");
                if (Path.GetExtension(path) == string.Empty) path = Path.Combine(path, "thales_store.db");
                return new SqliteKeyStore(path);
            }
            Directory.CreateDirectory(path);
            return new JsonKeyStore(path);
        }
    }
}
