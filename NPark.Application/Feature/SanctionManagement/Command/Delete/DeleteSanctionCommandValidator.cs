using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.SanctionManagement.Command.Delete
{
    internal sealed class DeleteSanctionCommandValidator : AbstractValidator<DeleteSanctionCommand>
    {
        private readonly IGenericRepository<Sanction> _repo;

        public DeleteSanctionCommandValidator(IGenericRepository<Sanction> repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ErrorMessage.SanctionId_Required)
                .MustAsync(async (id, ct) =>
                    await _repo.IsExistAsync(x => x.Id == id, ct))
                .WithMessage(ErrorMessage.Sanction_NotFound);
        }
    }
}