using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LinqToDB.Tools;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Core.Domain.Shipping;
using Nop.Web.Framework.Components;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Services.Common;
using Nop.Web.Factories;
using Nop.Web.Models.Common;

namespace Nop.Plugin.Shipping.EcLogistics.Components
{
    [ViewComponent(Name = "EcLogistics")]
    public class EcLogisticsViewComponent : NopViewComponent
    {

        #region Fields

        private readonly AddressSettings _addressSettings;
        private readonly IRepository<EcPayCvsShippingMethod> _ecPayCvsShippingMethodRepository;
        private readonly IRepository<Address> _addressRepository;
        private readonly IRepository<EcPayCvsAddressMapping> _ecPayCvsAddressMappingRepository;
        private readonly IWorkContext _workContext;
        private readonly IAddressModelFactory _addressModelFactory;
        private readonly IGenericAttributeService _genericAttributeService;


        #endregion

        #region Ctor

        public EcLogisticsViewComponent(AddressSettings addressSettings,
            IRepository<EcPayCvsShippingMethod> ecPayCvsShippingMethodRepository,
            IRepository<Address> addressRepository,
            IRepository<EcPayCvsAddressMapping> ecPayCvsAddressMappingRepository,
            IWorkContext workContext,
            IAddressModelFactory addressModelFactory,
            IGenericAttributeService genericAttributeService)
        {
            _addressSettings = addressSettings;
            _ecPayCvsShippingMethodRepository = ecPayCvsShippingMethodRepository;
            _addressRepository = addressRepository;
            _ecPayCvsAddressMappingRepository = ecPayCvsAddressMappingRepository;
            _workContext = workContext;
            _addressModelFactory = addressModelFactory;
            _genericAttributeService = genericAttributeService;
        }

        #endregion

        #region Method

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();

            var option = (ShippingOption)additionalData;

            var entity = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Description.Equals(option.Description)));
            var shippingOption = entity.FirstOrDefault();

            var customerAddressMappings = await _ecPayCvsAddressMappingRepository.GetAllAsync(am =>
                am.Where(am => am.CustomerId == customer.Id && am.CvsType == shippingOption.Name));
            var customerAddressIds = customerAddressMappings.Select(am => am.AddressId).ToList();
            var addressList = await _addressRepository.GetAllAsync(a => a.Where(a => a.Id.In(customerAddressIds)));
            
            var addresses = new List<CvsAddress>();
            foreach (var address in addressList)
            {
                addresses.Add(new CvsAddress()
                {
                    Id = address.Id,
                    Address = address.CvsStoreName + " - " + address.City + address.Address1
                });
            }

            var model = new CvsMapModel
            {
                CvsSubType = shippingOption.Name,
                Addresses = addresses,
                //ReceiverFirstName = await _genericAttributeService.GetAttributeAsync<string>(customer, "CvsReceiverFirstName"),
                //ReceiverLastName = await _genericAttributeService.GetAttributeAsync<string>(customer, "CvsReceiverLastName"),
                //ReceiverCellPhone = await _genericAttributeService.GetAttributeAsync<string>(customer, "CvsReceiverCellPhone")
        };
            return View("~/Plugins/Shipping.EcLogistics/Views/CvsRequest.cshtml", model);
        }

        #endregion
    }
}
