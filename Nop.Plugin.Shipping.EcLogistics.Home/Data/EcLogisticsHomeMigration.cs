using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Shipping.EcLogistics.Home.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Home.Data
{
    [NopMigration("2022/06/21 12:00:00", "Nop.Plugin.Shipping.EcLogistics.Home schema", MigrationProcessType.Installation)]

    public class EcLogisticsHomeMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<EcPayHomeShippingMethod>();
            Create.TableFor<EcPayHomeShippingFee>();
        }

    }
}
