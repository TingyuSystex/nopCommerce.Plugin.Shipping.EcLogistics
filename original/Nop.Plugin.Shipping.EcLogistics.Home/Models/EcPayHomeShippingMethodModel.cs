using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Shipping.EcLogistics.Home.Domain;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.EcLogistics.Home.Models
{
    public record EcPayHomeShippingMethodModel : BaseNopModel
    {
        public EcPayHomeShippingMethodModel()
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


        [NopResourceDisplayName("Plugins.Shipping.EcLogisticsHome.Fields.IsFixedFee")]
        public bool IsFixedFee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.Fee")]
        public decimal Fee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.TransitDay")]
        public int TransitDay { get; set; }

    }
}
