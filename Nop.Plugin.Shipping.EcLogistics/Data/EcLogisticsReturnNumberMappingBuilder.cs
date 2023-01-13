using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Orders;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Shipping.EcLogistics.Domain;

namespace Nop.Plugin.Shipping.EcLogistics.Data
{
    public class EcLogisticsReturnNumberMappingBuilder : NopEntityBuilder<EcPayReturnNumberMapping>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EcPayReturnNumberMapping.ReturnRequestId)).AsInt32().ForeignKey<ReturnRequest>().PrimaryKey().NotNullable()
                .WithColumn(nameof(EcPayReturnNumberMapping.ReturnNumber)).AsString();
        }
    }
}
