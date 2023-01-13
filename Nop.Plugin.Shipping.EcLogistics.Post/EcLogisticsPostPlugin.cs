using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Tools;
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

namespace Nop.Plugin.Shipping.EcLogistics.Post
{
    public class EcLogisticsPostPlugin : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IRepository<EcPayHomeShippingMethod> _ecPayHomeShippingMethodRepository;
        private readonly IRepository<EcPayHomeShippingFee> _ecPayHomeShippingFeeRepository;

        #endregion

        #region Ctor

        public EcLogisticsPostPlugin(ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper,
            IRepository<EcPayHomeShippingMethod> ecPayHomeShippingMethodRepository,
            IRepository<EcPayHomeShippingFee> ecPayHomeShippingFeeRepository)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _ecPayHomeShippingMethodRepository = ecPayHomeShippingMethodRepository;
            _ecPayHomeShippingFeeRepository = ecPayHomeShippingFeeRepository;
        }

        #endregion
        
        #region BasePlugin

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/EcLogistics/ConfigureHome/POST";
        }

        public override async Task InstallAsync()
        {
            var data = _ecPayHomeShippingMethodRepository.GetAll();
            var existed = "POST".In(data.Select(h => h.Name));
            if (!existed)
            {
                // insert POST to EcPayHomeShippingMethod in DB 
                await _ecPayHomeShippingMethodRepository.InsertAsync(new EcPayHomeShippingMethod
                {
                    Name = "POST",
                    Description = "中華郵政宅配",
                    PaymentMethod = "",
                    IsFixedFee = "N",
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                });
            }

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogisticsPost.Name"] = "中華郵政宅配",
            });

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogisticsPost.Name"] = "Chunghwa Post",
            }, 1);

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            var modelData = await _ecPayHomeShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("POST")));
            if (modelData.Any())
                await _ecPayHomeShippingMethodRepository.DeleteAsync(modelData.FirstOrDefault());

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.EcLogisticsPost");
            
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
            var result = new GetShippingOptionResponse() { ShippingOptions = new List<ShippingOption>() };

            var methodModel = await _ecPayHomeShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("POST")));
            var method = methodModel.FirstOrDefault();

            var items = getShippingOptionRequest.Items.Where(x => x.Product.IsShipEnabled && !x.Product.IsFreeShipping && !x.Product.ShipSeparately);
            var temperatureIdList = items.Select(i => i.Product.ProductTemperatureType).Distinct().ToList();
            // 購物車內商品溫層不統一
            if (temperatureIdList.Count > 1)
                return result;
            var temperatureId = (int)temperatureIdList.FirstOrDefault();
            // 溫層不符
            if (temperatureId == (int)ProductTemperatureType.Low || temperatureId == (int)ProductTemperatureType.Freeze)
                return result;

            //Use fixed fee
            if (method.IsFixedFee == "Y")
            {
                var option = new ShippingOption()
                {
                    ShippingRateComputationMethodSystemName = PluginDescriptor.SystemName,
                    Rate = method.Fee,
                    Name = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsPost.Name"),
                    Description = method.Description,
                    TransitDays = method.TransitDay,
                    IsPickupInStore = false,
                    IsCvsMethod = false,
                    PaymentMethods = method.PaymentMethod
                };
                result.ShippingOptions.Add(option);

                return result;
            }

            //Filter weight
            decimal totalWeight = 0;
            foreach (var item in items)
            {
                totalWeight += item.Product.Weight * Convert.ToDecimal(item.ShoppingCartItem.Quantity);
            }

            var feeList = _ecPayHomeShippingFeeRepository.GetAll();

            foreach (var fee in feeList)
            {
                if (fee.WeightFrom <= totalWeight && totalWeight <= fee.WeightTo)
                {
                    //TODO: filter 本島外島

                    var option = new ShippingOption()
                    {
                        ShippingRateComputationMethodSystemName = PluginDescriptor.SystemName,
                        Rate = fee.Fee,
                        Name = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsPost.Name"),
                        Description = method.Description,
                        TransitDays = method.TransitDay,
                        IsPickupInStore = false,
                        IsCvsMethod = false,
                        PaymentMethods = method.PaymentMethod
                    };
                    result.ShippingOptions.Add(option);
                }
            }

            return result;
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
