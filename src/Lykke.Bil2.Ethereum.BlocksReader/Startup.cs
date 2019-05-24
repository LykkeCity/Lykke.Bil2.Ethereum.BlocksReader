using JetBrains.Annotations;
using Lykke.Bil2.Ethereum.BlocksReader.Interfaces;
using Lykke.Bil2.Ethereum.BlocksReader.Services;
using Lykke.Bil2.Ethereum.BlocksReader.Settings;
using Lykke.Bil2.Sdk.BlocksReader;
using Lykke.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Lykke.Bil2.Ethereum.BlocksReader
{
    [UsedImplicitly]
    public class Startup
    {
        private const string IntegrationName = "Ethereum";

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildBlockchainBlocksReaderServiceProvider<AppSettings>(options =>
            {
                options.UseTransferAmountTransactionsModel();
                options.IntegrationName = IntegrationName;

                // Register required service implementations:

                options.BlockReaderFactory = ctx =>
                    new BlockReader
                    (
                        ctx.Services.GetRequiredService<IRpcBlocksReader>()
                    );

                // To access settings for any purpose,
                // usually, to register additional services like blockchain client,
                // uncomment code below:
                //
                options.UseSettings = (serviceCollection, settings) =>
                {
                    string ethereumUrl = settings.CurrentValue.NodeUrl;

                    serviceCollection.AddHttpClient();

                    serviceCollection.AddTransient<IDebugDecorator, DebugDecorator>((sp) =>
                    {
                        return new DebugDecorator(sp.GetRequiredService<IHttpClientFactory>(), ethereumUrl);
                    });

                    serviceCollection.AddTransient<IErc20ContractIndexingService, Erc20ContractIndexingService>((sp) =>
                    {
                        return new Erc20ContractIndexingService(ethereumUrl);
                    });

                    serviceCollection.AddTransient<IRpcBlocksReader, RpcBlocksReader>((sp) =>
                    {
                        return new RpcBlocksReader(
                            ethereumUrl,
                            30,
                            100_000,
                            sp.GetRequiredService<IDebugDecorator>(),
                            sp.GetRequiredService<IErc20ContractIndexingService>());
                    });
                };
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseBlockchainBlocksReader(options =>
            {
                options.IntegrationName = IntegrationName;
            });
        }
    }
}
