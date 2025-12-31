using BuildingBlock.Domain.Specification;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.ProductSpecification
{
    public sealed class GetSiteProductByIdSpec : Specification<SiteProduct>
    {
        public GetSiteProductByIdSpec(Guid id)
        {
            AddCriteria(sp => sp.Id == id);
        }
    }
}
