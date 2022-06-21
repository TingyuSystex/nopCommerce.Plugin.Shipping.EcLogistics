using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.EcLogistics.Domain
{
    public class EcPayCvsShippingMethod : BaseEntity
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string PaymentMethod { get; set; }

        public string TemperatureType { get; set; }

        public decimal LengthLimit { get; set; }

        public decimal SizeLimit { get; set; }

        public decimal WeightSizeLimit { get; set; }

        public decimal Fee { get; set; }

        public int TransitDay { get; set; }

        public DateTime? CreatedOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
    }
}
