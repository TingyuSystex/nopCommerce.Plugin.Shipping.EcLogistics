using System.Collections.Generic;
using System.Linq;
using FluentMigrator;
using Nop.Core.Domain.Messages;
using Nop.Data;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using Nop.Plugin.Shipping.EcLogistics.Domain;
using Nop.Services.Localization;
using Nop.Services.Messages;

namespace Nop.Plugin.Shipping.EcLogistics.Data
{
    //Go change the version in plugin.json instead of the dateTime here for the update to work
    [NopMigration("2022/10/13 00:00:00", "Insert Return Message Templates", UpdateMigrationType.Data, MigrationProcessType.Update)]
    public class EcLogisticsMigrationUpdate : AutoReversingMigration
    {
        #region Fields

        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IRepository<EmailAccount> _emailAccountRepository;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public EcLogisticsMigrationUpdate(IRepository<EmailAccount> emailAccountRepository,
            IMessageTemplateService messageTemplateService, ILocalizationService localizationService)
        {
            _messageTemplateService = messageTemplateService;
            _emailAccountRepository = emailAccountRepository;
            _localizationService = localizationService;
        }

        #endregion

        public override void Up()
        {
            //2022/10/13 - Version 1.04: Move alter column code by Amber from Migration at installation to here
            if (Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayCvsShippingMethod))).Exists() &&
                !Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayCvsShippingMethod))).Column(nameof(EcPayCvsShippingMethod.TemperatureTypeId)).Exists())
                Alter.Table(NameCompatibilityManager.GetTableName(typeof(EcPayCvsShippingMethod)))
                    .AddColumn(nameof(EcPayCvsShippingMethod.TemperatureTypeId)).AsInt32().Nullable();

            if (Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayHomeShippingFee))).Exists() &&
                !Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayHomeShippingFee))).Column(nameof(EcPayCvsShippingMethod.TemperatureTypeId)).Exists())
                Alter.Table(NameCompatibilityManager.GetTableName(typeof(EcPayHomeShippingFee)))
                    .AddColumn(nameof(EcPayCvsShippingMethod.TemperatureTypeId)).AsInt32().Nullable();

            //2022/09/14 - Version: 1.03: Have to select a shipping method for searching
            _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Shipping.EcLogistics.Print.ShippingMethodAlert"] = "配送方式不可選擇全部"
            });

            //2022/07/18 - Version: 1.02: Insert Return Message Templates
            if (!_messageTemplateService.GetMessageTemplatesByNameAsync("ReturnRequest.Authorized.Cvs").Result?.Any() ?? true)
                Insert.IntoTable(NameCompatibilityManager.GetTableName(typeof(MessageTemplate))).Row(new MessageTemplate
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

            if (!_messageTemplateService.GetMessageTemplatesByNameAsync("ReturnRequest.Authorized.Home").Result?.Any() ?? true)
                Insert.IntoTable(NameCompatibilityManager.GetTableName(typeof(MessageTemplate))).Row(new MessageTemplate
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

            //2022/07/14 - Version: 1.01: Add ReturnNumberMapping Table
            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayReturnNumberMapping))).Exists())
                Create.TableFor<EcPayReturnNumberMapping>();
        }
        
    }
}
