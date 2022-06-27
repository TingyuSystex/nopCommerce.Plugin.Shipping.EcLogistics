using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    public record ConfigureCvsModel : BaseNopModel
    {
        public ConfigureCvsModel()
        {
            PaymentMethods = new List<string>();
            AvailablePaymentMethod = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.Description")]
        public string Description { get; set; }


        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.PaymentMethod")]
        public IList<SelectListItem> AvailablePaymentMethod { get; set; }
        public IList<string> PaymentMethods { get; set; }


        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.TemperatureType")]
        public string TemperatureType { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.LengthLimit")]
        public decimal LengthLimit { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.SizeLimit")]
        public decimal SizeLimit { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.WeightSizeLimit")]
        public decimal WeightSizeLimit { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.Fee")]
        public decimal Fee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.TransitDay")]
        public int TransitDay { get; set; }
    }
}
