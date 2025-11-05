using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.PricingSchemaManagement.Query.GetOrderSchema;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.PricingSchemaSpecification
{
    public sealed class GetOrderSchemaSpecificaiton : Specification<OrderPricingSchema, GetOrderSchemaQueryResponse>
    {
        public GetOrderSchemaSpecificaiton()
        {
            Include(x => x.PricingScheme);
            Select(x => new GetOrderSchemaQueryResponse
            {
                Id = x.PricingSchemaId,
                Name = x.PricingScheme.Name,
                DurationType = x.PricingScheme.DurationType,
                Price = x.PricingScheme.Salary,
                IsRepeated = x.PricingScheme.IsRepeated,
                TotalHours = x.PricingScheme.TotalHours,
                Count = x.Count
            });
            UseNoTracking();
        }
    }
}