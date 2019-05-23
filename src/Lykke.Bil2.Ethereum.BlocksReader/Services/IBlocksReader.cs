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
            _retryPolicy = Policy.Handle<Exception>().Retry(3);
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

            string rawBlock = Newtonsoft.Json.JsonConvert.SerializeObject(block);
            var blockHash = block.BlockHash;
            var blockModel = new BlockModel
            {
                TransactionsCount = block.Transactions.Length,
                BlockHash = blockHash,
                Difficulty = block.Difficulty,
                ExtraData = block.ExtraData,
                GasLimit = block.GasLimit,
                GasUsed = block.GasUsed,
                LogsBloom = block.LogsBloom,
                Miner = block.Miner,
                Nonce = block.Nonce,
                Number = block.Number,
                ParentHash = block.ParentHash,
                ReceiptsRoot = block.ReceiptsRoot,
                Sha3Uncles = block.Sha3Uncles,
                Size = block.Size,
                StateRoot = block.StateRoot,
                Timestamp = block.Timestamp,
                TotalDifficulty = block.TotalDifficulty,
                TransactionsRoot = block.TransactionsRoot
            };

            #endregion

            #region Transactions

            var internalMessages = new List<InternalMessageModel>();
            var blockTransactions = new Dictionary<string, TransactionModel>(block.Transactions.Length);

            foreach (var transaction in block.Transactions)
            {
                var transactionReciept =
                    await _ethClient.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.TransactionHash);

                TraceResultModel traceResult = null;

                await _retryPolicy.ExecuteAsync(async () =>
                {
                    traceResult = await _debug.TraceTransactionAsync(transaction.TransactionHash);

                    if (traceResult != null && !traceResult.HasError && traceResult.Transfers != null)
                    {
                        internalMessages.AddRange
                        (
                            traceResult.Transfers.Select(x => new InternalMessageModel
                            {
                                BlockNumber = block.Number.Value,
                                Depth = x.Depth,
                                FromAddress = x.FromAddress,
                                MessageIndex = x.MessageIndex,
                                ToAddress = x.ToAddress,
                                TransactionHash = x.TransactionHash,
                                Value = x.Value,
                                Type = (InternalMessageModelType)x.Type,
                                BlockTimestamp = blockModel.Timestamp
                            })
                        );
                    }
                });

                var transactionModel = new TransactionModel
                {
                    BlockTimestamp = block.Timestamp,
                    BlockHash = transaction.BlockHash,
                    BlockNumber = transaction.BlockNumber,
                    From = transaction.From,
                    Gas = transaction.Gas,
                    GasPrice = transaction.GasPrice,
                    Input = transaction.Input,
                    Nonce = transaction.Nonce,
                    To = transaction.To,
                    TransactionHash = transaction.TransactionHash,
                    TransactionIndex = (int)transaction.TransactionIndex.Value,
                    Value = transaction.Value,
                    GasUsed = transactionReciept.GasUsed.Value,
                    ContractAddress = transactionReciept.ContractAddress,
                    HasError = (transactionReciept.HasErrors() ?? false)
                               || (traceResult?.HasError ?? false)
                };

                blockTransactions[transaction.TransactionHash] = transactionModel;
            }

            var addressHistory = ExtractAddressHistory(internalMessages, blockTransactions.Values);

            #endregion

            #region Contracts

            var deployedContracts = new List<DeployedContractModel>();

            foreach (var transaction in blockTransactions.Select(x => x.Value).Where(x => x.ContractAddress != null))
            {
                deployedContracts.Add(new DeployedContractModel
                {
                    Address = transaction.ContractAddress,
                    BlockHash = blockHash,
                    BlockNumber = block.Number.Value.ToString(),
                    BlockTimestamp = block.Timestamp.Value.ToString(),
                    DeployerAddress = transaction.From,
                    TransactionHash = transaction.TransactionHash
                });
            }

            foreach (var message in internalMessages.Where(x => x.Type == InternalMessageModelType.CREATION))
            {
                deployedContracts.Add(new DeployedContractModel
                {
                    Address = message.ToAddress,
                    BlockHash = blockHash,
                    BlockNumber = block.Number.Value.ToString(),
                    BlockTimestamp = block.Timestamp.Value.ToString(),
                    DeployerAddress = message.FromAddress,
                    TransactionHash = message.TransactionHash
                });
            }

            // Select contracts with distinct addresses
            deployedContracts = deployedContracts
                .GroupBy(x => x.Address)
                .Select(x => x.First())
                .ToList();

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
                .Select(x =>
                {
                    string trHash = x.TransactionHash;
                    TransactionModel transaction = null;
                    blockTransactions.TryGetValue(trHash, out transaction);

                    return new Erc20TransferHistoryModel
                    {
                        BlockHash = x.BlockHash,
                        BlockNumber = (ulong)x.BlockNumber.Value,
                        BlockTimestamp = (ulong)block.Timestamp.Value,
                        ContractAddress = x.Address,
                        From = x.GetAddressFromTopic(1),
                        LogIndex = (uint)x.LogIndex.Value,
                        To = x.GetAddressFromTopic(2),
                        TransactionHash = trHash,
                        TransactionIndex = (uint)x.TransactionIndex.Value,
                        TransferAmount = x.Data.HexToBigInteger(false),
                        GasUsed = transaction?.GasUsed ?? 0,
                        GasPrice = transaction?.GasPrice ?? 0
                    };
                })
                .ToList();

            #endregion

            var internalMessagesLookup = internalMessages.ToLookup(x => x.TransactionHash);
            var erc20TransferHistoryLookup = erc20TransferHistory.ToLookup(x => x.TransactionHash);
            var transactions = blockTransactions?.Select(x => x.Value).ToList();
            var extractedEvents = await ExtractAddressHistoryAsDictAsync(
                blockModel,
                transactions,
                internalMessagesLookup,
                erc20TransferHistoryLookup);

            return new BlockContent
            {
                TransferAmountTransactionExecutedEvents = extractedEvents.TransferEvents,
                TransactionFailedEvents = extractedEvents.FailedEvents,
                AddressHistory = addressHistory,
                BlockModel = blockModel,
                DeployedContracts = deployedContracts,
                InternalMessages = internalMessages,
                Transactions = blockTransactions?.Select(x => x.Value).ToList(),
                Transfers = erc20TransferHistory,
                RawBlock = rawBlock
            };
        }

        private static IEnumerable<AddressHistoryModel> ExtractAddressHistory(
            List<InternalMessageModel> internalMessages,
            IEnumerable<TransactionModel> blockTransactions)
        {
            var trHashIndexDictionary = new Dictionary<string, int>();
            var history = blockTransactions.Select(transaction =>
            {
                var index = (int)transaction.TransactionIndex;
                var trHash = transaction.TransactionHash;

                trHashIndexDictionary[trHash] = index;

                return new AddressHistoryModel
                {
                    MessageIndex = -1,
                    TransactionIndex = (int)transaction.TransactionIndex,
                    BlockNumber = (ulong)transaction.BlockNumber,
                    BlockTimestamp = (uint)transaction.BlockTimestamp,
                    From = transaction.From,
                    HasError = transaction.HasError,
                    To = transaction.To,
                    TransactionHash = transaction.TransactionHash,
                    Value = transaction.Value,
                    GasUsed = transaction.GasUsed,
                    GasPrice = transaction.GasPrice
                };
            });

            history = history.Concat(internalMessages.Select(message => new AddressHistoryModel
            {
                MessageIndex = message.MessageIndex,
                TransactionIndex = trHashIndexDictionary[message.TransactionHash],
                TransactionHash = message.TransactionHash,
                To = message.ToAddress,
                HasError = false,
                From = message.FromAddress,
                BlockNumber = (ulong)message.BlockNumber,
                BlockTimestamp = (uint)message.BlockTimestamp,
                Value = message.Value,
                GasPrice = 0,
                GasUsed = 0
            }));

            return history;
        }

        private async Task<(IEnumerable<(Base64String rawTransaction, TransferAmountExecutedTransaction transaction)> TransferEvents,
            IEnumerable<(Base64String rawTransaction, FailedTransaction failedTransaction)> FailedEvents)> ExtractAddressHistoryAsDictAsync(
            BlockModel blockModel,
            IEnumerable<TransactionModel> blockTransactions,
            ILookup<string, InternalMessageModel> internalMessagesDict,
            ILookup<string, Erc20TransferHistoryModel> erc20TransferHistoryDict)
        {
            BlockId blockId = blockModel.BlockHash;
            var blockTransactionsArray = blockTransactions?.ToArray() ?? new TransactionModel[0];
            List<(Base64String rawTransaction, TransferAmountExecutedTransaction transaction)> transferEvents =
                new List<(Base64String rawTransaction, TransferAmountExecutedTransaction transaction)>(blockTransactionsArray.Length);
            List<(Base64String rawTransaction, FailedTransaction failedTransaction)> failedEvents = 
                new List<(Base64String rawTransaction, FailedTransaction failedTransaction)>(blockTransactionsArray.Length);

            foreach (var transaction in blockTransactionsArray)
            {
                var transactionHash = transaction.TransactionHash;
                var transactionId = new TransactionId(transactionHash);
                var calculatedGasCost = transaction.GasPrice * transaction.GasUsed;
                var fees = new Fee[]
                {
                    new Fee(Assets.Assets.EthAsset, UMoney.Create(calculatedGasCost, 18))
                };

                //no error case
                if (!transaction.HasError)
                {
                    List<BalanceChange> balanceChanges = new List<BalanceChange>();

                    var senderBalanceChange = -(transaction.Value + calculatedGasCost);
                    var receiverBalanceChange = transaction.Value;
                    balanceChanges.Add(new BalanceChange(
                        transactionHash,
                        Assets.Assets.EthAsset,
                        Money.Create(-senderBalanceChange, 18),
                        new Address(transaction.From)));

                    balanceChanges.Add(new BalanceChange(
                        transactionHash,
                        Assets.Assets.EthAsset,
                        Money.Create(receiverBalanceChange, 18),
                        new Address(transaction.From)));


                    if (internalMessagesDict != null)
                    {
                        var internalMessages = internalMessagesDict[transactionHash];

                        foreach (var message in internalMessages)
                        {
                            balanceChanges.Add(new BalanceChange(
                                $"{transactionHash}:{message.MessageIndex}",
                                Assets.Assets.EthAsset,
                                Money.Create(-message.Value, 18),
                                new Address(message.FromAddress)));

                            balanceChanges.Add(new BalanceChange(
                                $"{transactionHash}:{message.MessageIndex}",
                                Assets.Assets.EthAsset,
                                Money.Create(message.Value, 18),
                                new Address(message.ToAddress)));
                        }
                    }

                    if (erc20TransferHistoryDict != null)
                    {
                        var erc20TransferHistory = erc20TransferHistoryDict[transactionHash];

                        foreach (var erc20Transfer in erc20TransferHistory)
                        {
                            var assetInfo = await _indexByAddress.GetItem(erc20Transfer.ContractAddress,
                                async (address) => await _erc20ContractIndexingService.GetContractAssetInfoAsync(address));

                            balanceChanges.Add(new BalanceChange(
                                $"{transactionHash}:log:{erc20Transfer.LogIndex}",
                                assetInfo.Asset,
                                Money.Create(-erc20Transfer.TransferAmount, assetInfo.Scale),
                                new Address(erc20Transfer.From)));

                            balanceChanges.Add(new BalanceChange(
                                $"{transactionHash}:log:{erc20Transfer.LogIndex}",
                                assetInfo.Asset,
                                Money.Create(erc20Transfer.TransferAmount, assetInfo.Scale),
                                new Address(erc20Transfer.To)));
                        }
                    }

                    var transferEvent = new TransferAmountExecutedTransaction(
                        transaction.TransactionIndex,
                        transactionId,
                        balanceChanges,
                        fees);

                    transferEvents.Add((Base64String.Encode(""), transferEvent));
                }
                // failed transaction
                else
                {
                    var failedEvent = new FailedTransaction(
                        transaction.TransactionIndex,
                        transactionId,
                        TransactionBroadcastingError.FeeTooLow,//TODO: Pick up right status for failed tx
                        "",
                        fees);

                    failedEvents.Add((Base64String.Encode(""), failedEvent));
                }
            }

            return (transferEvents, failedEvents);
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
