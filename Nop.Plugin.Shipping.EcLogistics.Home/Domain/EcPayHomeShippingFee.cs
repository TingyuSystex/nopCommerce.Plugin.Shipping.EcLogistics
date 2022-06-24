using System;
using Nop.Core;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.EcLogistics.Home.Domain
{
    public class EcPayHomeShippingFee : BaseEntity
    {
        public int EcPayHomeShippingMethodId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.TemperatureType")]
        public string TemperatureType { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogisticsHome.Fields.SizeFrom")]
        public decimal SizeFrom { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogisticsHome.Fields.SizeTo")]
        public decimal SizeTo { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogisticsHome.Fields.Fee")]
        public decimal Fee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogisticsHome.Fields.ForeignFee")]
        public decimal ForeignFee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogisticsHome.Fields.HolidayExtraFee")]
        public decimal HolidayExtraFee { get; set; }

        public DateTime? CreatedOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
    }
}
