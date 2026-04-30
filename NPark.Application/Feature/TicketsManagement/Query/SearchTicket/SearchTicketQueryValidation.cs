using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Query.SearchTicket
{
    public sealed class SearchTicketQueryValidation : AbstractValidator<SearchTicketQuery>
    {
        public SearchTicketQueryValidation()
        {
            RuleFor(x => x.SearchText)
                .MaximumLength(100)
                .WithMessage(ErrorMessage.SearchTicket_SearchText_MaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.SearchText));

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(ErrorMessage.SearchTicket_PageNumber_Invalid);

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage(ErrorMessage.SearchTicket_PageSize_Invalid)
                .LessThanOrEqualTo(100)
                .WithMessage(ErrorMessage.SearchTicket_PageSize_Max);
        }
    }
}