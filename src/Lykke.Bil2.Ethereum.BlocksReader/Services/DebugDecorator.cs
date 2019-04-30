using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using Lykke.Bil2.Ethereum.BlocksReader.Models.DebugModels;

namespace Lykke.Bil2.Ethereum.BlocksReader.Services
{
    public interface IDebugDecorator
    {
        Task<TraceResultModel> TraceTransactionAsync(string transactionHash);
    }

    public class DebugDecorator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _ethereumUrl;

        public DebugDecorator(IHttpClientFactory httpClientFactory, string ethereumUrl)
        {
            _ethereumUrl = ethereumUrl;
            var pipeline = new JsonApiTypeHandler()
            {
                InnerHandler = new HttpClientHandler()
            };

            _httpClientFactory = httpClientFactory;
        }

        //ropsten transactions
        //0x755babb47619dc781c3ad723946b41d12c8f8c01d677c8b5ae36630a0dd91f8b - with internal contract creation
        //0x1f8d164fef4efb88160d55d59eaab311e0e61b47eaf7364b7dc3108aceb6aa30 - with 3 internal transfers
        //0x0ddcd11d25e196d591959a98a39c45f138ab44f2006e347d668d09ca70dfdc69 - with error
        public async Task<TraceResultModel> TraceTransactionAsync(string transactionHash)
        {
            /*
             {
               "method": "trace_transaction",
               "params": [
                 "0x45699687953024dc33de48fad5cbbfd9c0b1e9e578e3452c9be33780fa5c5e0d"
               ],
               "id": 1,
               "jsonrpc": "2.0"
             }
             */
            var guid = Guid.NewGuid().ToString();
            CustomRpcRequest request = new CustomRpcRequest()
            {
                Id = guid,
                Method = "trace_transaction",//"debug_traceTransaction",
                Params = new List<object>()
                {
                    transactionHash
                }
            };

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(request);
            byte[] byteArray = Encoding.ASCII.GetBytes(serialized);
            MemoryStream stream = new MemoryStream(byteArray);
            ParityTransactionTraceResponse traceResponse = null;

            using (var parityClient = _httpClientFactory.CreateClient("parityClient"))
            {
                HttpResponseMessage response = await parityClient.PostAsync(_ethereumUrl, new StreamContent(stream));
                var responseString = await response.Content.ReadAsStringAsync();
                traceResponse = (ParityTransactionTraceResponse)
                    Newtonsoft.Json.JsonConvert.DeserializeObject(responseString, typeof(ParityTransactionTraceResponse));

                if (traceResponse?.TransactionTrace == null)
                {
                    //Means there is no trace for transaction hash;
                    return null;
                }
            }

            string errorMessage = traceResponse.TransactionTrace.FirstOrDefault()?.Error;
            IEnumerable<TransferValueModel> transfers = null;
            bool hasError = !string.IsNullOrEmpty(errorMessage);

            if (!hasError)
            {
                transfers = traceResponse.TransactionTrace.Skip(1)
                   .Where(y =>
                   {
                       var messageType = ExtractMessageType(y);
                       return messageType != TransferValueModelType.SUICIDE && !(y.Action.Value == BigInteger.Zero &&
                                    messageType == TransferValueModelType.TRANSFER);
                   })
                   .Select((x, counter) =>
                   {
                       return new TransferValueModel()
                       {
                           MessageIndex = counter,
                           Depth = 0,
                           FromAddress = x.Action.From,
                           ToAddress = x.Action.To,
                           TransactionHash = x.TransactionHash,
                           Type = ExtractMessageType(x),
                           Value = x.Action.Value,
                       };
                   });
            }

            return new TraceResultModel()
            {
                HasError = hasError,
                Transfers = transfers
            };
        }

        private TransferValueModelType ExtractMessageType(ParityTransactionTrace transaction)
        {
            switch (transaction.Action.CallType)
            {
                case "delegatecall":
                case "call":
                    return TransferValueModelType.TRANSFER;
                case "suicide":
                    return TransferValueModelType.SUICIDE;

                default:
                    break;
            }

            if (transaction.Type == "suicide")
                return TransferValueModelType.SUICIDE;

            if (!string.IsNullOrEmpty(transaction.Result.Address) && transaction.Type == "create")
            {
                transaction.Action.To = string.IsNullOrEmpty(transaction.Action.To) ?
                    transaction.Result.Address : transaction.Action.To;
                return TransferValueModelType.CREATION;
            }

            return TransferValueModelType.TRANSACTION;
        }
    }

    internal class JsonApiTypeHandler : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            //request.Headers.Add("Content-Type", "application/json");

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
