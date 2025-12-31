using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.SharedDto;
using CRM.Application.Specification.ProductSpecification;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Feature.ProductManagement.Query.GetRenewProductMonthly
{
    public sealed record GetRenewProductMonthlyQueryHandler : IQueryHandler<GetRenewProductMonthlyQuery, Pagination<GetRenewProductMonthlyQueryResponse>>
    {
        private readonly IGenericRepository<SiteProduct> _siteProductRepository;
        public GetRenewProductMonthlyQueryHandler(IGenericRepository<SiteProduct> siteProductRepository)
        {
            _siteProductRepository = siteProductRepository ?? throw new ArgumentNullException(nameof(siteProductRepository));
        }
        public async Task<Result<Pagination<GetRenewProductMonthlyQueryResponse>>> Handle(GetRenewProductMonthlyQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetRenewProductMonthlySpec(request.PageNumber, request.PageSize);
            (var data, var count) = _siteProductRepository.GetWithSpec(spec);
            var result = new Pagination<GetRenewProductMonthlyQueryResponse>(
                currentPage: request.PageNumber,
                pageSize: request.PageSize,
                totalItems: count,
                data: await data.ToListAsync(cancellationToken)
            );
            return Result<Pagination<GetRenewProductMonthlyQueryResponse>>.Ok(result);
        }
    }
}