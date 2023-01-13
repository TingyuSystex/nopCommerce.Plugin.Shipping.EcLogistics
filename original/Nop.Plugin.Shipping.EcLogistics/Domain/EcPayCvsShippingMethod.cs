using System;
using Nop.Core;

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
