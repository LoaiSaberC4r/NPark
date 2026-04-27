using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;
using System.Net;

namespace NPark.Application.Feature.ParkingSystemConfigurationManagement.Command.Update
{
    public sealed class UpdateParkingConfigurationCommandValidator
         : AbstractValidator<UpdateParkingConfigurationCommand>
    {
        private readonly IGenericRepository<PricingScheme> _pricingSchemeRepository;

        public UpdateParkingConfigurationCommandValidator(
            IGenericRepository<PricingScheme> pricingSchemeRepository)
        {
            _pricingSchemeRepository = pricingSchemeRepository
                ?? throw new ArgumentNullException(nameof(pricingSchemeRepository));

            RuleFor(x => x.EntryGatesCount)
                .GreaterThanOrEqualTo(1)
                .WithMessage(ErrorMessage.Invalid_EntryGatesCount);

            RuleFor(x => x.ExitGatesCount)
                .GreaterThanOrEqualTo(1)
                .WithMessage(ErrorMessage.Invalid_ExitGatesCount);

            RuleFor(x => x.PriceType)
                .IsInEnum()
                .WithMessage(ErrorMessage.Invalid_PriceType);

            RuleFor(x => x.VehiclePassengerData)
                .IsInEnum()
                .WithMessage(ErrorMessage.Invalid_VehiclePassengerData);

            RuleFor(x => x.PrintType)
                .IsInEnum()
                .WithMessage(ErrorMessage.Invalid_PrintType);

            RuleFor(x => x.AllowedParkingSlots)
                .GreaterThan(0)
                .When(x => x.AllowedParkingSlots.HasValue)
                .WithMessage(ErrorMessage.Invalid_AllowedParkingSlots);

            When(x => x.PriceType == PriceType.Enter, () =>
            {
                RuleFor(x => x.PricingSchemaId)
                    .NotNull()
                    .NotEqual(Guid.Empty)
                    .WithMessage(ErrorMessage.PricingSchemaId_RequiredWhenEnter)
                    .MustAsync(async (id, token) =>
                        await _pricingSchemeRepository.IsExistAsync(p => p.Id == id, token))
                    .WithMessage(ErrorMessage.PricingSchema_NotFound);
            });

            RuleFor(x => x.EntryGatesCount)
                .Equal(x => x.EntryGates.Count)
                .WithMessage("عدد بوابات الدخول يجب أن يطابق EntryGatesCount (EntryGatesCount must match the number of EntryGates).");

            RuleFor(x => x.ExitGatesCount)
                .Equal(x => x.ExitGates.Count)
                .WithMessage("عدد بوابات الخروج يجب أن يطابق ExitGatesCount (ExitGatesCount must match the number of ExitGates).");

            RuleForEach(x => x.EntryGates)
                .ChildRules(g =>
                {
                    g.RuleFor(x => x.ControlIp)
                        .NotEmpty()
                        .WithMessage("Control IP is required for entry gates.");

                    g.RuleFor(x => x.ControlIp)
                        .Must(IsValidIp)
                        .WithMessage("EntryGate Control IP must be a valid IPv4 or IPv6 address.");

                    g.RuleFor(x => x.LprIp)
                        .Must(IsValidIp)
                        .WithMessage("عنوان LPR في بوابات الدخول يجب أن يكون IP v4 أو v6 صحيحًا (EntryGate LPR IP must be a valid IPv4 or IPv6 address).")
                        .When(x => !string.IsNullOrWhiteSpace(x.LprIp));

                    g.RuleFor(x => x.PcIp)
                        .Must(IsValidIp)
                        .WithMessage("عنوان PC في بوابات الدخول يجب أن يكون IP v4 أو v6 صحيحًا (EntryGate PC IP must be a valid IPv4 or IPv6 address).")
                        .When(x => !string.IsNullOrWhiteSpace(x.PcIp));

                    g.When(x => x.ONVIFCameraSetting is not null, () =>
                    {
                        g.RuleFor(x => x.ONVIFCameraSetting!.UrlRequest)
                            .NotEmpty()
                            .WithMessage("ONVIF UrlRequest is required.");

                        g.RuleFor(x => x.ONVIFCameraSetting!.UserName)
                            .NotEmpty()
                            .WithMessage("ONVIF UserName is required.");

                        g.RuleFor(x => x.ONVIFCameraSetting!.Password)
                            .NotEmpty()
                            .WithMessage("ONVIF Password is required.");
                    });
                });

            RuleForEach(x => x.ExitGates)
                .ChildRules(g =>
                {
                    g.RuleFor(x => x.ControlIp)
                        .NotEmpty()
                        .WithMessage("Control IP is required for exit gates.");

                    g.RuleFor(x => x.ControlIp)
                        .NotNull()
                        .WithMessage("ExitGate Control IP must be a valid ");

                    g.RuleFor(x => x.LprIp)
                        .Must(IsValidIp)
                        .WithMessage("عنوان LPR في بوابات الخروج يجب أن يكون IP v4 أو v6 صحيحًا (ExitGate LPR IP must be a valid IPv4 or IPv6 address).")
                        .When(x => !string.IsNullOrWhiteSpace(x.LprIp));

                    g.RuleFor(x => x.PcIp)
                        .Must(IsValidIp)
                        .WithMessage("عنوان PC في بوابات الخروج يجب أن يكون IP v4 أو v6 صحيحًا (ExitGate PC IP must be a valid IPv4 or IPv6 address).")
                        .When(x => !string.IsNullOrWhiteSpace(x.PcIp));

                    g.When(x => x.ONVIFCameraSetting is not null, () =>
                    {
                        g.RuleFor(x => x.ONVIFCameraSetting!.UrlRequest)
                            .NotEmpty()
                            .WithMessage("ONVIF UrlRequest is required.");

                        g.RuleFor(x => x.ONVIFCameraSetting!.UserName)
                            .NotEmpty()
                            .WithMessage("ONVIF UserName is required.");

                        g.RuleFor(x => x.ONVIFCameraSetting!.Password)
                            .NotEmpty()
                            .WithMessage("ONVIF Password is required.");
                    });
                });

            RuleFor(x => x)
                .Must(HaveUniqueIps)
                .WithMessage("يجب أن يكون عنوان الـ LPR IP فريدًا ولا يتكرر بين بوابات الدخول والخروج (Each LPR IP must be unique across both EntryGates and ExitGates).");

            RuleFor(x => x)
                .Must(HaveUniqueControlIps)
                .WithMessage("يجب أن يكون عنوان الـ Control IP فريدًا ولا يتكرر بين بوابات الدخول والخروج (Each Control IP must be unique across both EntryGates and ExitGates).");
        }

        private static bool HaveUniqueIps(UpdateParkingConfigurationCommand command)
        {
            var allIps = command.EntryGates
                .Select(g => g.LprIp?.Trim())
                .Concat(command.ExitGates.Select(g => g.LprIp?.Trim()))
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToList();

            return allIps.Distinct(StringComparer.OrdinalIgnoreCase).Count() == allIps.Count;
        }

        private static bool HaveUniqueControlIps(UpdateParkingConfigurationCommand command)
        {
            var allIps = command.EntryGates
                .Select(g => g.ControlIp?.Trim())
                .Concat(command.ExitGates.Select(g => g.ControlIp?.Trim()))
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToList();

            return allIps.Distinct(StringComparer.OrdinalIgnoreCase).Count() == allIps.Count;
        }

        private static bool IsValidIp(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            return IPAddress.TryParse(ip, out _);
        }
    }
}