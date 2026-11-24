using FluentValidation;
using NPark.Domain.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionReport
{
    public sealed class GetSupervisorCollectionReportQueryValidator
        : AbstractValidator<GetSupervisorCollectionReportQuery>
    {
        public GetSupervisorCollectionReportQueryValidator()
        {
            RuleFor(x => x.SupervisorId)
                .NotEmpty()
                .WithMessage(ErrorMessage.User_NotFound);

            RuleFor(x => x.FromDate)
                .NotEmpty()
                .WithMessage("From date is required.");

            RuleFor(x => x.ToDate)
                .NotEmpty()
                .WithMessage("To date is required.");

            RuleFor(x => x)
                .Must(x => x.ToDate.Date >= x.FromDate.Date)
                .WithMessage("To date must be greater than or equal from date.");
        }
    }
}