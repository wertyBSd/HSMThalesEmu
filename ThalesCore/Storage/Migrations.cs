using System.Threading.Tasks;

namespace ThalesCore.Storage
{
    public static class Migrations
    {
        public static async Task EnsureInitializedAsync(IKeyStore store)
        {
            await store.InitializeAsync();
        }
    }
}
