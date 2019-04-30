using System.Numerics;

namespace Lykke.Bil2.Ethereum.BlocksReader.Models
{
    public class Erc20ContractModel
    {
        public string Address { get; set; }

        public string BlockHash { get; set; }

        public BigInteger BlockNumber { get; set; }

        public BigInteger BlockTimestamp { get; set; }

        public string DeployerAddress { get; set; }

        public uint? TokenDecimals { get; set; }

        public string TokenName { get; set; }

        public string TokenSymbol { get; set; }

        public BigInteger TokenTotalSupply { get; set; }

        public string TransactionHash { get; set; }
    }
}
