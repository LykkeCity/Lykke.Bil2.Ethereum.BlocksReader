using JetBrains.Annotations;
using Lykke.Bil2.Sdk.BlocksReader.Settings;

namespace Lykke.Bil2.Ethereum.BlocksReader.Settings
{
    /// <summary>
    /// Specific blockchain settings
    /// </summary>
    [UsedImplicitly]
    public class AppSettings : BaseBlocksReaderSettings<DbSettings, RabbitMqSettings>
    {
        public string NodeUrl { get; set; }
    }
}
