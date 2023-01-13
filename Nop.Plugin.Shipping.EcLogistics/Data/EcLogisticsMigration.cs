using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using Nop.Plugin.Shipping.EcLogistics.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Data
{
    [NopMigration("2022/10/13 00:00:00", "Nop.Plugin.Shipping.EcLogistics schema", MigrationProcessType.Installation)]
    public class EcLogisticsMigration : AutoReversingMigration
    {
        public override void Up()
        {
            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayCvsShippingMethod))).Exists())
                Create.TableFor<EcPayCvsShippingMethod>();
            
            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayCvsAddressMapping))).Exists())
                Create.TableFor<EcPayCvsAddressMapping>();
          
            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayHomeShippingMethod))).Exists())
                Create.TableFor<EcPayHomeShippingMethod>();

            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayHomeShippingFee))).Exists())
                Create.TableFor<EcPayHomeShippingFee>();
            
            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(EcPayReturnNumberMapping))).Exists())
                Create.TableFor<EcPayReturnNumberMapping>();
        }
    }
}
