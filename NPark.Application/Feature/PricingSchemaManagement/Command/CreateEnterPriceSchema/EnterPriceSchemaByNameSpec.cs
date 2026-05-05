using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.CreateEnterPriceSchema
{
    internal sealed class EnterPriceSchemaByNameSpec : Specification<PricingScheme>
    {
        public EnterPriceSchemaByNameSpec(string name)
        {
            var normalizedName = name.Trim();

            AddCriteria(x =>
                x.IsEnterPriceSchema &&
                x.Name == normalizedName);
        }
    }
}