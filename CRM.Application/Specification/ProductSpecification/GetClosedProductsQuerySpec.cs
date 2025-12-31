using BuildingBlock.Domain.Specification;
using CRM.Application.Feature.ProductManagement.Query.GetClosedProducts;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.ProductSpecification
{
    internal sealed class GetClosedProductsQuerySpec : Specification<SiteProduct, GetClosedProductsResponse>
    {
        public GetClosedProductsQuerySpec(int pageNumber, int pageSize)
        {
            var now = DateTime.Now;
            AddCriteria(sp => sp.SupportEndDate < now);
            Select(x => new GetClosedProductsResponse
            {
                SiteProductId = x.Id,
                SiteNameAr = x.Site.NameAr,
                SiteNameEn = x.Site.NameEn,
                EndDate = x.SupportEndDate,
                ProductNameAr = x.Product.NameAr,
                ProductNameEn = x.Product.NameEn
                ,
                AccountNameAr = x.Site.Account.NameAr,
                AccountNameEn = x.Site.Account.NameEn
            });
            ApplyPaging(pageNumber, pageSize);
            UseNoTracking();
        }
    }
}