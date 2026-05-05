using FluentValidation;
using NPark.Domain.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.UserManagement.Query.GetUsersByRoleId
{
    internal sealed class GetUsersByRoleIdQueryValidator
         : AbstractValidator<GetUsersByRoleIdQuery>
    {
        public GetUsersByRoleIdQueryValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage(ErrorMessage.RoleId_Required);

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