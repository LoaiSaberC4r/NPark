using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.SanctionManagement.Command.Add
{
    internal sealed class AddSanctionCommandValidator : AbstractValidator<AddSanctionCommand>
    {
        public AddSanctionCommandValidator()
        {
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