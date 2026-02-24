using BuildingBlock.Application.Abstraction;
using NPark.Application.Shared.Dto;

namespace NPark.Application.Feature.SanctionManagement.Query.GetAll
{
    public sealed record GetAllSanctionsQuery : IQuery<IReadOnlyList<SanctionDto>>
    {
    }
}