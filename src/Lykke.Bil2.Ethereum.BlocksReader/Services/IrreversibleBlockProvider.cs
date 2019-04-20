using System.Threading.Tasks;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Sdk.BlocksReader.Services;

namespace Lykke.Bil2.Ethereum.BlocksReader.Services
{
    public class IrreversibleBlockProvider : IIrreversibleBlockProvider
    {
        public IrreversibleBlockProvider(/* TODO: Provide specific settings and dependencies, if necessary */)
        {
        }

        public async Task<LastIrreversibleBlockUpdatedEvent> GetLastAsync()
        {
            // TODO: retrieve last irreversible block and return its number and hash
            //
            // For example:
            //
            // var lastIrreversibleBlock = ...;
            //
            // return new LastIrreversibleBlockUpdatedEvent(lastIrreversibleBlock.Number, lastIrreversibleBlock.Hash);


            throw new System.NotImplementedException();
        }
    }
}