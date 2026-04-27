using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.Auth.Command.SupervisorLogin
{
    public sealed class SupervisorLoginCommandValidation : AbstractValidator<SupervisorLoginCommand>
    {
        public SupervisorLoginCommandValidation()
        {
            RuleFor(x => x.UserName)
                .NotEmpty()
                .WithMessage(ErrorMessage.UserName_Required)
                .MaximumLength(100)
                .WithMessage(ErrorMessage.UserNameMaxLength);

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage(ErrorMessage.PasswordRequired);

            RuleFor(x => x.GateNumber)
                .GreaterThan(0)
                .WithMessage(ErrorMessage.GateNumberMustBeGreaterThanZero);

            RuleFor(x => x.GateType)
                .IsInEnum()
                .WithMessage(ErrorMessage.GateTypeInvalid);
        }
    }
}