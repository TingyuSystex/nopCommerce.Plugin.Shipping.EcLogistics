using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.EcLogistics.Unimart
{
    public class EcLogisticsUnimartPlugin : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IRepository<EcPayCvsShippingMethod> _ecPayCvsShippingMethodRepository;

        #endregion

        #region Ctor

        public EcLogisticsUnimartPlugin(ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper,
            IRepository<EcPayCvsShippingMethod> ecPayCvsShippingMethodRepository)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _ecPayCvsShippingMethodRepository = ecPayCvsShippingMethodRepository;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// 檢查是否符合配送條件
        /// 1.最長邊 ≦ 45cm
        /// 2.長+寬+高，合計 ≦ 105cm
        /// 3.重量 ≤ 10公斤
        /// </summary>
        /// <param name="method"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool CheckShippingLimit(EcPayCvsShippingMethod method, GetShippingOptionRequest request)
        {
            var items = request.Items.Where(x => x.Product.IsShipEnabled && !x.Product.IsFreeShipping && !x.Product.ShipSeparately);
            decimal totalSize = 0;
            decimal totalWeight = 0;
            foreach (var item in items)
            {
                totalSize += (item.Product.Length + item.Product.Width + item.Product.Height) * Convert.ToDecimal(item.ShoppingCartItem.Quantity);
                totalWeight += item.Product.Weight * Convert.ToDecimal(item.ShoppingCartItem.Quantity);
            }

            if (method.SizeLimit > 0 && method.SizeLimit < totalSize)
                return false;

            if (method.WeightSizeLimit > 0 && method.WeightSizeLimit < totalWeight)
                return false;

            // TODO: 最長邊 ≦ 45cm

            return true;
        }

        #endregion

        #region BasePlugin

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/EcLogistics/ConfigureCVS/UNIMART";
        }

        public override async Task InstallAsync()
        {
            // insert UNIMART to EcPayCvsShippingMethod in DB 
            await _ecPayCvsShippingMethodRepository.InsertAsync(new EcPayCvsShippingMethod
            {
                Name = "UNIMART",
                Description = "7-11 超商取貨",
                PaymentMethod = "",
                TemperatureType = "H",
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            });

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogisticsUnimart.Name"] = "7-11 超商取貨",
            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            var modelData = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("UNIMART")));
            await _ecPayCvsShippingMethodRepository.DeleteAsync(modelData[0]);

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.EcLogisticsUnimart");

            await base.UninstallAsync();
        }

        #endregion

        #region Shipping Provider

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the represents a response of getting shipping rate options
        /// </returns>
        public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest)
        {
            var model = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("UNIMART")));
            var shippingOption = model.FirstOrDefault();

            var options = new GetShippingOptionResponse() { ShippingOptions = new List<ShippingOption>() };

            if (CheckShippingLimit(shippingOption, getShippingOptionRequest))
            {
                var option = new ShippingOption()
                {
                    ShippingRateComputationMethodSystemName = PluginDescriptor.SystemName,
                    Rate = shippingOption.Fee,
                    Name = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsUnimart.Name"),
                    Description = shippingOption.Description,
                    TransitDays = shippingOption.TransitDay,
                    IsPickupInStore = false,
                    IsCvsMethod = true,
                    PaymentMethods = shippingOption.PaymentMethod
                };
                options.ShippingOptions.Add(option);
            }

            return options;
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the fixed shipping rate; or null in case there's no fixed shipping rate
        /// </returns>
        public Task<decimal?> GetFixedRateAsync(GetShippingOptionRequest getShippingOptionRequest)
        {
            return Task.FromResult<decimal?>(null);
        }

        /// <summary>
        /// Get associated shipment tracker
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment tracker
        /// </returns>
        public Task<IShipmentTracker> GetShipmentTrackerAsync()
        {
            return Task.FromResult<IShipmentTracker>(null);
        }

        #endregion

        //#region Shipping Provider

        ///// <summary>
        /////  Gets available shipping options
        ///// </summary>
        ///// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// The task result contains the represents a response of getting shipping rate options
        ///// </returns>
        //public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest)
        //{
        //    if (getShippingOptionRequest == null)
        //        throw new ArgumentNullException(nameof(getShippingOptionRequest));

        //    if (!getShippingOptionRequest.Items?.Any() ?? true)
        //        return new GetShippingOptionResponse { Errors = new[] { "No shipment items" } };

        //    if (getShippingOptionRequest.ShippingAddress?.CountryId == null)
        //        return new GetShippingOptionResponse { Errors = new[] { "Shipping address is not set" } };

        //    var ecLogisticsSettings = await _settingService.LoadSettingAsync<EcLogisticsSettings>();
        //    var response = new GetShippingOptionResponse();

        //    if (ecLogisticsSettings.UniMartIsActive)
        //        response.ShippingOptions.Add(new ShippingOption()
        //        {
        //            Name = ecLogisticsSettings.UniMartName,
        //            Description = ecLogisticsSettings.UniMartDescription,
        //            Rate = ecLogisticsSettings.UniMartRate,
        //            DisplayOrder = ecLogisticsSettings.UniMartDisplayOrder
        //        });
        //    if (ecLogisticsSettings.FamiIsActive)
        //        response.ShippingOptions.Add(new ShippingOption()
        //        {
        //            Name = ecLogisticsSettings.FamiName,
        //            Description = ecLogisticsSettings.FamiDescription,
        //            Rate = ecLogisticsSettings.FamiRate,
        //            DisplayOrder = ecLogisticsSettings.FamiDisplayOrder
        //        });
        //    if (ecLogisticsSettings.HiLifeIsActive)
        //        response.ShippingOptions.Add(new ShippingOption()
        //        {
        //            Name = ecLogisticsSettings.HiLifeName,
        //            Description = ecLogisticsSettings.HiLifeDescription,
        //            Rate = ecLogisticsSettings.HiLifeRate,
        //            DisplayOrder = ecLogisticsSettings.HiLifeDisplayOrder
        //        });
        //    if (ecLogisticsSettings.UniMartFreezeIsActive)
        //        response.ShippingOptions.Add(new ShippingOption()
        //        {
        //            Name = ecLogisticsSettings.UniMartFreezeName,
        //            Description = ecLogisticsSettings.UniMartFreezeDescription,
        //            Rate = ecLogisticsSettings.UniMartFreezeRate,
        //            DisplayOrder = ecLogisticsSettings.UniMartFreezeDisplayOrder
        //        });
        //    //if (ecLogisticsSettings.TcatIsActive)
        //    //    response.ShippingOptions.Add(new ShippingOption()
        //    //    {
        //    //        Name = ecLogisticsSettings.TcatName,
        //    //        Description = ecLogisticsSettings.TcatDescription,
        //    //        Rate = ecLogisticsSettings.TcatRate,
        //    //        DisplayOrder = ecLogisticsSettings.TcatDisplayOrder
        //    //    });
        //    //if (ecLogisticsSettings.EcanIsActive)
        //    //    response.ShippingOptions.Add(new ShippingOption()
        //    //    {
        //    //        Name = ecLogisticsSettings.EcanName,
        //    //        Description = ecLogisticsSettings.EcanDescription,
        //    //        Rate = ecLogisticsSettings.EcanRate,
        //    //        DisplayOrder = ecLogisticsSettings.EcanDisplayOrder
        //    //    });
        //    if (ecLogisticsSettings.HomeIsActive)
        //        response.ShippingOptions.Add(new ShippingOption()
        //        {
        //            Name = ecLogisticsSettings.HomeName,
        //            Description = ecLogisticsSettings.HomeDescription,
        //            Rate = ecLogisticsSettings.HomeRate,
        //            DisplayOrder = ecLogisticsSettings.HomeDisplayOrder
        //        });

        //    return response;
        //}

        ///// <summary>
        ///// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        ///// </summary>
        ///// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// The task result contains the fixed shipping rate; or null in case there's no fixed shipping rate
        ///// </returns>
        //public Task<decimal?> GetFixedRateAsync(GetShippingOptionRequest getShippingOptionRequest)
        //{
        //    return Task.FromResult<decimal?>(null);
        //}

        ///// <summary>
        ///// Get associated shipment tracker
        ///// </summary>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// The task result contains the shipment tracker
        ///// </returns>
        //public Task<IShipmentTracker> GetShipmentTrackerAsync()
        //{
        //    return Task.FromResult<IShipmentTracker>(null);
        //}

        //#endregion
    }
}
