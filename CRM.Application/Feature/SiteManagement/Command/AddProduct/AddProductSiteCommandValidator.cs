using CRM.Domain.Resources;
using FluentValidation;

namespace CRM.Application.Feature.SiteManagement.Command.AddProduct
{
    internal sealed class AddProductSiteCommandValidator : AbstractValidator<AddProductSiteCommand>
    {
        public AddProductSiteCommandValidator()
        {
            RuleFor(x => x.SiteId)
                .NotEmpty().WithMessage(ErrorMessage.Ticket_SiteId_Required);

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage(ErrorMessage.Product_ProductId_Required);

            RuleFor(x => x.SupportEndDate)
                .Must(d => d != default)
                .WithMessage(ErrorMessage.Product_SupportEndDate_Required)
                .Must(BeInFuture)
                .WithMessage(ErrorMessage.RenewalDate_MustBeFuture);
        }

        private static bool BeInFuture(DateTime date) => date > DateTime.Now;
    }
}