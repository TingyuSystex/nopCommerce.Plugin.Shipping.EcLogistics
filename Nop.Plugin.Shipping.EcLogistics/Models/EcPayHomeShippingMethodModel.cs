using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    public record EcPayHomeShippingMethodModel : BaseNopModel
    {
        public EcPayHomeShippingMethodModel()
        {
            PaymentMethods = new List<string>();
            AvailablePaymentMethod = new List<SelectListItem>();
            AvailableTemperatureType = new List<SelectListItem>();
            AvailablePickupTimes = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.Catalog.Products.Fields.AvailableProductTemperatureType")]
        public int TemperatureTypeId { get; set; }
        public IList<SelectListItem> AvailableTemperatureType { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.Description")]
        public string Description { get; set; }


        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.PaymentMethod")]
        public IList<SelectListItem> AvailablePaymentMethod { get; set; }
        public IList<string> PaymentMethods { get; set; }


        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.IsFixedFee")]
        public bool IsFixedFee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.Fee")]
        public decimal Fee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.TransitDay")]
        public int TransitDay { get; set; }


        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Home.Fields.ScheduledPickupTime")]
        public int ScheduledPickupTime { get; set; }
        public IList<SelectListItem> AvailablePickupTimes { get; set; }

    }
}
