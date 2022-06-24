using System;
using Nop.Core;

namespace Nop.Plugin.Shipping.EcLogistics.Home.Domain
{
    public class EcPayHomeShippingMethod : BaseEntity
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string PaymentMethod { get; set; }

        public string IsFixedFee { get; set; }
        
        public decimal Fee { get; set; }

        public int TransitDay { get; set; }

        public DateTime? CreatedOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
    }
}
