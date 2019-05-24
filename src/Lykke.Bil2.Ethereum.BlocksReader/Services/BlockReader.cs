using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Ethereum.BlocksReader.Extensions;
using Lykke.Bil2.Ethereum.BlocksReader.Interfaces;
using Lykke.Bil2.Sdk.BlocksReader.Services;
using Lykke.Bil2.SharedDomain;
using Lykke.Bil2.SharedDomain.Extensions;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Bil2.Ethereum.BlocksReader.Services
{
    public class BlockReader : IBlockReader
    {
        private readonly IRpcBlocksReader _rpcBlocksReader;

        public BlockReader(IRpcBlocksReader rpcBlocksReader)
        {
            _rpcBlocksReader = rpcBlocksReader;
        }

        public async Task ReadBlockAsync(long blockNumber, IBlockListener listener)
        {
            var block = await _rpcBlocksReader.ReadBlockAsync((BigInteger) blockNumber);

            if (block == null)
            {
                listener.HandleNotFoundBlock(new BlockNotFoundEvent(blockNumber));

                return;
            }

            listener.HandleRawBlock(
                block.RawBlock.ToBase64(),
                block.BlockHeaderReadEvent.BlockId);

            var transactionsListener = listener.StartBlockTransactionsHandling(block.BlockHeaderReadEvent);

            if (block.TransferAmountTransactionExecutedEvents != null)
            {
                foreach (var transferEvent in block.TransferAmountTransactionExecutedEvents)
                {
                    transactionsListener.HandleExecutedTransaction(transferEvent.transferAmount);
                    await transactionsListener.HandleRawTransactionAsync(transferEvent.rawTransaction, transferEvent.transferAmount.TransactionId);
                }
            }

            if (block.TransactionFailedEvents != null)
            {
                foreach (var failedEvent in block.TransactionFailedEvents)
                {
                    transactionsListener.HandleFailedTransaction(failedEvent.failedEvent);
                    await transactionsListener.HandleRawTransactionAsync(failedEvent.rawTransaction, failedEvent.failedEvent.TransactionId);
                }
            }
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
