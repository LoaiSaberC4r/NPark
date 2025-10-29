using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.PricingSchemaManagement.Query.GetAll;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.PricingSchemaSpecification
{
    public class GetAllPricingSchemaSpecification : Specification<PricingScheme, GetAllPricingSchemaQueryResponse>
    {
        public GetAllPricingSchemaSpecification(GetAllPricingSchemaQuery request)
        {
            if (request.SearchText != null)
            {
                AddCriteria(x => x.Name.Contains(request.SearchText));
            }
            ApplyPaging(request.PageNumber, request.PageSize);
            if (request.OrderSort == BuildingBlock.Domain.Enums.OrderSort.Newest)
            {
                AddOrderByDescending(x => x.CreatedOnUtc);
            }
            else
            {
                AddOrderBy(x => x.CreatedOnUtc);
            }
            Select(x => new GetAllPricingSchemaQueryResponse
            {
                Id = x.Id,
                Name = x.Name,
                DurationType = x.DurationType,
                Price = x.Salary,
                EndTime = x.EndTime,
                StartTime = x.StartTime,
                IsRepeated = x.IsRepeated,
                OrderPriority = x.OrderPriority,
                TotalDays = x.TotalDays,
                TotalHours = x.TotalHours
            });
            UseNoTracking();
        }
    }
}