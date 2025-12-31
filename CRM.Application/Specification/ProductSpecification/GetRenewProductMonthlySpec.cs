using BuildingBlock.Domain.Specification;
using CRM.Application.Feature.ProductManagement.Query.GetRenewProductMonthly;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.ProductSpecification
{
    internal class GetRenewProductMonthlySpec : Specification<SiteProduct, GetRenewProductMonthlyQueryResponse>
    {
        public GetRenewProductMonthlySpec(int pageNumber, int pageSize)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            AddCriteria(sp => sp.EnrollmentDate.HasValue
                      && sp.EnrollmentDate.Value >= monthStart
                      && sp.EnrollmentDate.Value < nextMonthStart);
            Select(x => new GetRenewProductMonthlyQueryResponse
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