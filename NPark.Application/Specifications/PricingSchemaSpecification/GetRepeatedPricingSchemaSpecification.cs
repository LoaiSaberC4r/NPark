using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.PricingSchemaManagement.Query.GetRepeatedPricingSchema;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.PricingSchemaSpecification
{
    public sealed class GetRepeatedPricingSchemaSpecification : Specification<PricingScheme, GetRepeatedPricingSchemaQueryResponse>
    {
        public GetRepeatedPricingSchemaSpecification()
        {
            AddCriteria(x => x.IsRepeated == true);

            Select(x => new GetRepeatedPricingSchemaQueryResponse
            {
                Id = x.Id,
                Name = x.Name,
                DurationType = x.DurationType,
                Price = x.Salary,
                TotalHours = x.TotalHours,
                IsRepeated = x.IsRepeated
            });
        }
    }
}