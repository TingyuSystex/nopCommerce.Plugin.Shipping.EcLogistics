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

namespace Nop.Plugin.Shipping.EcLogistics.UnimartFreeze
{
    public class EcLogisticsUnimartFreezePlugin : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IRepository<EcPayCvsShippingMethod> _ecPayCvsShippingMethodRepository;

        #endregion

        #region Ctor

        public EcLogisticsUnimartFreezePlugin(ILocalizationService localizationService,
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
        /// 紙箱三邊外尺寸不可大於30x30x45公分，
        /// 重量不可超過10公斤
        /// </summary>
        /// <param name="method"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool CheckShippingLimit(EcPayCvsShippingMethod method, GetShippingOptionRequest request)
        {
            var items = request.Items.Where(x => x.Product.IsShipEnabled && !x.Product.IsFreeShipping && !x.Product.ShipSeparately);
            decimal totalWeight = 0;
            foreach (var item in items)
            {
                totalWeight += item.Product.Weight * Convert.ToDecimal(item.ShoppingCartItem.Quantity);
            }

            if (method.WeightSizeLimit > 0 && method.WeightSizeLimit < totalWeight)
                return false;

            // TODO: 紙箱三邊外尺寸不可大於30x30x45公分，

            return true;
        }

        #endregion

        #region BasePlugin

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/EcLogistics/ConfigureCVS/UNIMARTFREEZE";
        }

        public override async Task InstallAsync()
        {
            // insert UNIMARTFREEZE to EcPayCvsShippingMethod in DB 
            await _ecPayCvsShippingMethodRepository.InsertAsync(new EcPayCvsShippingMethod
            {
                Name = "UNIMARTFREEZE",
                Description = "7-11 超商冷凍交貨便",
                PaymentMethod = "",
                TemperatureType = "L",
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            });

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogisticsUnimartFreeze.Name"] = "7-11 超商冷凍交貨便",
            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            var modelData = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("UNIMARTFREEZE")));
            await _ecPayCvsShippingMethodRepository.DeleteAsync(modelData[0]);

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.EcLogisticsUnimartFreeze");

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
                x.Where(sm => sm.Name.Equals("UNIMARTFREEZE")));
            var shippingOption = model.FirstOrDefault();

            var options = new GetShippingOptionResponse() { ShippingOptions = new List<ShippingOption>() };

            if (CheckShippingLimit(shippingOption, getShippingOptionRequest))
            {
                var option = new ShippingOption()
                {
                    ShippingRateComputationMethodSystemName = PluginDescriptor.SystemName,
                    Rate = shippingOption.Fee,
                    Name = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsUnimartFreeze.Name"),
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

    }
}
