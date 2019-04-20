using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Sdk;

namespace Lykke.Bil2.Ethereum.BlocksReader
{
    [UsedImplicitly]
    class Program
    {
        static async Task Main(string[] args)
        {
#if DEBUG
            await LykkeStarter.Start<Startup>(true, 5002);
#else
            await LykkeStarter.Start<Startup>(false);
#endif
        }
    }
}
