using FluentValidation;
using NPark.Application.Feature.TicketsManagement.Query.GetTicketDetails;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Query.GetExitTicketForAdmin
{
    public sealed class GetTicketDetailsQueryValidator : AbstractValidator<GetTicketDetailsQuery>
    {
        public GetTicketDetailsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage(ErrorMessage.SearchTicket_PageNumber_Invalid);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage(ErrorMessage.SearchTicket_PageSize_Max);

            RuleFor(x => x)
                .Must(x =>
                    !x.FromDate.HasValue ||
                    !x.ToDate.HasValue ||
                    x.FromDate.Value.Date <= x.ToDate.Value.Date)
                .WithMessage(ErrorMessage.SearchTicket_PageSize_Invalid);
        }
    }
}