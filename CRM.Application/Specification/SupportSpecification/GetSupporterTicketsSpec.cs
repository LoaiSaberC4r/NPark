using BuildingBlock.Domain.Specification;
using CRM.Application.Feature.SupportManagement.Query.GetSupporterTickets;
using CRM.Application.Feature.TicketManagement.Query.GetAllSuperAdmin;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.SupportSpecification
{
    internal sealed class GetSupporterTicketsSpec : Specification<Supporter, GetSupporterTicketsQueryResponse>
    {
        public GetSupporterTicketsSpec(Guid supporterId)
        {
            Select(u => new GetSupporterTicketsQueryResponse
            {
                AssignedTickets = u.AssignedTickets.Select(x => new GetAllSuperAdminQueryResponse
                {
                    Id = x.Id,
                    Subject = x.Subject,
                    Description = x.Description,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    CreatedAt = x.CreatedOnUtc,
                    Severity = x.Severity,
                    Status = x.Status,
                    SiteNameEn = x.Site.NameEn,
                    SiteNameAr = x.Site.NameAr,
                    AccountNameEn = x.Site.Account.NameEn,
                    AccountNameAr = x.Site.Account.NameAr,
                    IsInSupport = (DateTime.Now < x.Site.SiteProducts.Where(u => u.SiteId == x.SiteId
                                    && u.ProductId == x.ProductId).First().SupportEndDate) ? true : false,
                    IsInProducts = (x.Site.SiteProducts.Any(u => u.SiteId == x.SiteId && u.ProductId == x.ProductId))
                }).ToList(),
                RejectedTickets = u.RejectedTickets.Select(x => new GetAllSuperAdminQueryResponse
                {
                    Id = x.Id,
                    Subject = x.Subject,
                    Description = x.Description,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    CreatedAt = x.CreatedOnUtc,
                    Severity = x.Severity,
                    Status = x.Status,
                    SiteNameEn = x.Site.NameEn,
                    SiteNameAr = x.Site.NameAr,
                    AccountNameEn = x.Site.Account.NameEn,
                    AccountNameAr = x.Site.Account.NameAr,
                    IsInSupport = (DateTime.Now < x.Site.SiteProducts.Where(u => u.SiteId == x.SiteId
                                    && u.ProductId == x.ProductId).First().SupportEndDate) ? true : false,
                    IsInProducts = (x.Site.SiteProducts.Any(u => u.SiteId == x.SiteId && u.ProductId == x.ProductId))
                }).ToList(),
                ClosedTickets = u.ClosedTickets.Select(x => new GetAllSuperAdminQueryResponse
                {
                    Id = x.Id,
                    Subject = x.Subject,
                    Description = x.Description,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    CreatedAt = x.CreatedOnUtc,
                    Severity = x.Severity,
                    Status = x.Status,
                    SiteNameEn = x.Site.NameEn,
                    SiteNameAr = x.Site.NameAr,
                    AccountNameEn = x.Site.Account.NameEn,
                    AccountNameAr = x.Site.Account.NameAr,
                    IsInSupport = (DateTime.Now < x.Site.SiteProducts.Where(u => u.SiteId == x.SiteId
                                    && u.ProductId == x.ProductId).First().SupportEndDate) ? true : false,
                    IsInProducts = (x.Site.SiteProducts.Any(u => u.SiteId == x.SiteId && u.ProductId == x.ProductId))
                }).ToList()
            });
        }
    }
}