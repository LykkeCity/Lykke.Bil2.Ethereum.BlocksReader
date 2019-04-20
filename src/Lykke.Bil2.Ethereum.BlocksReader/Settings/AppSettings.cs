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
        // Implement specific blockchain settings, if necessary.
        // Mark sensitive data with SecureSettingsAttribute to prevent sensitive data leaks.
        //
        // For example:
        //
        // public string NodeUrl { get; set; }
        //
        // public string NodeRpcUsername { get; set; }
        //
        // [SecureSettings]
        // public string NodeRpcPassword { get; set; }
    }
}
