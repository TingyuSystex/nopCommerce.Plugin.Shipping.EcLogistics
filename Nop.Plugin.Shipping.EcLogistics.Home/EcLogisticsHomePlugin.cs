using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Plugin.Shipping.EcLogistics.Home.Domain;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.EcLogistics.Home
{
    public class EcLogisticsHomePlugin : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IRepository<EcPayHomeShippingMethod> _ecPayHomeShippingMethodRepository;
        private readonly IRepository<EcPayHomeShippingFee> _ecPayHomeShippingFeeRepository;

        #endregion

        #region Ctor

        public EcLogisticsHomePlugin(ILocalizationService localizationService,
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
            return _webHelper.GetStoreLocation() + "Admin/EcLogisticsHome/Configure";
        }

        public override async Task InstallAsync()
        {
            // insert HOME to EcPayHomeShippingMethod in DB 
            await _ecPayHomeShippingMethodRepository.InsertAsync(new EcPayHomeShippingMethod
            {
                Name = "HOME",
                Description = "宅配",
                PaymentMethod = "",
                IsFixedFee = "N",
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            });

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogisticsHome.Name"] = "宅配",
                ["Plugins.Shipping.EcLogisticsHome.ConfigFee"] = "費用設定",
                ["Plugins.Shipping.EcLogisticsHome.Fields.IsFixedFee"] = "固定運費",
                ["Plugins.Shipping.EcLogisticsHome.Fields.IsFixedFee.Hint"] = "是否使用固定運費",

                ["Plugins.Shipping.EcLogisticsHome.Fields.SizeFrom"] = "材積(長+寬+高cm)起",
                ["Plugins.Shipping.EcLogisticsHome.Fields.SizeFrom.Hint"] = "材積大於",
                ["Plugins.Shipping.EcLogisticsHome.Fields.SizeTo"] = "材積(長+寬+高cm)迄",
                ["Plugins.Shipping.EcLogisticsHome.Fields.SizeTo.Hint"] = "材積小於等於",
                ["Plugins.Shipping.EcLogisticsHome.Fields.Fee"] = "本島費用",
                ["Plugins.Shipping.EcLogisticsHome.Fields.Fee.Hint"] = "本島內互寄費用",
                ["Plugins.Shipping.EcLogisticsHome.Fields.ForeignFee"] = "外島費用",
                ["Plugins.Shipping.EcLogisticsHome.Fields.ForeignFee.Hint"] = "本島寄外道費用。不提供離島寄離島，以及離島島內互寄。",
                ["Plugins.Shipping.EcLogisticsHome.Fields.HolidayExtraFee"] = "假日加價",
                ["Plugins.Shipping.EcLogisticsHome.Fields.HolidayExtraFee.Hint"] = "假日、節日宅配運費本島與離島每件加價費用(例：農曆新年、中秋、端午節及各節前５個工作天取貨的貨品，每件皆須加價10元)",

                ["Plugins.Shipping.EcLogisticsHome.ShippingMethod.Addrecord"] = "新增費用設定",
                ["Plugins.Shipping.EcLogisticsHome.ShippingMethod.AddTitle"] = "新增宅配運費",
                ["Plugins.Shipping.EcLogisticsHome.ShippingMethod.EditTitle"] = "編輯宅配運費",

                ["Plugins.Shipping.EcLogisticsHome.ShippingMethod.SizeError"] = "材積迄需大於起",
                ["Plugins.Shipping.EcLogisticsHome.ShippingMethod.SizeRepeat"] = "同溫層材積範圍不能與現有範圍重複",

            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            var modelData = await _ecPayHomeShippingMethodRepository.GetAllAsync(x =>
                x.Where(sm => sm.Name.Equals("HOME")));
            await _ecPayHomeShippingMethodRepository.DeleteAsync(modelData[0]);

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.EcLogisticsHome");

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
                x.Where(sm => sm.Name.Equals("HOME")));
            var method = methodModel.FirstOrDefault();

            //Use fixed fee
            if (method.IsFixedFee == "Y")
            {
                var option = new ShippingOption()
                {
                    ShippingRateComputationMethodSystemName = PluginDescriptor.SystemName,
                    Rate = method.Fee,
                    Name = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsHome.Name"),
                    Description = method.Description,
                    TransitDays = method.TransitDay,
                    IsPickupInStore = false,
                    IsCvsMethod = false,
                    PaymentMethods = method.PaymentMethod
                };
                result.ShippingOptions.Add(option);

                return result;
            }
            
            //Filter size
            var items = getShippingOptionRequest.Items.Where(x => x.Product.IsShipEnabled && !x.Product.IsFreeShipping && !x.Product.ShipSeparately);
            decimal totalSize = 0;
            foreach (var item in items)
            {
                totalSize += (item.Product.Length + item.Product.Width + item.Product.Height) * Convert.ToDecimal(item.ShoppingCartItem.Quantity);
            }

            var feeList = _ecPayHomeShippingFeeRepository.GetAll();

            foreach (var fee in feeList)
            {
                if (fee.SizeFrom <= totalSize && totalSize <= fee.SizeTo)
                {
                    //TODO: filter 溫層
                    if (fee.TemperatureType == "L")
                    {
                        continue;
                    }
                    //TODO: filter 本島外島
                    //TODO: 假日加價

                    var option = new ShippingOption()
                    {
                        ShippingRateComputationMethodSystemName = PluginDescriptor.SystemName,
                        Rate = fee.Fee,
                        Name = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogisticsHome.Name"),
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
