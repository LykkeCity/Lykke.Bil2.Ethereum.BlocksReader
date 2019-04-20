using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Sdk.BlocksReader.Services;
using Lykke.Bil2.Contract.Common;

namespace Lykke.Bil2.Ethereum.BlocksReader.Services
{
    public class BlockReader : IBlockReader
    {
        public BlockReader(/* TODO: Provide specific settings and dependencies, if necessary */)
        {
        }

        public Task ReadBlockAsync(long blockNumber, IBlockListener listener)
        {
            // TODO: Process block with specified number, emit header and transaction(s) events
            //
            // For example:
            //
            // var block = ...;
            //
            // var handleRawBlockTask = listener.HandleRawBlockAsync
            // (
            //     block.Raw.ToBase58(),
            //     block.Hash
            // );
            
            // for (int i = 0; i < block.Transactions.Length; i++)
            // {
            //     var tx = block.Transactions[i];
            //
            //     if (tx.State == "SUCCESS")
            //     {
            //
            //         // If blockchain uses amount transfer scheme (is JSON-based) then emit TransferAmountTransactionExecutedEvent
            //
            //         await listener.HandleExecutedTransactionAsync(
            //             tx.Raw.ToBase58(),
            //             new TransferAmountTransactionExecutedEvent(
            //                 block.Hash,
            //                 i,
            //                 tx.Hash,
            //                 tx.Actions.SelectMany(act => new []
            //                 {
            //                     new BalanceChange(act.ActionId, new Asset(act.Token.Id), Money.Negate(Money.Create(act.Amount, act.Token.Accuracy)), act.From);
            //                     new BalanceChange(act.ActionId, new Asset(act.Token.Id), Money.Create(act.Amount, act.Token.Accuracy), act.To);
            //                 }),
            //                 fee: null,
            //                 isIrreversible: true
            //             )
            //         );
            //
            //         // If blockchain uses coins transfer scheme (is UTXO-based) then emit TransferCoinsTransactionExecutedEvent
            //
            //         await listener.HandleExecutedTransactionAsync(
            //             tx.Raw.ToBase58(),
            //             new TransferCoinsTransactionExecutedEvent(
            //                 block.Hash,
            //                 i,
            //                 tx.Hash,
            //                 tx.Outputs.Select(vout => new ReceivedCoin(vout.Number, "BTC", Money.Create(vout.Amount, 8), vout.Address)),
            //                 tx.Inputs.Select(vin => new CoinReference(vin.Hash, vin.Number)),
            //                 new [] { new Fee(new Asset("BTC"), Money.Create(tx.Fee, 8)) },
            //                 isIrreversible: null
            //             )
            //         );
            //     }
            //
            //     if (tx.State == "FAILURE")
            //     {
            //         await listener.HandleFailedTransactionAsync(
            //             tx.Raw.ToBase58(),
            //             new TransactionFailedEvent(
            //                 block.Hash,
            //                 i,
            //                 tx.Hash,
            //                 TransactionBroadcastingError.RebuildRequired,
            //                 tx.Error,
            //                 fee: null
            //             )
            //         );
            //     }
            // }
            
            // Better to send block header in the end:
            //
            // await listener.HandleHeaderAsync(new BlockHeaderReadEvent(
            //     block.Number,
            //     block.Hash,
            //     block.Time,
            //     block.Raw.Length,
            //     block.Transactions.Count,
            //     block.Previous.Hash
            // ));
            //
            // await handleRawBlockTask;


            throw new System.NotImplementedException();
        }
    }
}