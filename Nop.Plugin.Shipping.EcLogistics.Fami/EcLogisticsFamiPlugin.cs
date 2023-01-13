using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.EcLogistics.Fami
{
    public class EcLogisticsFamiPlugin : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IRepository<EcPayCvsShippingMethod> _ecPayCvsShippingMethodRepository;

        #endregion

        #region Ctor

        public EcLogisticsFamiPlugin(ILocalizationService localizationService,
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
        /// 1.長+寬+高≦105公分(材積≦45cm*30cm*30cm)
        /// 2.單邊最長長度≦45公分，其他兩邊需≦30公分
        /// 3.總重量5公斤以內
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

            // TODO: 單邊最長長度≦45公分，其他兩邊需≦30公分

            return true;
        }

        #endregion

        #region BasePlugin

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/EcLogistics/ConfigureCVS/FAMI";
        }

        public override async Task InstallAsync()
        {
            var data = _ecPayCvsShippingMethodRepository.GetAll();
            var existed = "FAMI".In(data.Select(cvs => cvs.Name));
            if (!existed)
            {
                // insert FAMI to EcPayCvsShippingMethod in DB 
                await _ecPayCvsShippingMethodRepository.InsertAsync(new EcPayCvsShippingMethod
                {
                    Name = "FAMI",
                    Description = "全家 超商取貨",
                    PaymentMethod = "",
                    TemperatureTypeId = (int)ProductTemperatureType.Normal,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                });
            }

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogisticsFami.Name"] = "全家 超商取貨",
            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            var modelData = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("FAMI")));
            if (modelData.Any()) 
                await _ecPayCvsShippingMethodRepository.DeleteAsync(modelData[0]);

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.EcLogisticsFami");

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
                x.Where(sm => sm.Name.Equals("FAMI")));
            var shippingOption = model.FirstOrDefault();

            var options = new GetShippingOptionResponse() { ShippingOptions = new List<ShippingOption>() };

            var items = getShippingOptionRequest.Items.Where(x => x.Product.IsShipEnabled && !x.Product.IsFreeShipping && !x.Product.ShipSeparately);
            var temperatureIdList = items.Select(i => i.Product.ProductTemperatureType).Distinct().ToList();
            // 購物車內商品溫層不統一
            if (temperatureIdList.Count > 1)
                return options;
            var temperatureId = (int)temperatureIdList.FirstOrDefault();
            // 溫層不符
            if (temperatureId == (int)ProductTemperatureType.Low || temperatureId == (int)ProductTemperatureType.Freeze)
                return options;

            if (CheckShippingLimit(shippingOption, getShippingOptionRequest))
            {
                var option = new ShippingOption()
                {
                    ShippingRateComputationMethodSystemName = PluginDescriptor.SystemName,
                    Rate = shippingOption.Fee,
                    Name = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsFami.Name"),
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
