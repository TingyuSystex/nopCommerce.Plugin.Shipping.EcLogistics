using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Shipping.EcLogistics.Home.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Home.Data
{
    public class EcPayHomeShippingFeeBuilder : NopEntityBuilder<EcPayHomeShippingFee>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EcPayHomeShippingFee.Id)).AsInt32().PrimaryKey().Identity()
                .WithColumn(nameof(EcPayHomeShippingFee.EcPayHomeShippingMethodId)).AsInt32().NotNullable().ForeignKey<EcPayHomeShippingMethod>()
                .WithColumn(nameof(EcPayHomeShippingFee.TemperatureType)).AsString(50).NotNullable().WithDefaultValue("H")
                .WithColumn(nameof(EcPayHomeShippingFee.SizeFrom)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.SizeTo)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.Fee)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.ForeignFee)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.HolidayExtraFee)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.CreatedOnUtc)).AsDateTime().NotNullable()
                .WithColumn(nameof(EcPayHomeShippingFee.UpdatedOnUtc)).AsDateTime();
        }
    }
}
