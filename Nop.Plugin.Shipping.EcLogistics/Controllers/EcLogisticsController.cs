using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.EcLogistics.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.DataTables;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

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
            IWorkContext workContext
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

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> GetShippingMethods()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return await AccessDeniedDataTablesJson();

            var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

            var sm = new List<EclShippingMethodsModel>()
            {
                new EclShippingMethodsModel()
                {
                    SubTypeCode = ecLogisticsSettings.UniMartSubTypeCode,
                    Name = ecLogisticsSettings.UniMartName,
                    Description = ecLogisticsSettings.UniMartDescription,
                    Rate = ecLogisticsSettings.UniMartRate,
                    DisplayOrder = ecLogisticsSettings.UniMartDisplayOrder,
                    IsActive = ecLogisticsSettings.UniMartIsActive
                },
                new EclShippingMethodsModel()
                {
                    SubTypeCode = ecLogisticsSettings.FamiSubTypeCode,
                    Name = ecLogisticsSettings.FamiName,
                    Description = ecLogisticsSettings.FamiDescription,
                    Rate = ecLogisticsSettings.FamiRate,
                    DisplayOrder = ecLogisticsSettings.FamiDisplayOrder,
                    IsActive = ecLogisticsSettings.FamiIsActive
                },
                new EclShippingMethodsModel()
                {
                    SubTypeCode = ecLogisticsSettings.HiLifeSubTypeCode,
                    Name = ecLogisticsSettings.HiLifeName,
                    Description = ecLogisticsSettings.HiLifeDescription,
                    Rate = ecLogisticsSettings.HiLifeRate,
                    DisplayOrder = ecLogisticsSettings.HiLifeDisplayOrder,
                    IsActive = ecLogisticsSettings.HiLifeIsActive
                },
                new EclShippingMethodsModel()
                {
                    SubTypeCode = ecLogisticsSettings.UniMartFreezeSubTypeCode,
                    Name = ecLogisticsSettings.UniMartFreezeName,
                    Description = ecLogisticsSettings.UniMartFreezeDescription,
                    Rate = ecLogisticsSettings.UniMartFreezeRate,
                    DisplayOrder = ecLogisticsSettings.UniMartDisplayOrder,
                    IsActive = ecLogisticsSettings.UniMartFreezeIsActive
                },
                //new EclShippingMethodsModel()
                //{
                //    SubTypeCode = ecLogisticsSettings.TcatSubTypeCode,
                //    Name = ecLogisticsSettings.TcatName,
                //    Description = ecLogisticsSettings.TcatDescription,
                //    Rate = ecLogisticsSettings.TcatRate,
                //    DisplayOrder = ecLogisticsSettings.TcatDisplayOrder,
                //    IsActive = ecLogisticsSettings.TcatIsActive
                //},
                //new EclShippingMethodsModel()
                //{
                //    SubTypeCode = ecLogisticsSettings.EcanSubTypeCode,
                //    Name = ecLogisticsSettings.EcanName,
                //    Description = ecLogisticsSettings.EcanDescription,
                //    Rate = ecLogisticsSettings.EcanRate,
                //    DisplayOrder = ecLogisticsSettings.EcanDisplayOrder,
                //    IsActive = ecLogisticsSettings.EcanIsActive
                //},
                new EclShippingMethodsModel()
                {
                    SubTypeCode = ecLogisticsSettings.HomeSubTypeCode,
                    Name = ecLogisticsSettings.HomeName,
                    Description = ecLogisticsSettings.HomeDescription,
                    Rate = ecLogisticsSettings.HomeRate,
                    DisplayOrder = ecLogisticsSettings.HomeDisplayOrder,
                    IsActive = ecLogisticsSettings.HomeIsActive
                }
            };

            return Json(new DataTablesModel() { Data = sm });
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [Route("~/Admin/EcLogistics/EditSubType/{SubTypeCode}")]
        public virtual async Task<IActionResult> EditSubType(string SubTypeCode)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

            var model = new EclShippingMethodsModel();
            switch (SubTypeCode)
            {
                case "UNIMART":
                    model = new EclShippingMethodsModel()
                    {
                        Name = ecLogisticsSettings.UniMartName,
                        Description = ecLogisticsSettings.UniMartDescription,
                        Rate = ecLogisticsSettings.UniMartRate,
                        DisplayOrder = ecLogisticsSettings.UniMartDisplayOrder,
                        IsActive = ecLogisticsSettings.UniMartIsActive
                    };
                    break;
                case "FAMI":
                    model = new EclShippingMethodsModel()
                    {
                        Name = ecLogisticsSettings.FamiName,
                        Description = ecLogisticsSettings.FamiDescription,
                        Rate = ecLogisticsSettings.FamiRate,
                        DisplayOrder = ecLogisticsSettings.FamiDisplayOrder,
                        IsActive = ecLogisticsSettings.FamiIsActive
                    };
                    break;
                case "HILIFE":
                    model = new EclShippingMethodsModel()
                    {
                        Name = ecLogisticsSettings.HiLifeName,
                        Description = ecLogisticsSettings.HiLifeDescription,
                        Rate = ecLogisticsSettings.HiLifeRate,
                        DisplayOrder = ecLogisticsSettings.HiLifeDisplayOrder,
                        IsActive = ecLogisticsSettings.HiLifeIsActive
                    };
                    break;
                case "UNIMARTFREEZE":
                    model = new EclShippingMethodsModel()
                    {
                        Name = ecLogisticsSettings.UniMartFreezeName,
                        Description = ecLogisticsSettings.UniMartFreezeDescription,
                        Rate = ecLogisticsSettings.UniMartFreezeRate,
                        DisplayOrder = ecLogisticsSettings.UniMartFreezeDisplayOrder,
                        IsActive = ecLogisticsSettings.UniMartFreezeIsActive
                    };
                    break;
                //case "TCAT":
                //    model = new EclShippingMethodsModel()
                //    {
                //        Name = ecLogisticsSettings.TcatName,
                //        Description = ecLogisticsSettings.TcatDescription,
                //        Rate = ecLogisticsSettings.TcatRate,
                //        DisplayOrder = ecLogisticsSettings.TcatDisplayOrder,
                //        IsActive = ecLogisticsSettings.TcatIsActive
                //    };
                //    break;
                //case "ECAN":
                //    model = new EclShippingMethodsModel()
                //    {
                //        Name = ecLogisticsSettings.EcanName,
                //        Description = ecLogisticsSettings.EcanDescription,
                //        Rate = ecLogisticsSettings.EcanRate,
                //        DisplayOrder = ecLogisticsSettings.EcanDisplayOrder,
                //        IsActive = ecLogisticsSettings.EcanIsActive
                //    };
                //    break;
                case "HOME":
                    model = new EclShippingMethodsModel()
                    {
                        Name = ecLogisticsSettings.HomeName,
                        Description = ecLogisticsSettings.HomeDescription,
                        Rate = ecLogisticsSettings.HomeRate,
                        DisplayOrder = ecLogisticsSettings.HomeDisplayOrder,
                        IsActive = ecLogisticsSettings.HomeIsActive
                    };
                    break;
            }

            return View("~/Plugins/Shipping.EcLogistics/Views/EditSubType.cshtml", model);
        }

        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Route("~/Admin/EcLogistics/EditSubType/{SubTypeCode}")]
        public virtual async Task<IActionResult> EditSubType(EclShippingMethodsModel model, bool continueEditing)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

                switch (model.SubTypeCode)
                {
                    case "UNIMART":
                        ecLogisticsSettings.UniMartName = model.Name;
                        ecLogisticsSettings.UniMartDescription = model.Description;
                        ecLogisticsSettings.UniMartRate = model.Rate;
                        ecLogisticsSettings.UniMartDisplayOrder = model.DisplayOrder;
                        ecLogisticsSettings.UniMartIsActive = model.IsActive;
                        break;
                    case "FAMI":
                        ecLogisticsSettings.FamiName = model.Name;
                        ecLogisticsSettings.FamiDescription = model.Description;
                        ecLogisticsSettings.FamiRate = model.Rate;
                        ecLogisticsSettings.FamiDisplayOrder = model.DisplayOrder;
                        ecLogisticsSettings.FamiIsActive = model.IsActive;
                        break;
                    case "HILIFE":
                        ecLogisticsSettings.HiLifeName = model.Name;
                        ecLogisticsSettings.HiLifeDescription = model.Description;
                        ecLogisticsSettings.HiLifeRate = model.Rate;
                        ecLogisticsSettings.HiLifeDisplayOrder = model.DisplayOrder;
                        ecLogisticsSettings.HiLifeIsActive = model.IsActive;
                        break;
                    case "UNIMARTFREEZE":
                        ecLogisticsSettings.UniMartFreezeName = model.Name;
                        ecLogisticsSettings.UniMartFreezeDescription = model.Description;
                        ecLogisticsSettings.UniMartFreezeRate = model.Rate;
                        ecLogisticsSettings.UniMartFreezeDisplayOrder = model.DisplayOrder;
                        ecLogisticsSettings.UniMartFreezeIsActive = model.IsActive;
                        break;
                    //case "TCAT":
                    //    ecLogisticsSettings.TcatName = model.Name;
                    //    ecLogisticsSettings.TcatDescription = model.Description;
                    //    ecLogisticsSettings.TcatRate = model.Rate;
                    //    ecLogisticsSettings.TcatDisplayOrder = model.DisplayOrder;
                    //    ecLogisticsSettings.TcatIsActive = model.IsActive;
                    //    break;
                    //case "ECAN":
                    //    ecLogisticsSettings.EcanName = model.Name;
                    //    ecLogisticsSettings.EcanDescription = model.Description;
                    //    ecLogisticsSettings.EcanRate = model.Rate;
                    //    ecLogisticsSettings.EcanDisplayOrder = model.DisplayOrder;
                    //    ecLogisticsSettings.EcanIsActive = model.IsActive;
                    //    break;
                    case "HOME":
                        ecLogisticsSettings.HomeName = model.Name;
                        ecLogisticsSettings.HomeDescription = model.Description;
                        ecLogisticsSettings.HomeRate = model.Rate;
                        ecLogisticsSettings.HomeDisplayOrder = model.DisplayOrder;
                        ecLogisticsSettings.HomeIsActive = model.IsActive;
                        break;
                }

                _settingService.SaveSettingAsync(ecLogisticsSettings);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Shipping.Methods.Updated"));

                return continueEditing ? RedirectToAction("EditSubType", "EcLogistics", model.SubTypeCode) : RedirectToAction("Configure");
            }

            //if we got this far, something failed, redisplay form
            return View("~/Plugins/Shipping.EcLogistics/Views/EditSubType.cshtml", model);
        }


        [HttpPost]
        public async Task<IActionResult> GetCVS()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            var shippingMethod = _genericAttributeService.GetAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, store.Id).Result;

            var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();

            if (shippingMethod.Name == ecLogisticsSettings.UniMartName)
                return Json(new { stc = ecLogisticsSettings.UniMartSubTypeCode });
            





            var cvsNames = new List<string>();
            cvsNames.Add(ecLogisticsSettings.UniMartName);
            cvsNames.Add(ecLogisticsSettings.FamiName);
            cvsNames.Add(ecLogisticsSettings.HiLifeName);
            cvsNames.Add(ecLogisticsSettings.UniMartFreezeName);

            var cvs = false;
            if (cvsNames.Contains(shippingMethod.Name))
                cvs = true;

            return Json(new { cvs = cvs });
        }

        #endregion

    }
}
