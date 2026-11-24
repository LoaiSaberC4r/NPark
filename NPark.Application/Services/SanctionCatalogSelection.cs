using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Services;

public sealed class SanctionCatalogSelection(IUnitOfWork db)
{
    public async Task<Result<IReadOnlyList<Sanction>>> LoadAsync(IEnumerable<Guid>? selection, CancellationToken ct)
    {
        var ids = selection?.Distinct().ToArray() ?? [];
        if (ids.Length == 0)
            return Result<IReadOnlyList<Sanction>>.Ok([]);

        var sanctions = await db.ListWithSpecAsync(new Matching<Sanction>(x => ids.Contains(x.Id)), ct);
        if (sanctions.Count != ids.Length)
            return Result<IReadOnlyList<Sanction>>.Fail(new Error(
                "Sanction.NotFound", ErrorMessage.Sanction_NotFound, ErrorType.NotFound));

        if (sanctions.Any(x => x.Amount < 0))
            return Result<IReadOnlyList<Sanction>>.Fail(new Error(
                "Sanction.InvalidAmount", WorkflowMessages.Get("Sanction.InvalidAmount"), ErrorType.Conflict));

        return Result<IReadOnlyList<Sanction>>.Ok(sanctions.OrderBy(x => x.Id).ToArray());
    }
}
