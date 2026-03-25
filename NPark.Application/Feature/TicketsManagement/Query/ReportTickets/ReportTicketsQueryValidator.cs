using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Query.ReportTickets
{
    public sealed class ReportTicketsQueryValidator : AbstractValidator<ReportTicketsQuery>
    {
        public ReportTicketsQueryValidator()
        {
            RuleFor(x => x.FromDate)
                .NotEmpty()
                .WithMessage(ErrorMessage.ReportTickets_FromDate_Required)
                .WithErrorCode("ReportTickets.FromDate.Required");

            RuleFor(x => x.ToDate)
                .NotEmpty()
                .WithMessage(ErrorMessage.ReportTickets_ToDate_Required)
                .WithErrorCode("ReportTickets.ToDate.Required");

            RuleFor(x => x)
                .Must(x => x.ToDate >= x.FromDate)
                .WithMessage(ErrorMessage.ReportTickets_ToDate_GreaterThanOrEqualFromDate)
                .WithErrorCode("ReportTickets.DateRange.Invalid");

            When(x => !string.IsNullOrWhiteSpace(x.SearchTerm), () =>
            {
                RuleFor(x => x.SearchTerm!)
                    .MaximumLength(200)
                    .WithMessage(ErrorMessage.ReportTickets_SearchTerm_MaxLength)
                    .WithErrorCode("ReportTickets.SearchTerm.MaxLength");
                //if you want to validate date range to be within a year, you can uncomment the following code
                //            RuleFor(x => x)
                //.Must(x => (x.ToDate.Date - x.FromDate.Date).TotalDays <= 366)
                //.WithMessage(ErrorMessage.ReportTickets_DateRange_MaxAllowed)
                //.WithErrorCode("ReportTickets.DateRange.MaxAllowed");
            });
        }
    }
}