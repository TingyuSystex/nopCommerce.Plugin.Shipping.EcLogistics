using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Shipping.EcLogistics.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Data
{
    public class EcLogisticsInfoBuilder : NopEntityBuilder<EcPayCvsShippingMethod>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EcPayCvsShippingMethod.Id)).AsInt32().PrimaryKey().Identity()
                .WithColumn(nameof(EcPayCvsShippingMethod.Name)).AsString(50).NotNullable().Unique()
                .WithColumn(nameof(EcPayCvsShippingMethod.Description)).AsString(100).NotNullable()
                .WithColumn(nameof(EcPayCvsShippingMethod.PaymentMethod)).AsString(500).NotNullable()
                .WithColumn(nameof(EcPayCvsShippingMethod.TemperatureTypeId)).AsInt32().NotNullable().WithDefaultValue(5)
                .WithColumn(nameof(EcPayCvsShippingMethod.LengthLimit)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayCvsShippingMethod.SizeLimit)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayCvsShippingMethod.WeightSizeLimit)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayCvsShippingMethod.Fee)).AsDecimal(18,4).NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayCvsShippingMethod.TransitDay)).AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(EcPayCvsShippingMethod.CreatedOnUtc)).AsDateTime().NotNullable()
                .WithColumn(nameof(EcPayCvsShippingMethod.UpdatedOnUtc)).AsDateTime();
        }
    }
}
