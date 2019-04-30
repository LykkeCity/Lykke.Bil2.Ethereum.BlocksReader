using System.Collections.Generic;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.SharedDomain;

namespace Lykke.Bil2.Ethereum.BlocksReader.Models
{
    public class BlockContent
    {
        public IEnumerable<(Base58String rawTransaction, TransferAmountTransactionExecutedEvent transferAmount)> 
            TransferAmountTransactionExecutedEvents { get; set; }

        public IEnumerable<(Base58String rawTransaction, TransactionFailedEvent failedEvent)> 
            TransactionFailedEvents { get; set; }

        public IEnumerable<AddressHistoryModel> AddressHistory { get; set; }

        public BlockModel BlockModel { get; set; }

        public List<DeployedContractModel> DeployedContracts { get; set; }

        public List<InternalMessageModel> InternalMessages { get; set; }

        public List<TransactionModel> Transactions { get; set; }

        public List<Erc20TransferHistoryModel> Transfers { get; set; }

        public string RawBlock { get; set; }
    }
}
