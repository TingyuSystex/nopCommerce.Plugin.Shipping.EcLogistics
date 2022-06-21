using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.EcLogistics
{
    /// <summary>
    /// Represents settings of the PayPal Standard payment plugin
    /// </summary>
    public class EcLogisticsSettings : ISettings
    {
        public string MerchantId { get; set; }

        public string HashKey { get; set; }

        public string HashIV { get; set; }

        public string? PlatformId { get; set; }

    }
}
