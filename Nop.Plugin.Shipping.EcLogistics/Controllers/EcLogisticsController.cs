using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Plugin.Shipping.EcLogistics.Factories;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Plugin.Shipping.EcLogistics.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.DataTables;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using ReturnRequest = Nop.Core.Domain.Orders.ReturnRequest;

namespace Nop.Plugin.Shipping.EcLogistics.Controllers
{
    public class EcLogisticsController : BasePaymentController
    {
        #region Fields

        private static string SEARCH_URL = "https://www.ecpay.com.tw/IntroTransport/Logistics_Search";

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
        private readonly IRepository<EcPayCvsAddressMapping> _ecPayCvsAddressMappingRepository;
        private readonly IRepository<EcPayHomeShippingFee> _ecPayHomeShippingFeeRepository;
        private readonly IRepository<EcPayHomeShippingMethod> _ecPayHomeShippingMethodRepository;
        private readonly IRepository<EcPayReturnNumberMapping> _ecPayReturnNumberMappingRepository;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShipmentService _shipmentService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly EcLogisticsService _ecLogisticsService;
        private readonly EcLogisticsModelFactory _ecLogisticsModelFactory;
        private readonly IReturnRequestService _returnRequestService;
        private readonly IRepository<ReturnRequest> _returnRequestsRepository;
        private readonly IReturnRequestModelFactory _returnRequestModelFactory;
        private readonly ILogger _logger;
        private readonly IEventPublisher _eventPublisher;

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
            IRepository<EcPayCvsAddressMapping> ecPayCvsAddressMappingRepository,
            IRepository<EcPayHomeShippingMethod> ecPayHomeShippingMethodRepository,
            IRepository<EcPayHomeShippingFee> ecPayHomeShippingFeeRepository,
            IRepository<EcPayReturnNumberMapping> ecPayReturnNumberMappingRepository,
            IPaymentPluginManager paymentPluginManager,
            IShoppingCartService shoppingCartService,
            IShipmentService shipmentService,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            EcLogisticsService ecLogisticsService,
            EcLogisticsModelFactory ecLogisticsModelFactory,
            IReturnRequestService returnRequestService,
            IRepository<ReturnRequest> returnRequestsRepository,
            IReturnRequestModelFactory returnRequestModelFactory,
            ILogger logger,
            IEventPublisher eventPublisher
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
            _ecPayCvsAddressMappingRepository = ecPayCvsAddressMappingRepository;
            _ecPayHomeShippingMethodRepository = ecPayHomeShippingMethodRepository;
            _ecPayHomeShippingFeeRepository = ecPayHomeShippingFeeRepository;
            _ecPayReturnNumberMappingRepository = ecPayReturnNumberMappingRepository;
            _paymentPluginManager = paymentPluginManager;
            _shoppingCartService = shoppingCartService;
            _shipmentService = shipmentService;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _ecLogisticsService = ecLogisticsService;
            _ecLogisticsModelFactory = ecLogisticsModelFactory;
            _returnRequestService = returnRequestService;
            _returnRequestsRepository = returnRequestsRepository;
            _returnRequestModelFactory = returnRequestModelFactory;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        #endregion

        #region MainConfiguration

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
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

            return View("~/Plugins/Shipping.EcLogistics/Views/Configure/Configure.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> Configure(EcLogisticsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
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

        #region CvsCinfiguration

        //個別超商取貨Configure
        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpGet]
        [Route("~/Admin/EcLogistics/ConfigureCVS/{cvsCode}")]
        public async Task<IActionResult> ConfigureCVS(string cvsCode)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var cvs = cvsCode.ToUpper();
            var modelData = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals(cvs)));

            var model = new ConfigureCvsModel
            {
                Name = modelData[0].Name,
                Description = modelData[0].Description,
                TemperatureTypeId = modelData[0].TemperatureTypeId,
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



            return View("~/Plugins/Shipping.EcLogistics/Views/Configure/ConfigureCVS.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> ConfigureCVS(ConfigureCvsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await ConfigureCVS(model.Name);

            var modelData = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals(model.Name)));

            var domainModel = modelData[0];

            domainModel.Name = model.Name;
            domainModel.Description = model.Description;
            domainModel.TemperatureTypeId = model.TemperatureTypeId;
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

        #endregion

        #region HomeConfiguration

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpGet]
        [Route("~/Admin/EcLogistics/ConfigureHome/{homeCode}")]
        public async Task<IActionResult> ConfigureHome(string homeCode)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var home = homeCode.ToUpper();
            var modelData = await _ecPayHomeShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals(home)));

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

            var allPaymentMethods = await (await _paymentPluginManager
                    .LoadActivePluginsAsync(customer, store.Id)).ToListAsync();

            var selectedPaymentMethods = modelData[0].PaymentMethod.Split(':', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim('[', ']')).ToList();

            model.AvailablePaymentMethod = allPaymentMethods.Select(item =>
            {
                var friendlyName = item.PluginDescriptor.FriendlyName;
                var systemName = item.PluginDescriptor.SystemName;
                return new SelectListItem(friendlyName, systemName, selectedPaymentMethods.Contains(systemName));
            }).ToList();

            if (homeCode == "TCAT")
            {
                model.ScheduledPickupTime = modelData[0].ScheduledPickupTime;
                model.AvailablePickupTimes = new List<SelectListItem>
                {
                    new SelectListItem("9 ~ 12", "1", model.ScheduledPickupTime == 1),
                    new SelectListItem("12 ~ 17", "2", model.ScheduledPickupTime == 2),
                    new SelectListItem(
                        await _localizationService.GetResourceAsync(
                            "Plugins.Shipping.EcLogistics.Home.Fields.ScheduledPickupTime.NoLimit"), "4",
                        model.ScheduledPickupTime == 4),
                };
            }

            return View("~/Plugins/Shipping.EcLogistics/Views/Configure/ConfigureHome.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> ConfigureHome(EcPayHomeShippingMethodModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
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

            if (model.Name == "TCAT")
            {
                domainModel.ScheduledPickupTime = model.ScheduledPickupTime;
            }

            await _ecPayHomeShippingMethodRepository.UpdateAsync(domainModel);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await ConfigureHome(model.Name);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> GetTcatShippingFees()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var method = await _ecPayHomeShippingMethodRepository.GetAllAsync(x => x.Where(x => x.Name == "TCAT"));
            var data = _ecPayHomeShippingFeeRepository.GetAll(x =>
                x.Where(x => x.EcPayHomeShippingMethodId == method.FirstOrDefault().Id));

            var models = new List<EcPayHomeShippingFeeModel>();

            foreach (var d in data)
            {
                var model = new EcPayHomeShippingFeeModel();
                model.Id = d.Id;
                model.EcPayHomeShippingMethodId = d.EcPayHomeShippingMethodId;
                model.Fee = d.Fee;
                model.SizeFrom = d.SizeFrom;
                model.SizeTo = d.SizeTo;
                model.WeightFrom = d.WeightFrom;
                model.WeightTo = d.WeightTo;
                model.ForeignFee = d.ForeignFee;
                model.CreatedOnUtc = d.CreatedOnUtc;
                model.UpdatedOnUtc = d.UpdatedOnUtc;
                model.HolidayExtraFee = d.HolidayExtraFee;
                model.TemperatureTypeId = d.TemperatureTypeId;
                model.TemperatureType = await _localizationService.GetLocalizedEnumAsync(d.ProductTemperatureType);
                models.Add(model);
            }

            return Json(new DataTablesModel() { Data = models });
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> GetPostShippingFees()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var method = await _ecPayHomeShippingMethodRepository.GetAllAsync(x => x.Where(x => x.Name == "POST"));
            var data = _ecPayHomeShippingFeeRepository.GetAll(x =>
                x.Where(x => x.EcPayHomeShippingMethodId == method.FirstOrDefault().Id));

            var models = new List<EcPayHomeShippingFeeModel>();

            foreach (var d in data)
            {
                var model = new EcPayHomeShippingFeeModel();
                model.Id = d.Id;
                model.EcPayHomeShippingMethodId = d.EcPayHomeShippingMethodId;
                model.Fee = d.Fee;
                model.SizeFrom = d.SizeFrom;
                model.SizeTo = d.SizeTo;
                model.WeightFrom = d.WeightFrom;
                model.WeightTo = d.WeightTo;
                model.ForeignFee = d.ForeignFee;
                model.CreatedOnUtc = d.CreatedOnUtc;
                model.UpdatedOnUtc = d.UpdatedOnUtc;
                model.HolidayExtraFee = d.HolidayExtraFee;
                model.TemperatureTypeId = d.TemperatureTypeId;
                model.TemperatureType = await _localizationService.GetLocalizedEnumAsync(d.ProductTemperatureType);
                models.Add(model);
            }

            return Json(new DataTablesModel() { Data = models });
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> AddHomeShippingMethodPopup()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var model = new EcPayHomeShippingFeeModel();

            return View("~/Plugins/Shipping.EcLogistics/Views/Configure/AddHomeShippingMethodPopup.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> SaveAddHomeShippingMethodPopup(EcPayHomeShippingFeeModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var isTcat = HttpContext.Request.Query["homeType"] == "TCAT";
            var tcat = await _ecPayHomeShippingMethodRepository.GetAllAsync(x => x.Where(x => x.Name == "TCAT"));
            var post = await _ecPayHomeShippingMethodRepository.GetAllAsync(x => x.Where(x => x.Name == "POST"));

            if (isTcat)
            {
                var from = model.SizeFrom;
                var to = model.SizeTo;
                //Check size
                if (from >= to)
                {
                    ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Home.ShippingMethod.SizeError");
                    return View("~/Plugins/Shipping.EcLogistics/Views/Configure/AddHomeShippingMethodPopup.cshtml", model);
                }

                //Check size range
                var rangeList = _ecPayHomeShippingFeeRepository.GetAll().Where(x => x.EcPayHomeShippingMethodId == tcat.FirstOrDefault().Id).Select(x => new
                {
                    sizeFrom = x.SizeFrom,
                    sizeTo = x.SizeTo,
                    temperature = x.TemperatureTypeId
                }).ToList();
                foreach (var range in rangeList)
                {
                    if (((range.sizeFrom <= from && from <= range.sizeTo) || (range.sizeFrom <= to && to <= range.sizeTo)) && range.temperature == model.TemperatureTypeId)
                    {
                        ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Home.ShippingMethod.SizeRepeat");
                        return View("~/Plugins/Shipping.EcLogistics/Views/Configure/AddHomeShippingMethodPopup.cshtml", model);
                    }
                }

                var ecPayHomeShippingFee = new EcPayHomeShippingFee()
                {
                    EcPayHomeShippingMethodId = tcat.FirstOrDefault().Id,
                    TemperatureTypeId = model.TemperatureTypeId,
                    SizeFrom = model.SizeFrom,
                    SizeTo = model.SizeTo,
                    WeightFrom = model.WeightFrom,
                    WeightTo = model.WeightTo,
                    Fee = model.Fee,
                    ForeignFee = model.ForeignFee,
                    HolidayExtraFee = model.HolidayExtraFee,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                };

                await _ecPayHomeShippingFeeRepository.InsertAsync(ecPayHomeShippingFee);
            }
            else
            {
                var from = model.WeightFrom;
                var to = model.WeightTo;
                //Check weight
                if (from >= to)
                {
                    ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Home.ShippingMethod.WeightError");
                    return View("~/Plugins/Shipping.EcLogistics/Views/Configure/AddHomeShippingMethodPopup.cshtml", model);
                }

                //Check weight range
                var rangeList = _ecPayHomeShippingFeeRepository.GetAll().Where(x => x.EcPayHomeShippingMethodId == post.FirstOrDefault().Id).Select(x => new
                {
                    weightFrom = x.WeightFrom,
                    weightTo = x.WeightTo
                }).ToList();
                foreach (var range in rangeList)
                {
                    if (((range.weightFrom <= from && from <= range.weightTo) || (range.weightFrom <= to && to <= range.weightTo)))
                    {
                        ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Home.ShippingMethod.WeightRepeat");
                        return View("~/Plugins/Shipping.EcLogistics/Views/Configure/AddHomeShippingMethodPopup.cshtml", model);
                    }
                }

                var ecPayHomeShippingFee = new EcPayHomeShippingFee()
                {
                    EcPayHomeShippingMethodId = post.FirstOrDefault().Id,
                    TemperatureTypeId = (int)ProductTemperatureType.Normal,
                    SizeFrom = model.SizeFrom,
                    SizeTo = model.SizeTo,
                    WeightFrom = model.WeightFrom,
                    WeightTo = model.WeightTo,
                    Fee = model.Fee,
                    ForeignFee = model.ForeignFee,
                    HolidayExtraFee = model.HolidayExtraFee,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                };

                await _ecPayHomeShippingFeeRepository.InsertAsync(ecPayHomeShippingFee);
            }

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Shipping.EcLogistics/Views/Configure/AddHomeShippingMethodPopup.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> EditHomeShippingMethodPopup(int id, string homeType)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var ecPayHomeShippingFee = await _ecPayHomeShippingFeeRepository.GetByIdAsync(id);
            if (ecPayHomeShippingFee == null)
                //no record found with the specified id
                return await ConfigureHome(homeType);

            var model = new EcPayHomeShippingFeeModel
            {
                EcPayHomeShippingMethodId = ecPayHomeShippingFee.Id,
                TemperatureTypeId = ecPayHomeShippingFee.TemperatureTypeId,
                TemperatureType = await _localizationService.GetLocalizedEnumAsync(ecPayHomeShippingFee.ProductTemperatureType),
                SizeFrom = ecPayHomeShippingFee.SizeFrom,
                SizeTo = ecPayHomeShippingFee.SizeTo,
                WeightFrom = ecPayHomeShippingFee.WeightFrom,
                WeightTo = ecPayHomeShippingFee.WeightTo,
                Fee = ecPayHomeShippingFee.Fee,
                ForeignFee = ecPayHomeShippingFee.ForeignFee,
                HolidayExtraFee = ecPayHomeShippingFee.HolidayExtraFee,
                CreatedOnUtc = ecPayHomeShippingFee.CreatedOnUtc,
                UpdatedOnUtc = DateTime.UtcNow
            };

            return View("~/Plugins/Shipping.EcLogistics/Views/Configure/EditHomeShippingMethodPopup.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> SaveEditHomeShippingMethodPopup(EcPayHomeShippingFeeModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var data = await _ecPayHomeShippingFeeRepository.GetByIdAsync(model.Id);
            if (data == null)
            {
                var homeType = HttpContext.Request.Query["homeType"] == "TCAT" ? "TCAT" : "POST";
                //no record found with the specified id
                return await ConfigureHome(homeType);
            }

            var tcat = await _ecPayHomeShippingMethodRepository.GetAllAsync(x => x.Where(x => x.Name == "TCAT"));
            var post = await _ecPayHomeShippingMethodRepository.GetAllAsync(x => x.Where(x => x.Name == "POST"));

            if (HttpContext.Request.Query["homeType"] == "TCAT")
            {
                var from = model.SizeFrom;
                var to = model.SizeTo;
                //Check size
                if (from >= to)
                {
                    ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Home.ShippingMethod.SizeError");
                    return View("~/Plugins/Shipping.EcLogistics/Views/Configure/EditHomeShippingMethodPopup.cshtml", model);
                }

                //Check size range
                var rangeList = _ecPayHomeShippingFeeRepository.GetAll().Where(x => x.EcPayHomeShippingMethodId == tcat.FirstOrDefault().Id && x.Id != model.Id).Select(x => new
                {
                    sizeFrom = x.SizeFrom,
                    sizeTo = x.SizeTo,
                    temperature = Enum.GetName(typeof(ProductTemperatureType), x.TemperatureTypeId)
                }).ToList();
                foreach (var range in rangeList)
                {
                    if (((range.sizeFrom <= from && from <= range.sizeTo) || (range.sizeFrom <= to && to <= range.sizeTo)) && range.temperature == Enum.GetName(typeof(ProductTemperatureType), model.TemperatureTypeId))
                    {
                        ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Home.ShippingMethod.SizeRepeat");
                        return View("~/Plugins/Shipping.EcLogistics/Views/Configure/EditHomeShippingMethodPopup.cshtml", model);
                    }
                }
            }
            else
            {
                var from = model.WeightFrom;
                var to = model.WeightTo;
                //Check weight
                if (from >= to)
                {
                    ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Home.ShippingMethod.WeightError");
                    return View("~/Plugins/Shipping.EcLogistics/Views/Configure/EditHomeShippingMethodPopup.cshtml", model);
                }

                //Check weight range
                var rangeList = _ecPayHomeShippingFeeRepository.GetAll().Where(x => x.EcPayHomeShippingMethodId == post.FirstOrDefault().Id && x.Id != model.Id).Select(x => new
                {
                    weightFrom = x.WeightFrom,
                    weightTo = x.WeightTo
                }).ToList();
                foreach (var range in rangeList)
                {
                    if (((range.weightFrom <= from && from <= range.weightTo) || (range.weightFrom <= to && to <= range.weightTo)))
                    {
                        ViewBag.error = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Home.ShippingMethod.WeightRepeat");
                        return View("~/Plugins/Shipping.EcLogistics/Views/Configure/EditHomeShippingMethodPopup.cshtml", model);
                    }
                }
            }

            data.TemperatureTypeId = model.TemperatureTypeId;
            data.SizeFrom = model.SizeFrom;
            data.SizeTo = model.SizeTo;
            data.WeightFrom = model.WeightFrom;
            data.WeightTo = model.WeightTo;
            data.Fee = model.Fee;
            data.ForeignFee = model.ForeignFee;
            data.HolidayExtraFee = model.HolidayExtraFee;
            data.UpdatedOnUtc = DateTime.UtcNow;

            await _ecPayHomeShippingFeeRepository.UpdateAsync(data);

            ViewBag.RefreshPage = true;

            var newModel = new EcPayHomeShippingFeeModel
            {
                Id = data.Id,
                EcPayHomeShippingMethodId = data.EcPayHomeShippingMethodId,
                TemperatureTypeId = data.TemperatureTypeId,
                SizeFrom = data.SizeFrom,
                SizeTo = data.SizeTo,
                WeightFrom = data.WeightFrom,
                WeightTo = data.WeightTo,
                Fee = data.Fee,
                ForeignFee = data.ForeignFee,
                HolidayExtraFee = data.HolidayExtraFee,
                CreatedOnUtc = data.CreatedOnUtc,
                UpdatedOnUtc = data.UpdatedOnUtc
            };

            return View("~/Plugins/Shipping.EcLogistics/Views/Configure/EditHomeShippingMethodPopup.cshtml", newModel);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
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

        #region FrontEnd Checkout

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
                $"&LogisticsSubType={cvsSubType}&LogisticsType=CVS&MerchantID={merchantId}&ExtraData={customer.Id}" +
                $"&ServerReplyURL={storeLocation}EcLogistics/CvsResponse/";

            return Json(new { mapUrl = mapUrl });
        }

        [HttpPost]
        public async Task<IActionResult> CvsResponse(IFormCollection response)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _customerService.GetCustomerByIdAsync(int.Parse(response["ExtraData"]));
            var cvsType = response["LogisticsSubType"];

            var address = new Address();
            address.CvsStoreId = response["CVSStoreID"];
            address.CvsStoreName = response["CVSStoreName"];
            address.City = response["CVSAddress"].ToString().Substring(0, 3);
            address.Address1 = response["CVSAddress"].ToString().Substring(3);
            address.PhoneNumber = response["CVSTelephone"].ToString();
            address.CreatedOnUtc = DateTime.UtcNow;

            var existedaddress = await _addressRepository.GetAllAsync(a => a.Where(a => a.CvsStoreId == address.CvsStoreId));

            if (existedaddress.Count == 0)
            {
                //some validation
                if (address.CountryId == 0)
                    address.CountryId = null;
                if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;

                await _addressService.InsertAddressAsync(address);
            }

            //Cvs address mapping with customer
            var addressmapping = new EcPayCvsAddressMapping()
            {
                CustomerId = customer.Id,
                AddressId = existedaddress == null ? address.Id : existedaddress.FirstOrDefault().Id,
                CvsType = cvsType
            };
            var existed = await _ecPayCvsAddressMappingRepository.GetAllAsync(am =>
                am.Where(am => am.CustomerId == addressmapping.CustomerId && am.AddressId == addressmapping.AddressId));
            if (existed.Count == 0)
                await _ecPayCvsAddressMappingRepository.InsertAsync(addressmapping);

            //await _genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.SelectCvsStoreId, response["CVSStoreID"], store.Id);

            ViewBag.storeName = response["CVSStoreName"] + " - " + response["CVSAddress"];
            ViewBag.addressId = existedaddress == null ? address.Id : existedaddress.FirstOrDefault().Id;

            return View("~/Plugins/Shipping.EcLogistics/Views/CvsResponse.cshtml");
        }

        #endregion

        #region Shipments

        #region Utilities

        protected virtual async ValueTask<bool> HasAccessToShipmentAsync(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            if (await _workContext.GetCurrentVendorAsync() == null)
                //not a vendor; has access
                return true;

            return await HasAccessToOrderAsync(shipment.OrderId);
        }

        protected virtual async Task<bool> HasAccessToOrderAsync(int orderId)
        {
            if (orderId == 0)
                return false;

            var currentVendor = await _workContext.GetCurrentVendorAsync();
            if (currentVendor == null)
                //not a vendor; has access
                return true;

            var vendorId = currentVendor.Id;
            var hasVendorProducts = (await _orderService.GetOrderItemsAsync(orderId, vendorId: vendorId)).Any();

            return hasVendorProducts;
        }


        #endregion

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> CreateList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel))
                return AccessDeniedView();

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareEclShipmentSearchModelAsync(new EclShipmentSearchModel());

            return View("~/Plugins/Shipping.EcLogistics/Views/Shipment/CreateList.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> CreateListSelect(EclShipmentSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return await AccessDeniedDataTablesJson();

            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareCreateListModelAsync(searchModel);

            return Json(model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> GetTrackingNumberSelected(ICollection<int> selectedIds)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            if (selectedIds == null || selectedIds.Count == 0)
                return NoContent();

            var shipments = await _shipmentService.GetShipmentsByIdsAsync(selectedIds.ToArray());

            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendorAsync() != null)
            {
                shipments = await shipments.WhereAwait(HasAccessToShipmentAsync).ToListAsync();
            }

            foreach (var shipment in shipments)
            {
                try
                {
                    // get tracking number
                    var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
                    var subType = order.ShippingRateComputationMethodSystemName.Substring(21).ToUpper();
                    if (subType == "TCAT" || subType == "POST")
                    {
                        shipment.TrackingNumber = await _ecLogisticsService.CreateHomeAsync(shipment);
                    }
                    else
                    {
                        shipment.TrackingNumber = await _ecLogisticsService.CreateCvsAsync(shipment);
                    }
                    shipment.ShipmentNo = await _ecLogisticsService.QueryTradeInfoAsync(shipment);
                    shipment.ShipmentUrl = SEARCH_URL;
                    await _shipmentService.UpdateShipmentAsync(shipment);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            return Json(new { Result = true });
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> PrintList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareEclShipmentSearchModelAsync(new EclShipmentSearchModel());

            return View("~/Plugins/Shipping.EcLogistics/Views/Shipment/PrintList.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> PrintListSelect(EclShipmentSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return await AccessDeniedDataTablesJson();

            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (searchModel.isInit || "All".Equals(searchModel.ShippingMethod)) {
               var newModel = await _ecLogisticsModelFactory.PreparePrintListModelAsync(new EclShipmentSearchModel());
               return Json(newModel);
            }
            //prepare model
            var model = await _ecLogisticsModelFactory.PreparePrintListModelAsync(searchModel);

            return Json(model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> PrintTradeDocumentSelected(ICollection<int> selectedIds)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            if (selectedIds == null || selectedIds.Count == 0)
                return NoContent();

            var shipments = await _shipmentService.GetShipmentsByIdsAsync(selectedIds.ToArray());

            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendorAsync() != null)
            {
                shipments = await shipments.WhereAwait(HasAccessToShipmentAsync).ToListAsync();
            }

            var trackingNumbers = shipments.Select(x => x.TrackingNumber).ToList();
            var idString = string.Join(",", trackingNumbers);

            var respHtml = await _ecLogisticsService.PrintTradeDocumentAsync(idString);

            return Json(new { respHtml = respHtml });
        }

        [HttpPost]
        public async Task<string> StatusReply(IFormCollection response)
        {
            try
            {
                var replyModel = new EcLogisticsReplyModel(response);
                await _logger.InsertLogAsync(Core.Domain.Logging.LogLevel.Information, "綠界正物流狀態回傳",
                    $"單號: {replyModel.MerchantTradeNo}\r\n " +
                    $"目前物流狀態: {replyModel.RtnCode}\r\n " +
                    $"物流狀態說明: {replyModel.RtnMsg}\r\n " +
                    $"物流類型: {replyModel.LogisticsType}\r\n " +
                    $"物流子類型: {replyModel.LogisticsSubType}");

                var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

                // 檢查碼
                var orderParams = response.OrderBy(kp => kp.Key).ToDictionary(d => d.Key, d => d.Value.ToString());
                orderParams["CheckMacValue"] = null;
                var queryString = EcLogisticsService.ToQueryString(orderParams);
                var checkValue = EcLogisticsService.generateCheckMacValue(queryString, ecLogisticsSettings.HashKey, ecLogisticsSettings.HashIV);

                if (!replyModel.Equals(checkValue))
                {
                    await _logger.ErrorAsync("檢查碼錯誤");
                    return "1|OK";
                }

                var rtnCode = replyModel.RtnCode;
                // TODO: MerchantTradeNo 取號規則
                var ids = replyModel.MerchantTradeNo.Split('-');
                int orderId, shipmentId;
                int.TryParse(ids[0], out orderId);
                int.TryParse(ids[1], out shipmentId);

                var logisticsType = replyModel.LogisticsType;
                var LogisticsSubType = replyModel.LogisticsSubType;

                if ("CVS".Equals(logisticsType))
                {
                    switch (LogisticsSubType)
                    {
                        case "FAMI":
                            if ("3022".Equals(rtnCode))
                                await UpdateShipmentStatus(shipmentId);
                            break;
                        case "UNIMART":
                        case "UNIMARTFREEZE":
                            if ("2067".Equals(rtnCode))
                                await UpdateShipmentStatus(shipmentId);
                            break;
                        case "HILIFE":
                            if ("2067".Equals(rtnCode) || "3022".Equals(rtnCode))
                                await UpdateShipmentStatus(shipmentId);
                            break;
                    }
                }
                else if ("HOME".Equals(logisticsType))
                {
                    switch (LogisticsSubType)
                    {
                        case "ECAN":
                            if ("3003".Equals(rtnCode))
                                await UpdateShipmentStatus(shipmentId);
                            break;
                        case "TCAT":
                            if ("3003".Equals(rtnCode))
                                await UpdateShipmentStatus(shipmentId);
                            break;
                        case "POST":
                            if ("3307".Equals(rtnCode) || "3308".Equals(rtnCode) || "3309".Equals(rtnCode))
                                await UpdateShipmentStatus(shipmentId);
                            break;
                    }
                }

                return "1|OK";
            }
            catch(Exception ex)
            {
                await _logger.ErrorAsync(ex.Message, ex);
                return "1|OK";
            }
        }

        private async Task UpdateShipmentStatus(int shipmentId)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(shipmentId);
            await _orderProcessingService.ShipAsync(shipment, false);
            await _orderProcessingService.DeliverAsync(shipment, false);
        }

        #endregion

        #region Return Requests

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> GetReturnRequestNumberList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReturnRequests))
                return AccessDeniedView();

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareReturnRequestSearchModelAsync(new ReturnRequestSearchModel());

            return View("~/Plugins/Shipping.EcLogistics/Views/ReturnRequests/List.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> GetReturnRequestNumberList(ReturnRequestSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReturnRequests))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareReturnRequestListModelAsync(searchModel);

            return Json(model);
        }

        //  退貨商品收取-查詢
        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> GetReturnItemList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReturnRequests))
                return AccessDeniedView();

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareReturnRequestSearchModelAsync(new ReturnRequestSearchModel());

            return View("~/Plugins/Shipping.EcLogistics/Views/ReturnRequests/GetReturnItem.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> GetReturnItemList(ReturnRequestSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReturnRequests))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareReturnItemListModelAsync(searchModel);

            return Json(model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [Route("~/Admin/EcLogistics/EditReturnRequest/{returnRequestId}")]
        [HttpGet]
        public virtual async Task<IActionResult> EditReturnRequest(int returnRequestId)
        {
            return RedirectToAction("Edit", "ReturnRequest", new { id = returnRequestId });
        }

        //  退貨商品收取-更新狀態
        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> Edit(ReturnRequestModel returnRequestModel)
        {

            //try to get a return request with the specified id
            var returnRequest = await _returnRequestService.GetReturnRequestByIdAsync(returnRequestModel.Id);

            if (returnRequestModel.ItemReceived)
            {
                returnRequest.ItemReceivedDate = DateTime.Now;
                returnRequest.ReturnRequestStatusId = 25;
            }
            else
            {
                returnRequest.ItemReceivedDate = null;
            }

            //prepare model
            await _returnRequestService.UpdateReturnRequestAsync(returnRequest);

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareReturnItemListModelAsync(new ReturnRequestSearchModel());

            return Json(model);
        }


        //  退款作業
        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> GetRefundList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReturnRequests))
                return AccessDeniedView();

            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareReturnRequestSearchModelAsync(new ReturnRequestSearchModel());

            return View("~/Plugins/Shipping.EcLogistics/Views/ReturnRequests/Refund.cshtml", model);
        }


        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> GetRefundList(ReturnRequestSearchModel searchModel)
        {

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReturnRequests))
                return await AccessDeniedDataTablesJson();

            var model = await _ecLogisticsModelFactory.PrepareRefundListModelAsync(searchModel);

            return Json(model);
        }

        //  更新退款金額
        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> EditRefund(ReturnRequestModel returnRequestModel)
        {
            //try to get a return request with the specified id
            var returnRequest = await _returnRequestService.GetReturnRequestByIdAsync(returnRequestModel.Id);

            returnRequest.ReturnRequestStatusId = (int)ReturnRequestStatus.ItemsRefunded;
            returnRequest.RefundAmount = returnRequestModel.RefundAmount;
            returnRequest.RefundDate = DateTime.Now;

            //prepare model
            await _returnRequestService.UpdateReturnRequestAsync(returnRequest);
            //raise event       
            await _eventPublisher.PublishAsync(new InvoiceReturnAllowanceEvent(returnRequest));
            //prepare model
            var model = await _ecLogisticsModelFactory.PrepareReturnItemListModelAsync(new ReturnRequestSearchModel());

            return Json(model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> GetReturnNumberSelected(ICollection<int> selectedIds)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReturnRequests))
                return AccessDeniedView();

            if (selectedIds == null || selectedIds.Count == 0)
                return NoContent();

            var requests = await _returnRequestsRepository.GetByIdsAsync(selectedIds.ToList());

            var groupedByOrder = requests.GroupBy(r => _orderService.GetOrderByOrderItemAsync(r.OrderItemId).Result.Id)
                .Select(g => g.ToList()).ToList();

            foreach (var groupedRequests in groupedByOrder)
            {
                try
                {
                    // get return number
                    var returnNum = string.Empty;
                    var order = await _orderService.GetOrderByOrderItemAsync(groupedRequests.FirstOrDefault().OrderItemId);
                    var subType = order.ShippingRateComputationMethodSystemName.Substring(21).ToUpper();
                    var isHome = subType == "TCAT" || subType == "POST";
                    if (isHome)
                    {
                        if (await _ecLogisticsService.ReturnHomeAsync(groupedRequests.ToList()))
                            returnNum = "OK";
                    }
                    else
                    {
                        returnNum = await _ecLogisticsService.ReturnCvsAsync(groupedRequests.ToList(), subType);
                    }

                    foreach (var request in groupedRequests)
                    {
                        //save return number
                        await _ecPayReturnNumberMappingRepository.InsertAsync(new EcPayReturnNumberMapping
                        {
                            ReturnRequestId = request.Id,
                            ReturnNumber = returnNum
                        });

                        //change return request's status 
                        request.ReturnRequestStatus = ReturnRequestStatus.ReturnAuthorized;
                        await _returnRequestService.UpdateReturnRequestAsync(request);
                    }

                    //notify buyer
                    await _ecLogisticsService.SendReturnRequestNumberEmailAsync(!isHome, order, returnNum);
                    await _ecLogisticsService.SendReturnRequestNumberSmsAsync(!isHome, order, returnNum);

                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            return Json(new { Result = true });
        }

        [HttpPost]
        public async Task<IActionResult> ReturnStatusReply(IFormCollection response)
        {
            //TODO: 逆物流狀態回傳
            return Content(response.ToString());
        }

        #endregion
    }
}
