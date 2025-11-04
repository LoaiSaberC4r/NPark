using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.PricingSchemaManagement.Query.GetPricingSchema;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.PricingSchemaSpecification
{
    public class GetPricingSchemaQuerySpecification : Specification<PricingScheme, GetPricingSchemaQueryResponse>
    {
        public GetPricingSchemaQuerySpecification()
        {
            AddCriteria(x => x.IsRepeated == false);

            Select(x => new GetPricingSchemaQueryResponse
            {
                Id = x.Id,
                Name = x.Name,
            });
        }
    }
}