using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetAll
{
    public sealed class GetAllPricingSchemaQuery : SearchParameters, IQuery<Pagination<GetAllPricingSchemaQueryResponse>>
    {
    }
}