using BuildingBlock.Application.Abstraction;
using NPark.Application.Shared.Dto;

namespace NPark.Application.Feature.SanctionManagement.Query
{
    public sealed record GetSanctionByIdQuery : IQuery<SanctionDto>
    {
        public Guid Id { get; init; }
    }
}