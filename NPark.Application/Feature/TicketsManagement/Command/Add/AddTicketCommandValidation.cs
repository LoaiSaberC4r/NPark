using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.Add
{
    public sealed class AddTicketCommandValidation : AbstractValidator<AddTicketCommand>
    {
        public AddTicketCommandValidation()
        {
            // ---------------------------
            // Common rules
            // ---------------------------

            RuleFor(x => x.VehicleNumber)
                .MaximumLength(50)
                .WithMessage(ErrorMessage.VehicleNumberMaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.VehicleNumber));

            RuleFor(x => x.CardNumber)
                .MaximumLength(50)
                .WithMessage(ErrorMessage.CardNumberMaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.CardNumber));

            // ---------------------------
            // Subscriber rules
            // ---------------------------
            //When(x => x.IsSubscriber, () =>
            //{
            //    RuleFor(x => x)
            //        .Must(HaveCardNumberOrVehicleNumber)
            //        .WithMessage(ErrorMessage.SubscriberTicketCardOrVehicleNumberRequired);
            //});

            // ---------------------------
            // Non-subscriber rules
            // ---------------------------
            //When(x => !x.IsSubscriber, () =>
            //{
            //    RuleFor(x => x.CardNumber)
            //        .Empty()
            //        .WithMessage(ErrorMessage.NonSubscriberTicketCardNumberMustBeEmpty);
            //});
        }

        private static bool HaveCardNumberOrVehicleNumber(AddTicketCommand command)
        {
            return !string.IsNullOrWhiteSpace(command.CardNumber)
                || !string.IsNullOrWhiteSpace(command.VehicleNumber);
        }
    }
}