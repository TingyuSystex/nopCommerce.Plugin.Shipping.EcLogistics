using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Shipping.EcLogistics.Home.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Home.Data
{
    public class EcPayHomeShippingMethodBuilder : NopEntityBuilder<EcPayHomeShippingMethod>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EcPayHomeShippingMethod.Id)).AsInt32().PrimaryKey().Identity()
                .WithColumn(nameof(EcPayHomeShippingMethod.Name)).AsString(50).NotNullable().Unique()
                .WithColumn(nameof(EcPayHomeShippingMethod.Description)).AsString(100).NotNullable()
                .WithColumn(nameof(EcPayHomeShippingMethod.PaymentMethod)).AsString(500).NotNullable()
                .WithColumn(nameof(EcPayHomeShippingMethod.IsFixedFee)).AsString(1).NotNullable().WithDefaultValue("N")
                .WithColumn(nameof(EcPayHomeShippingMethod.Fee)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingMethod.TransitDay)).AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingMethod.CreatedOnUtc)).AsDateTime().NotNullable()
                .WithColumn(nameof(EcPayHomeShippingMethod.UpdatedOnUtc)).AsDateTime();
        }
    }
}
