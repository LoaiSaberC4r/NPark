using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.CheckMembershipByCardNumber
{
    public sealed class CheckMembershipByCardNumberQueryValidator
        : AbstractValidator<CheckMembershipByCardNumberQuery>
    {
        public CheckMembershipByCardNumberQueryValidator()
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .WithMessage(ErrorMessage.CardNumberRequired);
        }
    }
}