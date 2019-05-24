using Lykke.Bil2.Client.BlocksReader;
using Lykke.Bil2.Client.BlocksReader.Services;
using Lykke.Bil2.RabbitMq.Subscription.MessageFilters;
using Lykke.Common;
using Lykke.Common.Log;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
//            var services = new ServiceCollection();

//            services.AddBlocksReaderClient(options =>
//            {
//                var settings = _settings.Bil2IndexerJob.RabbitMq;

//                options.RabbitMqConnString = settings.ConnString;
//#if DEBUG
//                options.RabbitVhost = settings.Vhost == "/"
//                    ? null
//                    : settings.Vhost ?? AppEnvironment.EnvInfo;
//#else
//                options.RabbitVhost = settings.Vhost;
//#endif
//                options.MessageConsumersCount = settings.MessageConsumersCount;
//                options.MessageProcessorsCount = settings.MessageProcessorsCount;
//                options.MaxFirstLevelRetryCount = settings.MaxFirstLevelRetryCount;
//                options.MaxFirstLevelRetryMessageAge = settings.MaxFirstLevelRetryMessageAge;
//                options.DefaultFirstLevelRetryTimeout = settings.DefaultFirstLevelRetryTimeout;
//                options.FirstLevelRetryQueueCapacity = settings.FirstLevelRetryQueueCapacity;
//                options.ProcessingQueueCapacity = settings.ProcessingQueueCapacity;

//                options.BlockEventsHandlerFactory = c => c.GetRequiredService<IBlockEventsHandler>();

//                foreach (var integration in _settings.Bil2IndexerJob.BlockchainIntegrations)
//                {
//                    options.AddIntegration(integration.Type);
//                }
//            });
        }
    }
}
