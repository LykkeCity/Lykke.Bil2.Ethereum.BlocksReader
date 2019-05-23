using Lykke.Bil2.Ethereum.BlocksReader.Models;
using Lykke.Bil2.SharedDomain;
using Nethereum.Web3;
using System;
using System.Threading.Tasks;

namespace Lykke.Bil2.Ethereum.BlocksReader.Services
{
    public interface IErc20ContractIndexingService
    {
        Task<AssetInfo> GetContractAssetInfoAsync(string address);
    }

    public class Erc20ContractIndexingService : IErc20ContractIndexingService
    {
        private const string MetadataAbi = @"[{""constant"":true,""inputs"":[],""name"":""name"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""totalSupply"",""outputs"":[{""name"":""totalSupply"",""type"":""uint256""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""decimals"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""symbol"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""type"":""function""}]";
        private readonly IWeb3 _web3;

        public Erc20ContractIndexingService(string ethereumUrl)
        {
            _web3 = new Web3(ethereumUrl);
        }

        public async Task<AssetInfo> GetContractAssetInfoAsync(string address)
        {
            Asset asset = null;
            int scale = 0;
            string symbol = null;
            var ethereumContract = _web3.Eth.GetContract(MetadataAbi, address);

            var symbalCallResult = await TryCallFunction<string>(ethereumContract, "symbol");
            symbol = symbalCallResult.Item2;

            if (!string.IsNullOrEmpty(symbol))
            {
                asset = new Asset(new AssetId(symbol), new AssetAddress(address));
            }
            else
            {
                asset = new Asset(new AssetId(address), new AssetAddress(address));
            }

            var decimalsCallResult = await TryCallFunction<uint>(ethereumContract, "decimals");
            if (decimalsCallResult.Item1)
            {
                scale = (int)decimalsCallResult.Item2;
            }

            var assetInfo = new AssetInfo(asset, scale);

            return assetInfo;
        }

        private static async Task<(bool, T)> TryCallFunction<T>(Nethereum.Contracts.Contract contract, string name)
        {
            var result = default(T);

            try
            {
                result = await contract.GetFunction(name).CallAsync<T>();

                return (true, result);
            }
            catch (Exception)
            {
                return (false, result);
            }
        }
    }
}
