using BuildingBlock.Domain.Specification;
using CRM.Application.Feature.ProductManagement.Query.GetProductEndedWithin30Days;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.ProductSpecification
{
    internal sealed class GetProductEndedWithin30DaysQuerySpec : Specification<SiteProduct, GetProductEndedWithin30DaysQueryResponse>
    {
        public GetProductEndedWithin30DaysQuerySpec(int pageNumber, int pageSize)
        {
            var now = DateTime.UtcNow;
            var end30 = now.AddDays(30);
            AddCriteria(sp => sp.SupportEndDate >= now && sp.SupportEndDate <= end30);
            Select(x => new GetProductEndedWithin30DaysQueryResponse
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