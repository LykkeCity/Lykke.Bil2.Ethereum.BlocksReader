using System;
using System.Numerics;
using System.Threading.Tasks;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Contract.Common.Extensions;
using Lykke.Bil2.Sdk.BlocksReader.Services;
using Lykke.Bil2.SharedDomain;
using Lykke.Numerics;

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
            // TODO: Process block with specified number, emit header and transaction(s) events
            //
            // For example:
            //
            var block = await _rpcBlocksReader.ReadBlockAsync((BigInteger)blockNumber);

            if (block == null)
                throw new Exception();

            var handleRawBlockTask = listener.HandleRawBlockAsync
            (
                /*block.Raw.ToBase58(),*/"".ToBase58(),
                block.BlockModel.BlockHash
            );

            for (int i = 0; i < block.Transactions.Length; i++)
            {
                var tx = block.Transactions[i];

                if (tx.State == "SUCCESS")
                {
                    // If blockchain uses amount transfer scheme (is JSON-based) then emit TransferAmountTransactionExecutedEvent

                    await listener.HandleExecutedTransactionAsync(
                        tx.Raw.ToBase58(),
                        new TransferAmountTransactionExecutedEvent(
                            block.Hash,
                            i,
                            tx.Hash,
                            tx.Actions.SelectMany(act => new[]
                            {
                                 new BalanceChange(act.ActionId, new Asset(act.Token.Id), Money.Negate(Money.Create(act.Amount, act.Token.Accuracy)), act.From);
                    new BalanceChange(act.ActionId, new Asset(act.Token.Id), Money.Create(act.Amount, act.Token.Accuracy), act.To);
                }),
                             fee: null,
                             isIrreversible: true
                         )
                     );
        }
            
                 if (tx.State == "FAILURE")
                 {
                     await listener.HandleFailedTransactionAsync(
                         tx.Raw.ToBase58(),
                         new TransactionFailedEvent(
                             block.Hash,
                             i,
                             tx.Hash,
                             TransactionBroadcastingError.RebuildRequired,
                             tx.Error,
                             fee: null
                         )
                     );
                 }
}

// Better to send block header in the end:

await listener.HandleHeaderAsync(new BlockHeaderReadEvent(
                (long)block.BlockModel.Number,
                block.BlockModel.BlockHash,
                UnixTimeStampToDateTime((double)block.BlockModel.Timestamp),
                0,
                block.Transactions.Count,
                block.BlockModel.ParentHash
            ));

            await handleRawBlockTask;
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
