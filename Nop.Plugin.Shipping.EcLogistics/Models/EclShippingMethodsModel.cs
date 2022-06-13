using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    public record EclShippingMethodsModel : BaseNopModel
    {
        public string SubTypeCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Rate { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

    }
}
