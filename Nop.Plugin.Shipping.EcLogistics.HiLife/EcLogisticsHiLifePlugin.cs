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

namespace Nop.Plugin.Shipping.EcLogistics.HiLife
{
    public class EcLogisticsHiLifePlugin : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IRepository<EcPayCvsShippingMethod> _ecPayCvsShippingMethodRepository;

        #endregion

        #region Ctor

        public EcLogisticsHiLifePlugin(ILocalizationService localizationService,
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

        #region BasePlugin

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/EcLogistics/ConfigureCVS/HILIFE";
        }

        public override async Task InstallAsync()
        {
            // insert HILIFE to EcPayCvsShippingMethod in DB 
            await _ecPayCvsShippingMethodRepository.InsertAsync(new EcPayCvsShippingMethod
            {
                Name = "HILIFE",
                Description = "萊爾富 超商取貨",
                PaymentMethod = "",
                TemperatureType = "H",
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            });

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogisticsHiLife.Name"] = "萊爾富 超商取貨",
            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            var modelData = await _ecPayCvsShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("HILIFE")));
            await _ecPayCvsShippingMethodRepository.DeleteAsync(modelData[0]);

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.EcLogisticsHiLife");

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
                x.Where(sm => sm.Name.Equals("HILIFE")));
            var shippingOption = model.FirstOrDefault();

            var options = new GetShippingOptionResponse() { ShippingOptions = new List<ShippingOption>() };

            var option = new ShippingOption()
            {
                ShippingRateComputationMethodSystemName = PluginDescriptor.SystemName,
                Rate = shippingOption.Fee,
                Name = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsHiLife.Name"),
                Description = shippingOption.Description,
                TransitDays = shippingOption.TransitDay,
                IsPickupInStore = false
            };
            options.ShippingOptions.Add(option);

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
