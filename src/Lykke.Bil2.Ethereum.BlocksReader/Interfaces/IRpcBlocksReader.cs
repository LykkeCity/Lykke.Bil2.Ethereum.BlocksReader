using Lykke.Bil2.Ethereum.BlocksReader.Models;
using Lykke.Bil2.SharedDomain;
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Bil2.Ethereum.BlocksReader.Interfaces
{
    public interface IRpcBlocksReader
    {
        Task<(long lastIrreversibleBlockNumber, BlockId blockId)>
            GetLastIrreversibleBlockAsync();

        Task<BlockContent> ReadBlockAsync(BigInteger blockHeight);
    }
}
