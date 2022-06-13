using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.EcLogistics.Models
{
    public record EcLogisticsModel : BaseNopModel
    {
        #region Ctor

        public EcLogisticsModel()
        {
            //LogisticsSubTypes = new List<string>();
            //AvailableLogisticsSubTypes = new List<SelectListItem>();
        }

        #endregion

        #region Properties
        
        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.MerchantId")]
        public string MerchantId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.HashKey")]
        public string HashKey { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.HashIV")]
        public string HashIV { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.EcLogistics.Fields.PlatformId")]
        public string? PlatformId { get; set; }



        #endregion
    }
}