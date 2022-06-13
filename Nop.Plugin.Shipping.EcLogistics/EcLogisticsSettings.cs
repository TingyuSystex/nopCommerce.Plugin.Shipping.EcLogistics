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

        
        public string UniMartSubTypeCode { get; set; }
        public string FamiSubTypeCode { get; set; }
        public string HiLifeSubTypeCode { get; set; }
        public string UniMartName { get; set; }
        public string FamiName { get; set; }
        public string HiLifeName { get; set; }
        public string UniMartDescription { get; set; }
        public string FamiDescription { get; set; }
        public string HiLifeDescription { get; set; }
        public decimal UniMartRate { get; set; }
        public decimal FamiRate { get; set; }
        public decimal HiLifeRate { get; set; }
        public int UniMartDisplayOrder { get; set; }
        public int FamiDisplayOrder { get; set; }
        public int HiLifeDisplayOrder { get; set; }
        public string UniMartFreezeSubTypeCode { get; set; }
        public string UniMartFreezeName { get; set; }
        public string UniMartFreezeDescription { get; set; }
        public decimal UniMartFreezeRate { get; set; }
        public int UniMartFreezeDisplayOrder { get; set; }
        //public string TcatSubTypeCode { get; set; }
        //public string TcatName { get; set; }
        //public string TcatDescription { get; set; }
        //public decimal TcatRate { get; set; }
        //public int TcatDisplayOrder { get; set; }
        //public string EcanSubTypeCode { get; set; }
        //public string EcanName { get; set; }
        //public string EcanDescription { get; set; }
        //public decimal EcanRate { get; set; }
        //public int EcanDisplayOrder { get; set; }
        public bool UniMartIsActive { get; set; }
        public bool FamiIsActive { get; set; }
        public bool HiLifeIsActive { get; set; }
        public bool UniMartFreezeIsActive { get; set; }
        //public bool TcatIsActive { get; set; }
        //public bool EcanIsActive { get; set; }

        public string HomeSubTypeCode { get; set; }
        public string HomeName { get; set; }
        public string HomeDescription { get; set; }
        public decimal HomeRate { get; set; }
        public int HomeDisplayOrder { get; set; }
        public bool HomeIsActive { get; set; }
    }
}
