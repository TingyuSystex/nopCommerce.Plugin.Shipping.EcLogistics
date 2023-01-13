
using System.Collections.Generic;
using Nop.Core.Domain.Common;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    public record CvsMapModel
    {
        public CvsMapModel()
        {
            Addresses = new List<CvsAddress>();
        }
        public string CvsSubType { get; set; }
        public List<CvsAddress> Addresses { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.ReceiverFirstName")]
        public string ReceiverFirstName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.ReceiverLastName")]
        public string ReceiverLastName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.ReceiverCellPhone")]
        public string ReceiverCellPhone { get; set; }

    }

    public class CvsAddress
    {
        public int Id { get; set; }
        public string Address { get; set; }
    }
}
