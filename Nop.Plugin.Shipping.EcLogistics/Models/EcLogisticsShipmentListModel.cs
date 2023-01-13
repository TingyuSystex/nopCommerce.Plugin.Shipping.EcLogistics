using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    public record EcLogisticsShipmentListModel
    {
            public List<EcLogisticsShipmentModel> Items { get; set; }
        
    }
}
