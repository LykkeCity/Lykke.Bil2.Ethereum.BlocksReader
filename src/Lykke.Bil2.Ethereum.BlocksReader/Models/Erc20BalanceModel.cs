using System.Numerics;

namespace Lykke.Bil2.Ethereum.BlocksReader.Models
{
    public class Erc20BalanceModel
    {
        public string AssetHolderAddress { get; set; }

        public BigInteger Balance { get; set; }

        public ulong BlockNumber { get; set; }
        
        public string ContractAddress { get; set; }
    }
}
