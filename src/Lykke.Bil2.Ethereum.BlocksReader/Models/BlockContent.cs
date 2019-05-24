using System.Collections.Generic;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.SharedDomain;

namespace Lykke.Bil2.Ethereum.BlocksReader.Models
{
    public class BlockContent
    {
        public IEnumerable<(Base64String rawTransaction, TransferAmountExecutedTransaction transferAmount)> 
            TransferAmountTransactionExecutedEvents { get; set; }

        public IEnumerable<(Base64String rawTransaction, FailedTransaction failedEvent)> 
            TransactionFailedEvents { get; set; }

        public BlockHeaderReadEvent BlockHeaderReadEvent { get; set; }

        public string RawBlock { get; set; }
    }
}
