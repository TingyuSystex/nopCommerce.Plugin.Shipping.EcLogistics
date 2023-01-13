using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Data;
using Nop.Core.Domain.Shipping;
using Nop.Web.Framework.Components;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Plugin.Shipping.EcLogistics.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Components
{
    [ViewComponent(Name = "EcLogistics")]
    public class EcLogisticsViewComponent : NopViewComponent
    {

        #region Fields

        private readonly IRepository<EcPayCvsShippingMethod> _ecPayCvsShippingMethodRepository;

        #endregion

        #region Ctor

        public EcLogisticsViewComponent(IRepository<EcPayCvsShippingMethod> ecPayCvsShippingMethodRepository)
        {
            _ecPayCvsShippingMethodRepository = ecPayCvsShippingMethodRepository;
        }

        #endregion

        #region Method

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            var option = (ShippingOption)additionalData;

            var entity = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Description.Equals(option.Description)));
            var shippingOption = entity.FirstOrDefault();

            var model = new CvsMapModel
            {
                CvsSubType = shippingOption.Name,
            };
            return View("~/Plugins/Shipping.EcLogistics/Views/CvsRequest.cshtml", model);
        }

        #endregion
    }
}
