using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetEnterPriceSchemasForSelection
{
    internal sealed class GetEnterPriceSchemasForSelectionSpec
       : Specification<PricingScheme, GetEnterPriceSchemasForSelectionResponse>
    {
        public GetEnterPriceSchemasForSelectionSpec()
        {
            AddCriteria(x => x.IsEnterPriceSchema);

            Select(x => new GetEnterPriceSchemasForSelectionResponse
            {
                Id = x.Id,
                Name = x.Name,
                Salary = x.Salary
            });
        }
    }
}