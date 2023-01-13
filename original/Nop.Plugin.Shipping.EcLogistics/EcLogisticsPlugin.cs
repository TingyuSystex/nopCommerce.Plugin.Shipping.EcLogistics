using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Services.Cms;
using Nop.Services.Localization;
using Nop.Services.Plugins;

namespace Nop.Plugin.Shipping.EcLogistics
{
    public class EcLogisticsPlugin : BasePlugin, IWidgetPlugin
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;

        public bool HideInWidgetList => false;

        #endregion

        #region Ctor

        public EcLogisticsPlugin(ILocalizationService localizationService,
            IWebHelper webHelper)
        {
            _localizationService = localizationService;
            _webHelper = webHelper;
        }

        #endregion

        #region BasePlugin

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/EcLogistics/Configure";
        }

        public override async Task InstallAsync()
        {
            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogistics.Configuration.Title"] = "綠界物流整合API",
                ["Plugins.Shipping.EcLogistics.Configuration.Instructions"] = @"
                    <p>
	                    註冊綠界會員（<a href=""https://member.ecpay.com.tw/MemberReg/MemberRegister?back=N"" target=""_blank"">
                        https://member.ecpay.com.tw/MemberReg/MemberRegister?back=N</a>）<br />
                        取得MerchantID及HashKey、HashIV：廠商後台 → 系統開發管理 → 系統介接設定 <br />
                        廠商後台：<a href=""https://vendor.ecpay.com.tw/"" target=""_blank"">https://vendor.ecpay.com.tw/</a> <br />
	                    <br />
                    </p>",

                ["Plugins.Shipping.EcLogistics.Configuration.CreateTestData"] = "測試標籤資料產生",
                ["Plugins.Shipping.EcLogistics.Configuration.CreateTestData.Instructions"] = @"
                    <span>
                        申請全家及7-ELEVEN超商B2C物流的廠商， <b>需要測標通過後才可建立B2C物流訂單</b><br/>
                        產生的測試標籤所需要的測試訂單資訊可於<a href=""https://vendor.ecpay.com.tw/"" target=""_blank"">廠商管理後台</a>列印 <br />
                        注意事項：
                    </span>
                        <ol>
                          <li>請務必使用雷射印表機列印標籤，並用「信封袋寄送至各物流中心進行標籤刷讀，如不符合標準，日後出貨商品不予驗收，以退貨處理。以下地址僅供測標寄送
                            <ul>
                              <li>全家標籤寄送地址：335桃園市大溪區新光東路76巷22-2號。日翊文化電子商務部-EC測標收。</li>
                              <li>7-ELEVEN標籤寄送地址：238新北市樹林區佳園路二段70-1號。大智通文化行銷-EC驗收組收。</li>
                              <li>7-ELEVEN B2C冷凍店取標籤寄送地址：303新竹縣湖口鄉八德路三段30之1號。統昶物流中心-EC冷凍店取測標收。</li>
                            </ul>
                          </li>
                          <li>7-ELEVEN訂單測標資料建立後，請於D+5日14:00前列印，超過便無法列印</li>
                          <li>全家訂單建立後，請於6日內列印，超過便無法列印</li>
                          <li>測標結果查詢：
                            <ul>
                              <li>全家：測標結果請務必點擊「廠商管理後台」 → 物流管理 → 對帳查詢【 一段標查詢 】，查詢按鈕後，若測標成功，方可正常建立訂單。</li>
                              <li>7-ELEVEN：測標結果由綠界科技客服通知，可在「廠商管理後台」 → 廠商專區 → 廠商基本資料 → 物流資訊 → 測標結果查詢， 若測標成功，方可正常建立訂單 。</li>
                            </ul>
                          </li>
                        </ol>
                        需先儲存上方API欄位資訊後再進行測試 <br /><br />",

                ["Plugins.Shipping.EcLogistics.Configuration.Test"] = "產生測試標籤",
                ["Plugins.Shipping.EcLogistics.Configuration.Test.Unimart"] = "7-ELEVEN",
                ["Plugins.Shipping.EcLogistics.Configuration.Test.UnimartFreeze"] = "7-ELEVEN冷凍店取",
                ["Plugins.Shipping.EcLogistics.Configuration.Test.Fami"] = "全家",

                ["Plugins.Shipping.EcLogistics.Fields.UseSandbox"] = "使用測試環境",
                ["Plugins.Shipping.EcLogistics.Fields.MerchantId"] = "MerchantID",
                ["Plugins.Shipping.EcLogistics.Fields.MerchantId.Hint"] = "輸入廠商(會員)編號",
                ["Plugins.Shipping.EcLogistics.Fields.HashKey"] = "HashKey",
                ["Plugins.Shipping.EcLogistics.Fields.HashKey.Hint"] = "物流介接的HashKey",
                ["Plugins.Shipping.EcLogistics.Fields.HashIV"] = "HashIV",
                ["Plugins.Shipping.EcLogistics.Fields.HashIV.Hint"] = "物流介接的HashIV",
                ["Plugins.Shipping.EcLogistics.Fields.PlatformId"] = "特約合作平台商代號",
                ["Plugins.Shipping.EcLogistics.Fields.PlatformId.Hint"] = "由綠界科技提供此參數為專案合作的平台商使用，一般廠商介接請放空值。若為專案合作的平台商使用時，MerchantID請帶賣家所綁定的MerchantID。",
                
                ["Plugins.Shipping.EcLogistics.Fields.MerchantId.Required"] = "MerchantID必填",
                ["Plugins.Shipping.EcLogistics.Fields.HashKey.Required"] = "HashKey必填",
                ["Plugins.Shipping.EcLogistics.Fields.HashIV.Required"] = "HashIV必填",

                ["Plugins.Shipping.EcLogistics.ShippingMethod.EditTitle"] = "編輯配送方法",
                ["Plugins.Shipping.EcLogistics.Fields.Name"] = "名稱",
                ["Plugins.Shipping.EcLogistics.Fields.Name.Hint"] = "系統代號",
                ["Plugins.Shipping.EcLogistics.Fields.Description"] = "描述",
                ["Plugins.Shipping.EcLogistics.Fields.Description.Hint"] = "顯示於結帳頁面之配送方法描述",
                ["Plugins.Shipping.EcLogistics.Fields.PaymentMethod"] = "付款方式",
                ["Plugins.Shipping.EcLogistics.Fields.PaymentMethod.Hint"] = "配送方法適用付款方式",
                ["Plugins.Shipping.EcLogistics.Fields.TemperatureType"] = "適用溫層",
                ["Plugins.Shipping.EcLogistics.Fields.TemperatureType.Hint"] = "配送方法適用溫層",
                ["Plugins.Shipping.EcLogistics.Fields.TemperatureType.H"] = "常溫",
                ["Plugins.Shipping.EcLogistics.Fields.TemperatureType.L"] = "低溫",
                ["Plugins.Shipping.EcLogistics.Fields.LengthLimit"] = "單邊長度限制",
                ["Plugins.Shipping.EcLogistics.Fields.LengthLimit.Hint"] = "單邊長度最長限制(cm)",
                ["Plugins.Shipping.EcLogistics.Fields.SizeLimit"] = "材積(長+寬+高)",
                ["Plugins.Shipping.EcLogistics.Fields.SizeLimit.Hint"] = "長+寬+高(cm)",
                ["Plugins.Shipping.EcLogistics.Fields.WeightSizeLimit"] = "重量限制",
                ["Plugins.Shipping.EcLogistics.Fields.WeightSizeLimit.Hint"] = "重量限制(kg)",
                ["Plugins.Shipping.EcLogistics.Fields.Fee"] = "運費",
                ["Plugins.Shipping.EcLogistics.Fields.Fee.Hint"] = "配送運費",
                ["Plugins.Shipping.EcLogistics.Fields.TransitDay"] = "配送天數",
                ["Plugins.Shipping.EcLogistics.Fields.TransitDay.Hint"] = "配送所需天數，0表示結帳不顯示",
                ["Plugins.Shipping.EcLogistics.Fields.Description.Required"] = "配送方法描述必填",
                ["Plugins.Shipping.EcLogistics.Fields.Description.LengthError"] = "配送方法描述長度需小於100",

                ["Plugins.Shipping.EcLogistics.SelectCvsStore"] = "選擇超商門市",
                ["Plugins.Shipping.EcLogistics.SelectOther"] = "選擇其他超商門市",
                ["Plugins.Shipping.EcLogistics.SelectStore"] = "選擇門市"
            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.EcLogistics");

            await base.UninstallAsync();
        }

        #endregion

        #region Widget

        /// <summary>
        /// Gets a name of a view component for displaying widget
        /// </summary>
        /// <param name="widgetZone">Name of the widget zone</param>
        /// <returns>View component name</returns>
        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "EcLogistics";
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the widget zones
        /// </returns>
        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string> { "checkout_shipping_address_cvs" });
        }

        #endregion
    }
}
