using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create
{
    public sealed class CreateTicketExceedTransactionCommandValidation
       : AbstractValidator<CreateTicketExceedTransactionCommand>
    {
        public CreateTicketExceedTransactionCommandValidation()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage(ErrorMessage.TicketId_Required);

            RuleFor(x => x.SanctionIds)
                .NotNull()
                .WithMessage(ErrorMessage.SanctionIds_Required);

            RuleFor(x => x.SanctionIds)
                .Must(x => x is { Count: > 0 })
                .WithMessage(ErrorMessage.SanctionIds_Required);

            RuleForEach(x => x.SanctionIds)
                .NotEmpty()
                .WithMessage(ErrorMessage.SanctionId_Required);

            RuleFor(x => x.SanctionIds)
                .Must(x => x.Distinct().Count() == x.Count)
                .When(x => x.SanctionIds is { Count: > 0 })
                .WithMessage(ErrorMessage.SanctionIds_Duplicated);
        }
    }
}