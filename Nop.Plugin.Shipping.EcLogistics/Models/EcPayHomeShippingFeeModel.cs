using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    public class EcPayHomeShippingFeeModel : BaseEntity
    {
        public int EcPayHomeShippingMethodId { get; set; }

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

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.TemperatureType")]
        public int TemperatureTypeId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.TemperatureType")]
        public string TemperatureType { get; set; }

        /// <summary>
        /// Gets or sets the product temperature type
        /// </summary>
        public ProductTemperatureType ProductTemperatureType
        {
            get => (ProductTemperatureType)TemperatureTypeId;
            set => TemperatureTypeId = (int)value;
        }

        public DateTime? CreatedOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
    }
}
