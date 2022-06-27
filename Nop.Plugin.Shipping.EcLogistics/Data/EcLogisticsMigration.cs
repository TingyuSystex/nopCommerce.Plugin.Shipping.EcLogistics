using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Shipping.EcLogistics.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Data
{
    [NopMigration("2022/06/23 12:00:00", "Nop.Plugin.Shipping.EcLogistics schema", MigrationProcessType.Installation)]
    public class EcLogisticsMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<EcPayCvsShippingMethod>();
        }
        
    }
}
