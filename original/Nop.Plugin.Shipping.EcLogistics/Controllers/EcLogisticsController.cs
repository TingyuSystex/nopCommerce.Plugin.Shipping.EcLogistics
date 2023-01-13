using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Plugin.Shipping.EcLogistics.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.EcLogistics.Controllers
{
    public class EcLogisticsController : BasePaymentController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;
        private readonly IAddressService _addressService;
        private readonly ICustomerService _customerService;
        private readonly IRepository<Address> _addressRepository;
        private readonly IRepository<EcPayCvsShippingMethod> _ecPayCvsShippingMethodRepository;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly EcLogisticsService _ecLogisticsService;

        #endregion

        #region Ctor

        public EcLogisticsController(
            ISettingService settingService,
            IStoreContext storeContext,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IGenericAttributeService genericAttributeService,
            IWorkContext workContext,
            IWebHelper webHelper,
            IAddressService addressService,
            ICustomerService customerService, 
            IRepository<Address> addressRepository,
            IRepository<EcPayCvsShippingMethod> ecPayCvsShippingMethodRepository,
            IPaymentPluginManager paymentPluginManager,
            IShoppingCartService shoppingCartService,
            EcLogisticsService ecLogisticsService
        )
        {
            _settingService = settingService;
            _storeContext = storeContext;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _genericAttributeService = genericAttributeService;
            _workContext = workContext;
            _webHelper = webHelper;
            _addressService = addressService;
            _customerService = customerService;
            _addressRepository = addressRepository;
            _ecPayCvsShippingMethodRepository = ecPayCvsShippingMethodRepository;
            _paymentPluginManager = paymentPluginManager;
            _shoppingCartService = shoppingCartService;
            _ecLogisticsService = ecLogisticsService;
        }

        #endregion

        #region Methods

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

            var model = new EcLogisticsModel
            {
                UseSandbox = ecLogisticsSettings.UseSandbox,
                MerchantId = ecLogisticsSettings.MerchantId,
                HashKey = ecLogisticsSettings.HashKey,
                HashIV = ecLogisticsSettings.HashIV,
                PlatformId = ecLogisticsSettings.PlatformId
            };

            return View("~/Plugins/Shipping.EcLogistics/Views/Configure.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> Configure(EcLogisticsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

            //save settings
            ecLogisticsSettings.UseSandbox = model.UseSandbox;
            ecLogisticsSettings.MerchantId = model.MerchantId;
            ecLogisticsSettings.HashKey = model.HashKey;
            ecLogisticsSettings.HashIV = model.HashIV;
            ecLogisticsSettings.PlatformId = model.PlatformId;

            await _settingService.SaveSettingAsync(ecLogisticsSettings);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        //個別超商取貨Configure
        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpGet]
        [Route("~/Admin/EcLogistics/ConfigureCVS/{cvsCode}")]
        public async Task<IActionResult> ConfigureCVS(string cvsCode)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();
            
            var cvs = cvsCode.ToUpper();
            var modelData = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals(cvs)));

            var model = new ConfigureCvsModel
            {
                Name = modelData[0].Name,
                Description = modelData[0].Description,
                TemperatureType = modelData[0].TemperatureType,
                LengthLimit = modelData[0].LengthLimit,
                SizeLimit = modelData[0].SizeLimit,
                WeightSizeLimit = modelData[0].WeightSizeLimit,
                Fee = modelData[0].Fee,
                TransitDay = modelData[0].TransitDay,
            };

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            //all payment methods (do not filter by country here as it could be not specified yet)
            var allPaymentMethods = await (await _paymentPluginManager
                    .LoadActivePluginsAsync(customer, store.Id)).ToListAsync();
            ////payment methods displayed during checkout (not with "Button" type)
            //var paymentMethods = allPaymentMethods
            //    .Where(pm => pm.PaymentMethodType != PaymentMethodType.Button).ToList();

            var selectedPaymentMethods = modelData[0].PaymentMethod.Split(':', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim('[', ']')).ToList();

            model.AvailablePaymentMethod = allPaymentMethods.Select(item =>
            {
                var friendlyName = item.PluginDescriptor.FriendlyName;
                var systemName = item.PluginDescriptor.SystemName;
                return new SelectListItem(friendlyName, systemName, selectedPaymentMethods.Contains(systemName));
            }).ToList();

            return View("~/Plugins/Shipping.EcLogistics/Views/ConfigureCVS.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> ConfigureCVS(ConfigureCvsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await ConfigureCVS(model.Name);

            var modelData = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals(model.Name)));

            var domainModel = modelData[0];

            domainModel.Name = model.Name;
            domainModel.Description = model.Description;
            domainModel.TemperatureType = model.TemperatureType;
            domainModel.LengthLimit = model.LengthLimit;
            domainModel.SizeLimit = model.SizeLimit;
            domainModel.WeightSizeLimit = model.WeightSizeLimit;
            domainModel.Fee = model.Fee;
            domainModel.TransitDay = model.TransitDay;
            domainModel.UpdatedOnUtc = DateTime.UtcNow;
            domainModel.PaymentMethod = string.Join(':', model.PaymentMethods.Select(pm => $"[{pm}]"));

            await _ecPayCvsShippingMethodRepository.UpdateAsync(domainModel);
            
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await ConfigureCVS(model.Name);
        }

        #region old

        [HttpPost]
        public async Task<IActionResult> GetCvs(string cvsSubType)
        {
            var storeLocation = _webHelper.GetStoreLocation();
            var devUrl = "https://logistics-stage.ecpay.com.tw/Express/map";
            var prodUrl = "https://logistics.ecpay.com.tw/Express/map";

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var paymentMethod = _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id).Result;

            var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

            var baseUrl = ecLogisticsSettings.UseSandbox ? devUrl : prodUrl;

            var merchantId = ecLogisticsSettings.MerchantId;
            var isCollection = "N";

            if (paymentMethod == "Payments.PayInStore")
                isCollection = "Y";

            var mapUrl = $"{baseUrl}?IsCollection={isCollection}" +
                $"&LogisticsSubType={cvsSubType}&LogisticsType=CVS&MerchantID={merchantId}" +
                $"&ServerReplyURL={storeLocation}EcLogistics/CvsResponse/";

            return Json(new { mapUrl = mapUrl });
        }

        #endregion

        [HttpPost]
        public async Task<IActionResult> CvsResponse(IFormCollection response)
        {
            var store = await _storeContext.GetCurrentStoreAsync();

            var address = new Address();
            address.CvsStoreId = response["CVSStoreID"];
            address.CvsStoreName = response["CVSStoreName"];
            address.City = response["CVSAddress"].ToString().Substring(0, 3);
            address.Address1 = response["CVSAddress"].ToString().Substring(3);
            address.PhoneNumber = response["CVSTelephone"].ToString();
            address.CreatedOnUtc = DateTime.UtcNow;

            var query = from a in _addressRepository.Table
                        where a.CvsStoreId != null
                        select a;

            var existedaddress = _addressService.FindAddress(query.ToList(),
                "", "", "", "", "", "",
                address.Address1, "", address.City, "", null, "", null, "");

            if (existedaddress == null)
            {
                //some validation
                if (address.CountryId == 0)
                    address.CountryId = null;
                if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;

                await _addressService.InsertAddressAsync(address);

                //TODO: Customer CvsAddresses Mapping Table
                //await _customerService.InsertCustomerAddressAsync(customer, address);
            }

            //await _genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.SelectCvsStoreId, response["CVSStoreID"], store.Id);

            ViewBag.storeName = response["CVSStoreName"] + " - " + response["CVSAddress"];
            ViewBag.addressId = existedaddress == null ? address.Id : existedaddress.Id;

            return View("~/Plugins/Shipping.EcLogistics/Views/CvsResponse.cshtml");
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpGet]
        [Route("~/Admin/EcLogistics/SendTest/{subType}")]
        public async Task<IActionResult> SendTest(string subType)
        {
            var result = await _ecLogisticsService.SendTestRequestAsync(subType.ToUpper());

            return Json(new { status = result });
        }

        #endregion

    }
}
