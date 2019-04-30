﻿using System.Numerics;

namespace Lykke.Bil2.Ethereum.BlocksReader.Models
{
    public class Erc20TransferHistoryModel
    {
        public string BlockHash { get; set; }

        public ulong BlockNumber { get; set; }

        public ulong BlockTimestamp { get; set; }

        public string ContractAddress { get; set; }

        public string From { get; set; }

        public uint LogIndex { get; set; }

        public string To { get; set; }

        public string TransactionHash { get; set; }

        public uint TransactionIndex { get; set; }

        public BigInteger TransferAmount { get; set; }

        public BigInteger GasUsed { get; set; }

        public BigInteger GasPrice { get; set; }
    }
}
