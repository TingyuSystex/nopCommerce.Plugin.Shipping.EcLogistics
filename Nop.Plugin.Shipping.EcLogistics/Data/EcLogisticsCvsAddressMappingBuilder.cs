using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Shipping.EcLogistics.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Data
{
    public class EcLogisticsMappingBuilder : NopEntityBuilder<EcPayCvsAddressMapping>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EcPayCvsAddressMapping.CustomerId)).AsInt32().ForeignKey<Customer>().PrimaryKey().NotNullable()
                .WithColumn(nameof(EcPayCvsAddressMapping.AddressId)).AsInt32().ForeignKey<Address>().PrimaryKey().NotNullable()
                .WithColumn(nameof(EcPayCvsAddressMapping.CvsType)).AsString(15).NotNullable();
        }
    }
}
