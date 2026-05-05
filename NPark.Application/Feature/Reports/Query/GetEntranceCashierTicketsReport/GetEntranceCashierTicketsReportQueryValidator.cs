using FluentValidation;
using NPark.Domain.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport
{
    internal sealed class GetEntranceCashierTicketsReportQueryValidator
       : AbstractValidator<GetEntranceCashierTicketsReportQuery>
    {
        public GetEntranceCashierTicketsReportQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(ErrorMessage.UserId_Required);

            RuleFor(x => x.From)
                .NotEmpty()
                .WithMessage(ErrorMessage.FromDate_Required);

            RuleFor(x => x.To)
                .NotEmpty()
                .WithMessage(ErrorMessage.ToDate_Required);

            RuleFor(x => x)
                .Must(x => x.To.Date >= x.From.Date)
                .WithMessage(ErrorMessage.ToDate_MustBeGreaterThanOrEqualFromDate);
        }
    }
}