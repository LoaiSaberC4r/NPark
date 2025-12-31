using CRM.Domain.Resources;
using FluentValidation;

namespace CRM.Application.Feature.ProductManagement.Command.RenewProductSite
{
    internal sealed class RenewProductSiteCommandValidator : AbstractValidator<RenewProductSiteCommand>
    {
        public RenewProductSiteCommandValidator()
        {
            RuleFor(x => x.SiteProductId)
                .NotEmpty().WithMessage(ErrorMessage.SiteProduct_Id_Required);

            RuleFor(x => x.RenewalDate)
                .Must(BeInFuture)
                .WithMessage(ErrorMessage.RenewalDate_MustBeFuture);
        }

        private static bool BeInFuture(DateTime date)
            => date > DateTime.UtcNow; // or DateTime.Now based on your system
    }
}