using FluidCaching;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Ethereum.BlocksReader.Extensions;
using Lykke.Bil2.Ethereum.BlocksReader.Interfaces;
using Lykke.Bil2.Ethereum.BlocksReader.Models;
using Lykke.Bil2.Ethereum.BlocksReader.Models.DebugModels;
using Lykke.Bil2.SharedDomain;
using Lykke.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Filters;
using Nethereum.Web3;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Bil2.Ethereum.BlocksReader.Services
{
    public class RpcBlocksReader : IRpcBlocksReader
    {
        private readonly Web3 _ethClient;
        private readonly IDebugDecorator _debug;
        private readonly RetryPolicy _retryPolicy;
        private readonly int _confirmationBlocks;
        private readonly FluidCache<AssetInfo> _cache;
        private readonly IIndex<string, AssetInfo> _indexByAddress;
        private readonly IErc20ContractIndexingService _erc20ContractIndexingService;

        public RpcBlocksReader(
            string url,
            int confirmationBlocks,
            int cacheCapacity,
            IDebugDecorator debug,
            IErc20ContractIndexingService erc20ContractIndexingService)
        {
            _retryPolicy = Policy.Handle<Exception>().RetryAsync(3);
            _ethClient = new Web3(url);
            _debug = debug;
            _confirmationBlocks = confirmationBlocks;
            _cache = new FluidCache<AssetInfo>(cacheCapacity, TimeSpan.Zero, TimeSpan.MaxValue, () => DateTime.UtcNow);
            _indexByAddress = _cache.AddIndex
            (
                "byAddress",
                x => (x.Asset.Address.Value)
            );
            _erc20ContractIndexingService = erc20ContractIndexingService;
        }

        public async Task<(long lastIrreversibleBlockNumber, BlockId blockId)>
            GetLastIrreversibleBlockAsync()
        {
            var bestBlock = await _ethClient.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            ulong irreversibleBlockNumber = (ulong)(bestBlock.Value - _confirmationBlocks);
            var irreversibleBlock = await _ethClient
                .Eth
                .Blocks
                .GetBlockWithTransactionsHashesByNumber.SendRequestAsync(new BlockParameter(irreversibleBlockNumber));

            return ((long)irreversibleBlockNumber, new BlockId(irreversibleBlock.BlockHash));
        }

        public async Task<BlockContent> ReadBlockAsync(BigInteger blockHeight)
        {
            var block = await _ethClient.Eth.Blocks
                .GetBlockWithTransactionsByNumber
                .SendRequestAsync(new HexBigInteger(blockHeight));

            var logs = new EthGetLogs(_ethClient.Client);

            #region Block

            if (block == null)
                return null;

            List<(Base64String rawTransaction, TransferAmountExecutedTransaction transaction)> transferTransactions =
                new List<(Base64String rawTransaction, TransferAmountExecutedTransaction transaction)>(block.Transactions.Length);
            List<(Base64String rawTransaction, FailedTransaction failedTransaction)> failedTransactions =
                new List<(Base64String rawTransaction, FailedTransaction failedTransaction)>(block.Transactions.Length);

            string rawBlock = Newtonsoft.Json.JsonConvert.SerializeObject(block);
            var blockId = new BlockId(block.BlockHash);
            var previousBlockId = new BlockId(block.ParentHash);
            var blockTime = UnixTimeStampToDateTime((double)block.Timestamp.Value);
            var blockHeaderReadEvent = new BlockHeaderReadEvent(
                (long)block.Number.Value,
                blockId,
                blockTime,
                (int)block.Size.Value,
                block.Transactions.Length,
                previousBlockId
                );

            #endregion

            #region Transfers

            var blockNumber = (ulong)blockHeight;
            var filter = new NewFilterInput
            {
                FromBlock = new BlockParameter(blockNumber),
                ToBlock = new BlockParameter(blockNumber),
                Topics = new object[]
                {
                    "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef"
                }
            };

            var transferLogs = await logs.SendRequestAsync(filter);
            var erc20TransferHistory = transferLogs
                .Where(x => x.Topics.Length == 3)
                .ToLookup(x => x.TransactionHash);

            #endregion

            #region Transactions

            var transactionIndex = 0;
            foreach (var transaction in block.Transactions)
            {
                TransactionReceipt transactionReciept = null;

                await _retryPolicy.ExecuteAsync(async () =>
                {
                    transactionReciept = await _ethClient.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.TransactionHash);
                });

                if (transactionReciept == null)
                {
                    throw new Exception($"Block is not valid {blockHeight}");
                }

                #region InternalMessages

                TraceResultModel traceResult = null;

                await _retryPolicy.ExecuteAsync(async () =>
                {
                    traceResult = await _debug.TraceTransactionAsync(transaction.TransactionHash);
                });

                var hasError = (transactionReciept.HasErrors() ?? false) || (traceResult?.HasError ?? false);
                var transactionHash = transaction.TransactionHash;
                var transactionId = new TransactionId(transactionHash);
                var calculatedGasCost = transaction.GasPrice.Value * transactionReciept.GasUsed.Value;
                var fees = new Fee[]
                {
                    new Fee(Assets.Assets.EthAsset, new UMoney(calculatedGasCost, 18))
                };

                #endregion

                //no error case
                if (!hasError)
                {
                    List<BalanceChange> balanceChanges = new List<BalanceChange>();

                    var senderBalanceChange = -(transaction.Value + calculatedGasCost);
                    var receiverBalanceChange = transaction.Value.Value;

                    balanceChanges.Add(new BalanceChange(
                        "value",
                        Assets.Assets.EthAsset,
                        new Money(-senderBalanceChange, 18),
                        new Address(transaction.From)));

                    if (!string.IsNullOrEmpty(transaction.To))
                    {
                        balanceChanges.Add(new BalanceChange(
                            "value",
                            Assets.Assets.EthAsset,
                            new Money(receiverBalanceChange, 18),
                            new Address(transaction.To)));
                    }

                    if (traceResult != null && !traceResult.HasError && traceResult.Transfers != null)
                    {
                        foreach (var message in traceResult.Transfers)
                        {
                            balanceChanges.Add(new BalanceChange(
                                $"{message.MessageIndex}",
                                Assets.Assets.EthAsset,
                                new Money(-message.Value, 18),
                                new Address(message.FromAddress)));

                            balanceChanges.Add(new BalanceChange(
                                $"{transactionHash}:{message.MessageIndex}",
                                Assets.Assets.EthAsset,
                                new Money(message.Value, 18),
                                new Address(message.ToAddress)));
                        }
                    }

                    var erc20TransfersForTransaction = erc20TransferHistory[transactionHash].ToList();
                    if (erc20TransfersForTransaction.Any())
                    {
                        foreach (var erc20Transfer in erc20TransfersForTransaction)
                        {
                            var from = erc20Transfer.GetAddressFromTopic(1);
                            var to = erc20Transfer.GetAddressFromTopic(2);
                            string contractAddress = erc20Transfer.Address;
                            var transferAmount = erc20Transfer.Data.HexToBigInteger(false);
                            var assetInfo = await _indexByAddress.GetItem(contractAddress,
                                async (address) => await _erc20ContractIndexingService.GetContractAssetInfoAsync(address));

                            balanceChanges.Add(new BalanceChange(
                                $"log:{erc20Transfer.LogIndex.Value}",
                                assetInfo.Asset,
                                new Money(-transferAmount, assetInfo.Scale),
                                new Address(from)));

                            balanceChanges.Add(new BalanceChange(
                                $"log:{erc20Transfer.LogIndex}",
                                assetInfo.Asset,
                                new Money(transferAmount, assetInfo.Scale),
                                new Address(to)));
                        }
                    }

                    var transferEvent = new TransferAmountExecutedTransaction(
                        transactionIndex,
                        transactionId,
                        balanceChanges,
                        fees);

                    transferTransactions.Add((Base64String.Encode(""), transferEvent));
                }
                // failed transaction
                else
                {
                    var failedEvent = new FailedTransaction(
                        transactionIndex,
                        transactionId,
                        TransactionBroadcastingError.Unknown,
                        "transaction failed",
                        fees);

                    failedTransactions.Add((Base64String.Encode(""), failedEvent));
                }

                transactionIndex++;
            }

            #endregion

            return new BlockContent
            {
                TransferAmountTransactionExecutedEvents = transferTransactions,
                TransactionFailedEvents = failedTransactions,
                BlockHeaderReadEvent = blockHeaderReadEvent,
                RawBlock = rawBlock
            };
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
