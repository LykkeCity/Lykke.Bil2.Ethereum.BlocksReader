using System.Numerics;

namespace Lykke.Bil2.Ethereum.BlocksReader.Models
{
    public enum InternalMessageModelType
    {
        CREATION,
        TRANSACTION,
        TRANSFER
    }

    public class InternalMessageModel
    {
        public string TransactionHash { get; set; }

        public BigInteger BlockNumber { get; set; }

        public string FromAddress { get; set; }

        public string ToAddress { get; set; }

        public int Depth { get; set;}

        public BigInteger Value { get; set; }

        public int MessageIndex { get; set; }

        public InternalMessageModelType Type { get; set; }

        public BigInteger BlockTimestamp { get; set; }
    }
}
