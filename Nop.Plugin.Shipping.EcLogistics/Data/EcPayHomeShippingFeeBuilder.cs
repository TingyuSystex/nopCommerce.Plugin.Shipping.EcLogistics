using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Shipping.EcLogistics.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Data
{
    public class EcPayHomeShippingFeeBuilder : NopEntityBuilder<EcPayHomeShippingFee>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EcPayHomeShippingFee.Id)).AsInt32().PrimaryKey().Identity()
                .WithColumn(nameof(EcPayHomeShippingFee.EcPayHomeShippingMethodId)).AsInt32().NotNullable().ForeignKey<EcPayHomeShippingMethod>()
                .WithColumn(nameof(EcPayHomeShippingFee.TemperatureTypeId)).AsInt32().NotNullable().WithDefaultValue(5)
                .WithColumn(nameof(EcPayHomeShippingFee.SizeFrom)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.SizeTo)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.WeightFrom)).AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.WeightTo)).AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.Fee)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.ForeignFee)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.HolidayExtraFee)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayHomeShippingFee.CreatedOnUtc)).AsDateTime().NotNullable()
                .WithColumn(nameof(EcPayHomeShippingFee.UpdatedOnUtc)).AsDateTime();
        }
    }
}
