using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Home.Domain;
using Nop.Plugin.Shipping.EcLogistics.Home.Models;
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
using Nop.Web.Framework.Models.DataTables;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.EcLogistics.Home.Controllers
{
    [AutoValidateAntiforgeryToken]
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class EcLogisticsHomeController : BasePaymentController
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
        private readonly IRepository<EcPayHomeShippingFee> _ecPayHomeShippingFeeRepository;
        private readonly IRepository<EcPayHomeShippingMethod> _ecPayHomeShippingMethodRepository;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IShoppingCartService _shoppingCartService;

        #endregion

        #region Ctor

        public EcLogisticsHomeController(
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
            IRepository<EcPayHomeShippingMethod> ecPayHomeShippingMethodRepository,
            IRepository<EcPayHomeShippingFee> ecPayHomeShippingFeeRepository,
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
            _paymentPluginManager = paymentPluginManager;
            _shoppingCartService = shoppingCartService;
            _ecPayHomeShippingMethodRepository = ecPayHomeShippingMethodRepository;
            _ecPayHomeShippingFeeRepository = ecPayHomeShippingFeeRepository;
        }

        #endregion

        #region Methods
        
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var modelData = await _ecPayHomeShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("HOME")));

            var model = new EcPayHomeShippingMethodModel
            {
                Name = modelData[0].Name,
                Description = modelData[0].Description,
                Fee = modelData[0].Fee,
                TransitDay = modelData[0].TransitDay,
            };
            model.IsFixedFee = modelData[0].IsFixedFee == "Y";

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

            return View("~/Plugins/Shipping.EcLogistics.Home/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(EcPayHomeShippingMethodModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            var modelData = await _ecPayHomeShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals(model.Name)));

            var domainModel = modelData.FirstOrDefault();

            domainModel.Name = model.Name;
            domainModel.Description = model.Description;
            domainModel.IsFixedFee = model.IsFixedFee ? "Y" : "N";
            domainModel.Fee = model.Fee;
            domainModel.TransitDay = model.TransitDay;
            domainModel.UpdatedOnUtc = DateTime.UtcNow;
            domainModel.PaymentMethod = string.Join(':', model.PaymentMethods.Select(pm => $"[{pm}]"));

            await _ecPayHomeShippingMethodRepository.UpdateAsync(domainModel);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        public async Task<IActionResult> GetHomeShippingMethods(EcPayHomeShippingMethodModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var data =  _ecPayHomeShippingFeeRepository.GetAll();

            return Json(new DataTablesModel() { Data = data });
        }

        public async Task<IActionResult> AddHomeShippingMethodPopup()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var model = new EcPayHomeShippingFee();
            
            return View("~/Plugins/Shipping.EcLogistics.Home/Views/AddHomeShippingMethodPopup.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAddHomeShippingMethodPopup(EcPayHomeShippingFee model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var from = model.SizeFrom;
            var to = model.SizeTo;
            //Check size
            if (from >= to)
            {
                ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsHome.ShippingMethod.SizeError");
                return View("~/Plugins/Shipping.EcLogistics.Home/Views/AddHomeShippingMethodPopup.cshtml", model);
            }

            //Check size range
            var rangeList = _ecPayHomeShippingFeeRepository.GetAll().Select(x => new
            {
                sizeFrom = x.SizeFrom,
                sizeTo = x.SizeTo,
                temperature = x.TemperatureType
            }).ToList();
            foreach (var range in rangeList)
            {
                if (((range.sizeFrom <= from && from <= range.sizeTo) || (range.sizeFrom <= to && to <= range.sizeTo)) && range.temperature == model.TemperatureType)
                {
                    ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsHome.ShippingMethod.SizeRepeat");
                    return View("~/Plugins/Shipping.EcLogistics.Home/Views/AddHomeShippingMethodPopup.cshtml", model);
                }
            }

            await _ecPayHomeShippingFeeRepository.InsertAsync(new EcPayHomeShippingFee()
            {
                EcPayHomeShippingMethodId = _ecPayHomeShippingMethodRepository.GetAll().FirstOrDefault().Id,
                TemperatureType = model.TemperatureType,
                SizeFrom = model.SizeFrom,
                SizeTo = model.SizeTo,
                Fee = model.Fee,
                ForeignFee = model.ForeignFee,
                HolidayExtraFee = model.HolidayExtraFee,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            });

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Shipping.EcLogistics.Home/Views/AddHomeShippingMethodPopup.cshtml", model);
        }

        public async Task<IActionResult> EditHomeShippingMethodPopup(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var model = await _ecPayHomeShippingFeeRepository.GetByIdAsync(id);
            if (model == null)
                //no record found with the specified id
                return RedirectToAction("Configure");

            return View("~/Plugins/Shipping.EcLogistics.Home/Views/EditHomeShippingMethodPopup.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEditHomeShippingMethodPopup(EcPayHomeShippingFee model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var data = await _ecPayHomeShippingFeeRepository.GetByIdAsync(model.Id);
            if (data == null)
                //no record found with the specified id
                return RedirectToAction("Configure");

            var from = model.SizeFrom;
            var to = model.SizeTo;
            //Check size
            if (from >= to)
            {
                ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsHome.ShippingMethod.SizeError");
                return View("~/Plugins/Shipping.EcLogistics.Home/Views/EditHomeShippingMethodPopup.cshtml", model);
            }

            //Check size range with same temperature type
            var rangeList = _ecPayHomeShippingFeeRepository.GetAll().Where(x => x.Id != model.Id).Select(x => new
            {
                sizeFrom = x.SizeFrom,
                sizeTo = x.SizeTo,
                temperature = x.TemperatureType
            }).ToList();
            foreach (var range in rangeList)
            {
                if (((range.sizeFrom <= from && from <= range.sizeTo) || (range.sizeFrom <= to && to <= range.sizeTo)) && range.temperature == model.TemperatureType)
                {
                    ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsHome.ShippingMethod.SizeRepeat");
                    return View("~/Plugins/Shipping.EcLogistics.Home/Views/EditHomeShippingMethodPopup.cshtml", model);
                }
            }
            
            data.TemperatureType = model.TemperatureType;
            data.SizeFrom = model.SizeFrom;
            data.SizeTo = model.SizeTo;
            data.Fee = model.Fee;
            data.ForeignFee = model.ForeignFee;
            data.HolidayExtraFee = model.HolidayExtraFee;
            data.UpdatedOnUtc = DateTime.UtcNow;

            await _ecPayHomeShippingFeeRepository.UpdateAsync(data);

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Shipping.EcLogistics.Home/Views/EditHomeShippingMethodPopup.cshtml", model);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteHomeShippingMethod(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            var model = await _ecPayHomeShippingFeeRepository.GetByIdAsync(id);
            if (model != null)
                await _ecPayHomeShippingFeeRepository.DeleteAsync(model);
            
            return new NullJsonResult();
        }

        #endregion

    }
}
