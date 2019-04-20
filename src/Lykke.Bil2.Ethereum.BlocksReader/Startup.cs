using JetBrains.Annotations;
using Lykke.Bil2.Ethereum.BlocksReader.Services;
using Lykke.Bil2.Ethereum.BlocksReader.Settings;
using Lykke.Bil2.Sdk.BlocksReader;
using Lykke.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

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
                options.IntegrationName = IntegrationName;

#if DEBUG
                options.RabbitVhost = AppEnvironment.EnvInfo;
#endif

                // Register required service implementations:

                options.BlockReaderFactory = ctx =>
                    new BlockReader
                    (
                        /* TODO: Provide specific settings and dependencies, if necessary */
                    );

                // Register irreversible block retrieving strategy

                options.AddIrreversibleBlockPulling(ctx =>
                    new IrreversibleBlockProvider
                    (
                        /* TODO: Provide specific settings and dependencies, if necessary */
                    ));

                // To access settings for any purpose,
                // usually, to register additional services like blockchain client,
                // uncomment code below:
                //
                // options.UseSettings = settings =>
                // {
                //     services.AddSingleton<IService>(new ServiceImpl(settings.CurrentValue.ServiceSettingValue));
                // };
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
