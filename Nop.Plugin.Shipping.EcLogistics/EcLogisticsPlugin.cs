using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using Nop.Web.Framework;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Shipping.EcLogistics
{
    public class EcLogisticsPlugin : BasePlugin, IWidgetPlugin, IShippingRateComputationMethod, IAdminMenuPlugin
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IShippingPluginManager _shippingPluginManager;
        private readonly IPermissionService _permissionService;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IRepository<EmailAccount> _emailAccountRepository;


        public bool HideInWidgetList => false;

        #endregion

        #region Ctor

        public EcLogisticsPlugin(ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper,
            IShippingPluginManager shippingPluginManager,
            IPermissionService permissionService,
            IMessageTemplateService messageTemplateService,
            IRepository<EmailAccount> emailAccountRepository
            )
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _shippingPluginManager = shippingPluginManager;
            _permissionService = permissionService;
            _messageTemplateService = messageTemplateService;
            _emailAccountRepository = emailAccountRepository;
        }

        #endregion

        #region BasePlugin

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/EcLogistics/Configure";
        }

        public override async Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new EcLogisticsSettings());

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["plugins.friendlyname.shipping.eclogistics"] = "綠界物流(EC Logistics)",
                //Core
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

                //子物流
                ["Plugins.Shipping.EcLogistics.ShippingMethod.EditTitle"] = "編輯配送方法",
                ["Plugins.Shipping.EcLogistics.Fields.Name"] = "名稱",
                ["Plugins.Shipping.EcLogistics.Fields.Name.Hint"] = "系統代號",
                ["Plugins.Shipping.EcLogistics.Fields.Description"] = "描述",
                ["Plugins.Shipping.EcLogistics.Fields.Description.Hint"] = "顯示於結帳頁面之配送方法描述",
                ["Plugins.Shipping.EcLogistics.Fields.PaymentMethod"] = "付款方式",
                ["Plugins.Shipping.EcLogistics.Fields.PaymentMethod.Hint"] = "配送方法適用付款方式",
                ["Plugins.Shipping.EcLogistics.Fields.TemperatureType"] = "適用溫層",
                ["Plugins.Shipping.EcLogistics.Fields.TemperatureType.Hint"] = "配送方法適用溫層",
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

                //前端結帳
                ["Plugins.Shipping.EcLogistics.ReceiverFirstName"] = "收件人 名",
                ["Plugins.Shipping.EcLogistics.ReceiverLastName"] = "收件人 姓",
                ["Plugins.Shipping.EcLogistics.ReceiverCellPhone"] = "收件人 手機",
                ["Plugins.Shipping.EcLogistics.ReceiverCellPhone.Invalid"] = "收件人手機格是須為長度10碼且09開頭",
                ["Plugins.Shipping.EcLogistics.SelectCvsStore"] = "選擇超商門市",
                ["Plugins.Shipping.EcLogistics.SelectOther"] = "選擇其他超商門市",
                ["Plugins.Shipping.EcLogistics.SelectStore"] = "選擇門市",

                //Admin menu functions
                ["Plugins.Shipping.EcLogistics.API"] = "託運資訊",
                ["Plugins.Shipping.EcLogistics.Create"] = "取得追蹤碼",
                ["Plugins.Shipping.EcLogistics.Create.Instruction"] = "勾選欲取得物流追蹤碼出貨單後，批次取號。成功取號後請至列印託運單查看。",
                ["Plugins.Shipping.EcLogistics.Print"] = "列印託運單",
                ["Plugins.Shipping.EcLogistics.Print.Instruction"] = "7-11、全家及萊爾富，因各家列印一段標格式不同，不可與其他超商一段標混合列印使用。批次選取列印請查詢篩選同超商之出貨單。",
                ["Plugins.Shipping.EcLogistics.Create.Selected"] = "取得追蹤碼(已選取)",
                ["Plugins.Shipping.EcLogistics.Print.Selected"] = "列印託運單(已選取)",
                ["Plugins.Shipping.EcLogistics.Print.ShippingMethodAlert"] = "配送方式不可選擇全部",
                ["Plugins.Shipping.EcLogistics.GetReturnNumber"] = "取得退貨編號",
                ["Plugins.Shipping.EcLogistics.GetReturnNumber.Instruction"] = "勾選多筆批次取得退貨編號，取號後即同意退貨並自動發送退貨編號通知買家。同一筆訂單商品退貨需同時勾選該訂單所有商品一次取號，若同訂單之退貨商品分開取號會分開產生退貨編號，即分開退貨。",
                ["Plugins.Shipping.EcLogistics.GetReturnNumber.Selected"] = "取得退貨編號(已選取)",

                //  退貨商品收取
                ["Plugins.Shipping.EcLogistics.GetReturnItem"] = "退貨商品收取",

                //  退款作業
                ["Plugins.Shipping.EcLogistics.Refund"] = "退款作業",

                //宅配
                ["Plugins.Shipping.EcLogistics.Home.Name"] = "宅配",
                ["Plugins.Shipping.EcLogistics.Home.ConfigFee"] = "費用設定",
                ["Plugins.Shipping.EcLogistics.Home.Fields.IsFixedFee"] = "固定運費",
                ["Plugins.Shipping.EcLogistics.Home.Fields.IsFixedFee.Hint"] = "是否使用固定運費",
                                                                
                ["Plugins.Shipping.EcLogistics.Home.Fields.SizeFrom"] = "材積(長+寬+高cm)起",
                ["Plugins.Shipping.EcLogistics.Home.Fields.SizeFrom.Hint"] = "材積大於",
                ["Plugins.Shipping.EcLogistics.Home.Fields.SizeTo"] = "材積(長+寬+高cm)迄",
                ["Plugins.Shipping.EcLogistics.Home.Fields.SizeTo.Hint"] = "材積小於等於",
                ["Plugins.Shipping.EcLogistics.Home.Fields.WeightFrom"] = "重量(kg)起",
                ["Plugins.Shipping.EcLogistics.Home.Fields.WeightFrom.Hint"] = "重量大於",
                ["Plugins.Shipping.EcLogistics.Home.Fields.WeightTo"] = "重量(kg)迄",
                ["Plugins.Shipping.EcLogistics.Home.Fields.WeightTo.Hint"] = "重量小於等於",
                ["Plugins.Shipping.EcLogistics.Home.Fields.Fee"] = "本島費用",
                ["Plugins.Shipping.EcLogistics.Home.Fields.Fee.Hint"] = "本島內互寄費用",
                ["Plugins.Shipping.EcLogistics.Home.Fields.ForeignFee"] = "外島費用",
                ["Plugins.Shipping.EcLogistics.Home.Fields.ForeignFee.Hint"] = "本島寄外道費用。不提供離島寄離島，以及離島島內互寄。",
                ["Plugins.Shipping.EcLogistics.Home.Fields.HolidayExtraFee"] = "假日加價",
                ["Plugins.Shipping.EcLogistics.Home.Fields.HolidayExtraFee.Hint"] = "假日、節日宅配運費本島與離島每件加價費用(例：農曆新年、中秋、端午節及各節前５個工作天取貨的貨品，每件皆須加價10元)",
                ["Plugins.Shipping.EcLogistics.Home.Fields.ScheduledPickupTime"] = "預定取件時段",
                ["Plugins.Shipping.EcLogistics.Home.Fields.ScheduledPickupTime.Hint"] = "宅配物流商預定取貨的時段",
                ["Plugins.Shipping.EcLogistics.Home.Fields.ScheduledPickupTime.NoLimit"] = "不限時",
                                                                
                ["Plugins.Shipping.EcLogistics.Home.ShippingMethod.AddRecord"] = "新增費用設定",
                ["Plugins.Shipping.EcLogistics.Home.ShippingMethod.AddTitle"] = "新增宅配運費",
                ["Plugins.Shipping.EcLogistics.Home.ShippingMethod.EditTitle"] = "編輯宅配運費",
                                                                
                ["Plugins.Shipping.EcLogistics.Home.ShippingMethod.SizeError"] = "材積迄需大於起",
                ["Plugins.Shipping.EcLogistics.Home.ShippingMethod.SizeRepeat"] = "同溫層材積範圍不能與現有範圍重複",
                ["Plugins.Shipping.EcLogistics.Home.ShippingMethod.WeightError"] = "重量迄需大於起",
                ["Plugins.Shipping.EcLogistics.Home.ShippingMethod.WeightRepeat"] = "重量範圍不能與現有範圍重複",

                //退貨簡訊
                ["Plugins.Shipping.EcLogistics.ReturnRequest.Sms.Cvs"] = "您的退貨申請已通過，訂單編號：{0}，超商通路：{1}，退貨代碼：{2}。超商退貨請至原超商通路，操作機台輸入退貨代碼，將退貨商品包裝妥當後，列印單據並將包裹交給門市人員。",
                ["Plugins.Shipping.EcLogistics.ReturnRequest.Sms.Home"] = "您的退貨申請已通過，訂單編號：{0}，配送方法：{1}。宅配退貨請將退貨商品包裝妥當後，等待物流人員至取件地址收貨。",
                
            });

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogistics.Configuration.Title"] = "EcPay Logistics Api",
                ["plugins.friendlyname.shipping.eclogistics"] = "EcPay Logistics",
                ["plugins.shipping.eclogistics.shippingmethod.edittitle"] = "Edit Shipping Method",
                ["plugins.shipping.eclogisticsfami.name"] = "Family",
                ["plugins.shipping.eclogistics.fields.name"] = "Name",
                ["plugins.shipping.eclogistics.fields.description"] = "Description",
                ["plugins.shipping.eclogistics.fields.paymentmethod"] = "Payment Method",
                ["plugins.shipping.eclogistics.fields.temperaturetype"] = "Temperature Type",
                ["plugins.shipping.eclogistics.fields.lengthlimit"] = "Length Limit",
                ["plugins.shipping.eclogistics.fields.sizelimit"] = "Volumn Limit",
                ["plugins.shipping.eclogistics.fields.weightsizelimit"] = "Weight Size Limit",
                ["plugins.shipping.eclogistics.fields.fee"] = "Fee",
                ["plugins.shipping.eclogistics.fields.transitday"] = "Transit Day",

                ["Plugins.Shipping.EcLogistics.GetReturnNumber"] = "Get Return Number",
                ["Plugins.Shipping.EcLogistics.GetReturnItem"] = "Get Rerturn Item",
                ["Plugins.Shipping.EcLogistics.Refund"] = "Refund",

                ["Plugins.Shipping.EcLogistics.Home.ConfigFee"] = "Config Fee",
                ["Plugins.Shipping.EcLogistics.Home.Fields.IsFixedFee"] = "Is Fixed Fee",
                ["Plugins.Shipping.EcLogistics.Home.Fields.ScheduledPickupTime"] = "Scheduled Pickup Time",
                ["Plugins.Shipping.EcLogistics.Home.Fields.WeightFrom"] = "Weight(From)",
                ["plugins.shipping.eclogistics.home.fields.weightto"] = "Weight(To)",
                ["plugins.shipping.eclogistics.fields.temperaturetype"] = "Temperature Type",
                ["plugins.shipping.eclogistics.home.fields.fee"] = "Local Fee",

                ["Plugins.Shipping.EcLogistics.Home.Fields.SizeFrom"] = "Volumn(From)",
                ["Plugins.Shipping.EcLogistics.Home.Fields.SizeTo"] = "Volumn(To)",
                ["plugins.shipping.eclogistics.home.fields.foreignfee"] = "Forign Fee",
                ["plugins.shipping.eclogistics.home.fields.holidayextrafee"] = "Holiday Extra Fee",
                ["plugins.shipping.eclogistics.home.shippingmethod.addrecord"] = "Add",

                //["Plugins.Shipping.EcLogistics.API"] = "託運資訊",
                ["Plugins.Shipping.EcLogistics.Create"] = "Get Tracking Number",
                ["Plugins.Shipping.EcLogistics.Print"] = "Print Shipping Orders",
            }, 1);

            //message template
            await _messageTemplateService.InsertMessageTemplateAsync(new MessageTemplate
            {
                Name = "ReturnRequest.Authorized.Cvs",
                Subject = "%Store.Name% 退貨申請已通過",
                EmailAccountId = _emailAccountRepository.Table.FirstOrDefault().Id,
                Body = @"<p>
                <a href='%Store.URL%'>%Store.Name%</a>
                <br /><br />
                Hello %Order.CustomerFullName%,<br />
                您的退貨申請已通過<br />
                訂單編號：%Order.OrderNumber%<br />
                退貨申請： <a target = '_blank'
                href = '%ReturnRequest.HistoryURLForCustomer%' >%ReturnRequest.HistoryURLForCustomer%</a>
                <br /><br />
                超商退貨請至原超商通路，操作機台輸入以下退貨代碼，將退貨商品包裝妥當後，列印單據並將包裹交給門市人員。
                <br /><br />
                超商通路：%Order.ShippingMethod%<br />
                退貨代碼：%ReturnRequest.ReturnNumber%
                <br /><br /></p> ",
                IsActive = true
            });
    

            await _messageTemplateService.InsertMessageTemplateAsync(new MessageTemplate
            {
                Name = "ReturnRequest.Authorized.Home",
                Subject = "%Store.Name% 退貨申請已通過",
                EmailAccountId = _emailAccountRepository.Table.FirstOrDefault().Id,
                Body = @"<p>
                <a href='%Store.URL%'>%Store.Name%</a>
                <br /><br />
                Hello %Order.CustomerFullName%,<br />
                您的退貨申請已通過<br />
                訂單編號：%Order.OrderNumber%<br />
                退貨申請： <a target = '_blank' href = '%ReturnRequest.HistoryURLForCustomer%' >%ReturnRequest.HistoryURLForCustomer%</a>
                <br /><br />
                宅配退貨請將退貨商品包裝妥當後，等待物流人員至取件地址收貨。
                <br /><br />
                取件地址<br />
                %Order.ShippingLastName% %Order.ShippingFirstName%<br />
                %Order.ShippingCountry% %Order.ShippingStateProvince%<br />
                %Order.ShippingZipPostalCode% %Order.ShippingCity%<br />
                %Order.ShippingAddress1%<br />
                %Order.ShippingAddress2%<br />
                <br />
                配送方法：%Order.ShippingMethod%
                <br /><br /></p>",
                IsActive = true
            });
     
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<EcLogisticsSettings>();

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

        #region Shipping Provider

        //Configure返回列表 => 返回Shipping Provider
        public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest)
        {
            return new GetShippingOptionResponse() { ShippingOptions = new List<ShippingOption>() };
        }
        
        public Task<decimal?> GetFixedRateAsync(GetShippingOptionRequest getShippingOptionRequest)
        {
            return Task.FromResult<decimal?>(null);
        }
        
        public Task<IShipmentTracker> GetShipmentTrackerAsync()
        {
            return Task.FromResult<IShipmentTracker>(null);
        }

        #endregion

        #region Admin Menu

        /// <summary>
        /// Manage sitemap. You can use "SystemName" of menu items to manage existing sitemap or add a new menu item.
        /// </summary>
        /// <param name="rootNode">Root node of the sitemap.</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            if (!await _shippingPluginManager.IsPluginActiveAsync("Shipping.EcLogistics"))
                return;

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return;

            var salesNode = rootNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Sales"));
            if (salesNode is null)
                return;

            var shipmentsNode = salesNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Shipments"));
            if (shipmentsNode is null)
                return;

            salesNode.ChildNodes.Insert(salesNode.ChildNodes.IndexOf(shipmentsNode) + 1, new SiteMapNode
            {
                SystemName = "EcLogistics API",
                Title = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.API"), //託運資訊
                //ControllerName = "EcLogistics",
                //ActionName = "BatchList",
                IconClass = "far fa-dot-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });

            var eclNode = salesNode.ChildNodes.FirstOrDefault((node => node.SystemName.Equals("EcLogistics API")));
            eclNode.ChildNodes.Insert(0, new SiteMapNode
            {
                SystemName = "EcLogisticsCreate",
                Title = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Create"),
                ControllerName = "EcLogistics",
                ActionName = "CreateList",
                IconClass = "far fa-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });
            eclNode.ChildNodes.Insert(1, new SiteMapNode
            {
                SystemName = "EcLogisticsPrint",
                Title = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Print"),
                ControllerName = "EcLogistics",
                ActionName = "PrintList",
                IconClass = "far fa-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });

            var returnRequestsNode = salesNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Return requests"));
            if (returnRequestsNode is null)
                return;

            var actionName = returnRequestsNode.ActionName;
            var controallerName = returnRequestsNode.ControllerName;
            // null 才能展開toggle list
            returnRequestsNode.ActionName = null;
            returnRequestsNode.ControllerName = null;

            returnRequestsNode.ChildNodes.Insert(0, new SiteMapNode
            {
                SystemName = returnRequestsNode.SystemName,
                Title = returnRequestsNode.Title,
                ControllerName = controallerName,
                ActionName = actionName,
                IconClass = "far fa-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });
           
            returnRequestsNode.ChildNodes.Insert(1, new SiteMapNode
            {
                SystemName = "EcLogisticsGetReturnNumber",
                Title = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.GetReturnNumber"),
                ControllerName = "EcLogistics",
                ActionName = "GetReturnRequestNumberList",
                IconClass = "far fa-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });
            
            returnRequestsNode.ChildNodes.Insert(2, new SiteMapNode
            {
                SystemName = "EcLogisticsGetReturnItem",
                Title = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.GetReturnItem"),
                ControllerName = "EcLogistics",
                ActionName = "GetReturnItemList",
                IconClass = "far fa-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });

            //  20220812 退款作業
            returnRequestsNode.ChildNodes.Insert(3, new SiteMapNode
            {
                SystemName = "EcLogisticsRefund",
                Title = await _localizationService.GetResourceAsync("Plugins.Shipping.EcLogistics.Refund"),
                ControllerName = "EcLogistics",
                ActionName = "GetRefundList",
                IconClass = "far fa-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });
        }

        #endregion
    }
}
