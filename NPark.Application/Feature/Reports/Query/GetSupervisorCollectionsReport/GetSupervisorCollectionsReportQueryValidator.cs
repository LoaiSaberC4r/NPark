using FluentValidation;
using NPark.Domain.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    internal sealed class GetSupervisorCollectionsReportQueryValidator
        : AbstractValidator<GetSupervisorCollectionsReportQuery>
    {
        public GetSupervisorCollectionsReportQueryValidator()
        {
            RuleFor(x => x.SupervisorId)
                .NotEmpty()
                .WithMessage(ErrorMessage.SupervisorId_Required);

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