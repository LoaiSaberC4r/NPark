using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.SharedDto;
using CRM.Application.Specification.SupportSpecification;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Feature.SupportManagement.Query.Paginate
{
    internal sealed record PaginateSupporterQueryHandler(IGenericRepository<Supporter> supporterRepo)
        : IQueryHandler<PaginateSupporterQuery, Pagination<PaginateSupporterQueryResponse>>
    {
        public async Task<Result<Pagination<PaginateSupporterQueryResponse>>> Handle(PaginateSupporterQuery request, CancellationToken cancellationToken)
        {
            var spec = new PaginationSupportSpec();
            (var data, var count) = supporterRepo.GetWithSpec(spec);
            var response = new Pagination<PaginateSupporterQueryResponse>
                (
                data: await data.ToListAsync(cancellationToken),
                totalItems: count,
                pageSize: request.PageSize,
                currentPage: request.PageNumber

                );
            return Result<Pagination<PaginateSupporterQueryResponse>>.Ok(response);
        }
    }
}