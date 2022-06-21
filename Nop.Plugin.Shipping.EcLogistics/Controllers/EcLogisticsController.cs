using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Payments;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.DataTables;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Nop.Plugin.Shipping.EcLogistics.Controllers
{
    public class EcLogisticsController : BasePaymentController
    {
        #region Fields

        //private readonly Services.EcLogistics _service;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IWorkContext _workContext;
        private readonly IAddressService _addressService;
        private readonly ICustomerService _customerService;
        private readonly IRepository<Address> _addressRepository;
        private readonly IRepository<EcPayCvsShippingMethod> _ecPayCvsShippingMethodRepository;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IShoppingCartService _shoppingCartService;

        #endregion

        #region Ctor

        public EcLogisticsController(
            //Services.EcLogistics service,
            ISettingService settingService,
            IStoreContext storeContext,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IGenericAttributeService genericAttributeService,
            IWorkContext workContext,
            IAddressService addressService,
            ICustomerService customerService, 
            IRepository<Address> addressRepository,
            IRepository<EcPayCvsShippingMethod> ecPayCvsShippingMethodRepository,
            IPaymentPluginManager paymentPluginManager,
            IShoppingCartService shoppingCartService
        )
        {
            //_service = service;
            _settingService = settingService;
            _storeContext = storeContext;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _genericAttributeService = genericAttributeService;
            _workContext = workContext;
            _addressService = addressService;
            _customerService = customerService;
            _addressRepository = addressRepository;
            _ecPayCvsShippingMethodRepository = ecPayCvsShippingMethodRepository;
            _paymentPluginManager = paymentPluginManager;
            _shoppingCartService = shoppingCartService;
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

        //[AutoValidateAntiforgeryToken]
        //[AuthorizeAdmin]
        //[Area(AreaNames.Admin)]
        //[Route("~/Admin/EcLogistics/EditSubType/{SubTypeCode}")]
        //public virtual async Task<IActionResult> EditSubType(string SubTypeCode)
        //{
        //    if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
        //        return AccessDeniedView();

        //    var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

        //    var model = new EclShippingMethodsModel();
        //    switch (SubTypeCode)
        //    {
        //        case "UNIMART":
        //            model = new EclShippingMethodsModel()
        //            {
        //                Name = ecLogisticsSettings.UniMartName,
        //                Description = ecLogisticsSettings.UniMartDescription,
        //                Rate = ecLogisticsSettings.UniMartRate,
        //                DisplayOrder = ecLogisticsSettings.UniMartDisplayOrder,
        //                IsActive = ecLogisticsSettings.UniMartIsActive
        //            };
        //            break;
        //        case "FAMI":
        //            model = new EclShippingMethodsModel()
        //            {
        //                Name = ecLogisticsSettings.FamiName,
        //                Description = ecLogisticsSettings.FamiDescription,
        //                Rate = ecLogisticsSettings.FamiRate,
        //                DisplayOrder = ecLogisticsSettings.FamiDisplayOrder,
        //                IsActive = ecLogisticsSettings.FamiIsActive
        //            };
        //            break;
        //        case "HILIFE":
        //            model = new EclShippingMethodsModel()
        //            {
        //                Name = ecLogisticsSettings.HiLifeName,
        //                Description = ecLogisticsSettings.HiLifeDescription,
        //                Rate = ecLogisticsSettings.HiLifeRate,
        //                DisplayOrder = ecLogisticsSettings.HiLifeDisplayOrder,
        //                IsActive = ecLogisticsSettings.HiLifeIsActive
        //            };
        //            break;
        //        case "UNIMARTFREEZE":
        //            model = new EclShippingMethodsModel()
        //            {
        //                Name = ecLogisticsSettings.UniMartFreezeName,
        //                Description = ecLogisticsSettings.UniMartFreezeDescription,
        //                Rate = ecLogisticsSettings.UniMartFreezeRate,
        //                DisplayOrder = ecLogisticsSettings.UniMartFreezeDisplayOrder,
        //                IsActive = ecLogisticsSettings.UniMartFreezeIsActive
        //            };
        //            break;
        //        //case "TCAT":
        //        //    model = new EclShippingMethodsModel()
        //        //    {
        //        //        Name = ecLogisticsSettings.TcatName,
        //        //        Description = ecLogisticsSettings.TcatDescription,
        //        //        Rate = ecLogisticsSettings.TcatRate,
        //        //        DisplayOrder = ecLogisticsSettings.TcatDisplayOrder,
        //        //        IsActive = ecLogisticsSettings.TcatIsActive
        //        //    };
        //        //    break;
        //        //case "ECAN":
        //        //    model = new EclShippingMethodsModel()
        //        //    {
        //        //        Name = ecLogisticsSettings.EcanName,
        //        //        Description = ecLogisticsSettings.EcanDescription,
        //        //        Rate = ecLogisticsSettings.EcanRate,
        //        //        DisplayOrder = ecLogisticsSettings.EcanDisplayOrder,
        //        //        IsActive = ecLogisticsSettings.EcanIsActive
        //        //    };
        //        //    break;
        //        case "HOME":
        //            model = new EclShippingMethodsModel()
        //            {
        //                Name = ecLogisticsSettings.HomeName,
        //                Description = ecLogisticsSettings.HomeDescription,
        //                Rate = ecLogisticsSettings.HomeRate,
        //                DisplayOrder = ecLogisticsSettings.HomeDisplayOrder,
        //                IsActive = ecLogisticsSettings.HomeIsActive
        //            };
        //            break;
        //    }

        //    return View("~/Plugins/Shipping.EcLogistics/Views/EditSubType.cshtml", model);
        //}

        //[AutoValidateAntiforgeryToken]
        //[AuthorizeAdmin]
        //[Area(AreaNames.Admin)]
        //[HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        //[Route("~/Admin/EcLogistics/EditSubType/{SubTypeCode}")]
        //public virtual async Task<IActionResult> EditSubType(EclShippingMethodsModel model, bool continueEditing)
        //{
        //    if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
        //        return AccessDeniedView();

        //    if (ModelState.IsValid)
        //    {
        //        var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

        //        switch (model.SubTypeCode)
        //        {
        //            case "UNIMART":
        //                ecLogisticsSettings.UniMartName = model.Name;
        //                ecLogisticsSettings.UniMartDescription = model.Description;
        //                ecLogisticsSettings.UniMartRate = model.Rate;
        //                ecLogisticsSettings.UniMartDisplayOrder = model.DisplayOrder;
        //                ecLogisticsSettings.UniMartIsActive = model.IsActive;
        //                break;
        //            case "FAMI":
        //                ecLogisticsSettings.FamiName = model.Name;
        //                ecLogisticsSettings.FamiDescription = model.Description;
        //                ecLogisticsSettings.FamiRate = model.Rate;
        //                ecLogisticsSettings.FamiDisplayOrder = model.DisplayOrder;
        //                ecLogisticsSettings.FamiIsActive = model.IsActive;
        //                break;
        //            case "HILIFE":
        //                ecLogisticsSettings.HiLifeName = model.Name;
        //                ecLogisticsSettings.HiLifeDescription = model.Description;
        //                ecLogisticsSettings.HiLifeRate = model.Rate;
        //                ecLogisticsSettings.HiLifeDisplayOrder = model.DisplayOrder;
        //                ecLogisticsSettings.HiLifeIsActive = model.IsActive;
        //                break;
        //            case "UNIMARTFREEZE":
        //                ecLogisticsSettings.UniMartFreezeName = model.Name;
        //                ecLogisticsSettings.UniMartFreezeDescription = model.Description;
        //                ecLogisticsSettings.UniMartFreezeRate = model.Rate;
        //                ecLogisticsSettings.UniMartFreezeDisplayOrder = model.DisplayOrder;
        //                ecLogisticsSettings.UniMartFreezeIsActive = model.IsActive;
        //                break;
        //            //case "TCAT":
        //            //    ecLogisticsSettings.TcatName = model.Name;
        //            //    ecLogisticsSettings.TcatDescription = model.Description;
        //            //    ecLogisticsSettings.TcatRate = model.Rate;
        //            //    ecLogisticsSettings.TcatDisplayOrder = model.DisplayOrder;
        //            //    ecLogisticsSettings.TcatIsActive = model.IsActive;
        //            //    break;
        //            //case "ECAN":
        //            //    ecLogisticsSettings.EcanName = model.Name;
        //            //    ecLogisticsSettings.EcanDescription = model.Description;
        //            //    ecLogisticsSettings.EcanRate = model.Rate;
        //            //    ecLogisticsSettings.EcanDisplayOrder = model.DisplayOrder;
        //            //    ecLogisticsSettings.EcanIsActive = model.IsActive;
        //            //    break;
        //            case "HOME":
        //                ecLogisticsSettings.HomeName = model.Name;
        //                ecLogisticsSettings.HomeDescription = model.Description;
        //                ecLogisticsSettings.HomeRate = model.Rate;
        //                ecLogisticsSettings.HomeDisplayOrder = model.DisplayOrder;
        //                ecLogisticsSettings.HomeIsActive = model.IsActive;
        //                break;
        //        }

        //        _settingService.SaveSettingAsync(ecLogisticsSettings);

        //        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Shipping.Methods.Updated"));

        //        return continueEditing ? RedirectToAction("EditSubType", "EcLogistics", model.SubTypeCode) : RedirectToAction("Configure");
        //    }

        //    //if we got this far, something failed, redisplay form
        //    return View("~/Plugins/Shipping.EcLogistics/Views/EditSubType.cshtml", model);
        //}


        //[HttpPost]
        //public async Task<IActionResult> GetCvs()
        //{
        //    var customer = await _workContext.GetCurrentCustomerAsync();
        //    var store = await _storeContext.GetCurrentStoreAsync();
        //    var shippingMethod = _genericAttributeService.GetAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, store.Id).Result;
        //    var paymentMethod = _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id).Result;

        //    var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

        //    var merchantId = ecLogisticsSettings.MerchantId;
        //    var cvsSubType = "";
        //    var isCollection = "N";

        //    if (shippingMethod.Name == ecLogisticsSettings.UniMartName)
        //        cvsSubType = ecLogisticsSettings.UniMartSubTypeCode;
        //    if (shippingMethod.Name == ecLogisticsSettings.FamiName)
        //        cvsSubType = ecLogisticsSettings.FamiSubTypeCode;
        //    if (shippingMethod.Name == ecLogisticsSettings.HiLifeName)
        //        cvsSubType = ecLogisticsSettings.HiLifeSubTypeCode;

        //    if (paymentMethod == "Payments.PayInStore")
        //        isCollection = "Y";

        //    var mapUrl = $"https://logistics-stage.ecpay.com.tw/Express/map?IsCollection={isCollection}" +
        //        $"&LogisticsSubType={cvsSubType}&LogisticsType=CVS&MerchantID={merchantId}" +
        //        $"&ServerReplyURL=https://localhost:44369/EcLogistics/CvsResponse/";

        //    return Json(new { mapUrl = mapUrl });
        //}

        #endregion

        [HttpPost]
        public async Task<IActionResult> CvsResponse(IFormCollection response)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();

            var address = new Address();
            address.CVSStoreID = response["CVSStoreID"];
            address.CVSStoreName = response["CVSStoreName"];
            address.City = response["CVSAddress"].ToString().Substring(0, 3);
            address.Address1 = response["CVSAddress"].ToString().Substring(3);
            address.CreatedOnUtc = DateTime.UtcNow;

            var query = from a in _addressRepository.Table
                        where a.CVSStoreID != null
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

            await _genericAttributeService.SaveAttributeAsync<string>(customer, "SelectedCvsStoreId", response["CVSStoreID"], store.Id);

            ViewBag.storeName = response["CVSStoreName"] + " - " + response["CVSAddress"];

            return View("~/Plugins/Shipping.EcLogistics/Views/CvsResponse.cshtml");
        }

        #endregion

    }
}
