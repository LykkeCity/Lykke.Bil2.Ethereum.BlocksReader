using System.Threading.Tasks;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Ethereum.BlocksReader.Interfaces;
using Lykke.Bil2.Sdk.BlocksReader.Services;

namespace Lykke.Bil2.Ethereum.BlocksReader.Services
{
    public class IrreversibleBlockProvider : IIrreversibleBlockProvider
    {
        private readonly IRpcBlocksReader _rpcBlocksReader;

        public IrreversibleBlockProvider(IRpcBlocksReader rpcBlocksReader)
        {
            _rpcBlocksReader = rpcBlocksReader;
        }

        public async Task<LastIrreversibleBlockUpdatedEvent> GetLastAsync()
        {
            var (number, blockId) = await _rpcBlocksReader.GetLastIrreversibleBlockAsync();

            return new LastIrreversibleBlockUpdatedEvent(number, blockId);
        }
    }
}
