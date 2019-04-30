using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Newtonsoft.Json;

namespace Lykke.Bil2.Ethereum.BlocksReader.Models.DebugModels
{
    public class CustomConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //explicitly specify the concrete type we want to create
            return serializer.Deserialize<T>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
        }
    }

    #region GETH

    [DataContract]
    public class TransactionTrace
    {
        [DataMember(Name = "gas")]
        [JsonConverter(typeof(CustomConverter<BigInteger>))]
        public BigInteger Gas { get; set; }

        [DataMember(Name = "returnValue")]
        public string ReturnValue { get; set; }

        [DataMember(Name = "structLogs")]
        public IEnumerable<StructLogItem> StructLogs { get; set; }
    }

    [DataContract]
    public class TransactionTraceResponse
    {
        [DataMember(Name = "result")]
        public TransactionTrace TransactionTrace { get; set; }

    }

    [DataContract]
    public class StructLogItem
    {
        [DataMember(Name = "op")]
        public string Opcode { get; set; }
        [DataMember(Name = "gas")]
        [JsonConverter(typeof(CustomConverter<BigInteger>))]
        public BigInteger Gas { get; set; }
        [DataMember(Name = "gasCost")]
        [JsonConverter(typeof(CustomConverter<BigInteger>))]
        public BigInteger GasCost { get; set; }
        [DataMember(Name = "depth")]
        public int Depth { get; set; }
        [DataMember(Name = "error")]
        public object Error { get; set; }
        [DataMember(Name = "stack")]
        public List<string> Stack { get; set; }
        //[DataMember(Name = "memory")]
        //public IEnumerable<string>  Memory { get; set; }
        //[DataMember(Name = "storage")]
        //public Dictionary<string, string> Storage { get; set; }
    }

    #endregion

    #region PARITY

    [DataContract]
    public class ParityTransactionTrace
    {
        [DataMember(Name = "action")]
        public ParityTransactionAction Action { get; set; }

        [DataMember(Name = "subtraces")]
        public int Subtraces { get; set; }

        [DataMember(Name = "transactionHash")]
        public string TransactionHash { get; set; }

        [DataMember(Name = "transactionPosition")]
        public int TransactionPosition { get; set; }

        [DataMember(Name = "traceAddresses")]
        public int[] TraceAddresses { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "blockNumber")]
        public ulong BlockNumber { get; set; }

        [DataMember(Name = "blockHash")]
        public string BlockHash { get; set; }

        [DataMember(Name = "result")]
        public ParityTransactionResult Result { get; set; }
    }

    [DataContract]
    public class ParityTransactionAction
    {
        [DataMember(Name = "callType")]
        public string CallType { get; set; }

        [DataMember(Name = "from")]
        public string From { get; set; }

        //[DataMember(Name = "input")]
        //public string Input { get; set; }

        [DataMember(Name = "to")]
        public string To { get; set; }

        [JsonConverter(typeof(CustomConverter<HexBigInteger>))]
        [DataMember(Name = "gas")]
        public HexBigInteger Gas { get; set; }

        [JsonConverter(typeof(CustomConverter<HexBigInteger>))]
        [DataMember(Name = "value")]
        public HexBigInteger Value { get; set; }
    }

    [DataContract]
    public class ParityTransactionResult
    {
        [JsonConverter(typeof(CustomConverter<HexBigInteger>))]
        [DataMember(Name = "gasUsed")]
        public HexBigInteger GasUsed { get; set; }

        [DataMember(Name = "output")]
        public string Output { get; set; }

        [DataMember(Name = "address")]
        public string Address { get; set; }
    }

    [DataContract]
    public class ParityTransactionTraceResponse
    {
        [DataMember(Name = "result")]
        public IEnumerable<ParityTransactionTrace> TransactionTrace { get; set; }

    }

    #endregion
}
