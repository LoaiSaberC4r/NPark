using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.SanctionManagement.Command.Update
{
    internal sealed class UpdateSanctionCommandValidator : AbstractValidator<UpdateSanctionCommand>
    {
        private readonly IGenericRepository<Sanction> _repo;

        public UpdateSanctionCommandValidator(IGenericRepository<Sanction> repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ErrorMessage.SanctionId_Required)
                .MustAsync(async (id, ct) =>
                    await _repo.IsExistAsync(x => x.Id == id, ct))
                .WithMessage(ErrorMessage.Sanction_NotFound);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ErrorMessage.SanctionName_Required)
                .MaximumLength(200);

            RuleFor(x => x.Amount)
                .NotEmpty().WithMessage(ErrorMessage.SanctionAmount_Required)
                .GreaterThan(0).WithMessage(ErrorMessage.SanctionAmount_Invalid);

            RuleFor(x => x.Description)
                .MaximumLength(1000);
        }
    }
}