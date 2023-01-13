using System;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.EcLogistics.Domain
{
    public class EcPayHomeShippingFee : BaseEntity
    {
        public int EcPayHomeShippingMethodId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.TemperatureType")]
        public int TemperatureTypeId { get; set; }

        public ProductTemperatureType ProductTemperatureType
        {
            get => (ProductTemperatureType)TemperatureTypeId;
            set => TemperatureTypeId = (int)value;
        }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.SizeFrom")]
        public decimal SizeFrom { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.SizeTo")]
        public decimal SizeTo { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.WeightFrom")]
        public decimal WeightFrom { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.WeightTo")]
        public decimal WeightTo { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.Fee")]
        public decimal Fee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.ForeignFee")]
        public decimal ForeignFee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.HolidayExtraFee")]
        public decimal HolidayExtraFee { get; set; }

        public DateTime? CreatedOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
    }
}
