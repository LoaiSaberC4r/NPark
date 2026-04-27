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
        private const int PrinterNameMaxLength = 250;

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
                    .Cascade(CascadeMode.Stop)
                    .NotNull()
                    .NotEqual(Guid.Empty)
                    .WithMessage(ErrorMessage.PricingSchemaId_RequiredWhenEnter)
                    .MustAsync(async (id, token) =>
                        id.HasValue &&
                        await _pricingSchemeRepository.IsExistAsync(p => p.Id == id.Value, token))
                    .WithMessage(ErrorMessage.PricingSchema_NotFound);
            });

            RuleFor(x => x.EntryGatesCount)
                .Equal(x => x.EntryGates.Count)
                .WithMessage(ErrorMessage.Gate_EntryGatesCount_MustMatchEntryGates);

            RuleFor(x => x.ExitGatesCount)
                .Equal(x => x.ExitGates.Count)
                .WithMessage(ErrorMessage.Gate_ExitGatesCount_MustMatchExitGates);

            RuleForEach(x => x.EntryGates)
                .ChildRules(gate =>
                {
                    AddGateRules(gate, "EntryGate");
                });

            RuleForEach(x => x.ExitGates)
                .ChildRules(gate =>
                {
                    AddGateRules(gate, "ExitGate");
                });

            RuleFor(x => x)
                .Must(HaveUniqueLprIps)
                .WithMessage(ErrorMessage.Gate_LprIp_MustBeUnique);

            RuleFor(x => x)
                .Must(HaveUniqueControlIps)
                .WithMessage(ErrorMessage.Gate_ControlIp_MustBeUnique);
        }

        private static void AddGateRules(
            InlineValidator<GateInfo> gate,
            string gateLabel)
        {
            gate.RuleFor(x => x.ControlIp)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(string.Format(
                    ErrorMessage.Gate_ControlIp_Required_ForGateType,
                    gateLabel))
                .Must(IsValidIp)
                .WithMessage(string.Format(
                    ErrorMessage.Gate_ControlIp_Invalid_ForGateType,
                    gateLabel));

            gate.RuleFor(x => x.LprIp)
                .Must(IsValidIp)
                .WithMessage(string.Format(
                    ErrorMessage.Gate_LprIp_Invalid_ForGateType,
                    gateLabel))
                .When(x => !string.IsNullOrWhiteSpace(x.LprIp));

            gate.RuleFor(x => x.PcIp)
                .Must(IsValidIp)
                .WithMessage(string.Format(
                    ErrorMessage.Gate_PcIp_Invalid_ForGateType,
                    gateLabel))
                .When(x => !string.IsNullOrWhiteSpace(x.PcIp));

            gate.When(x => x.ONVIFCameraSetting is not null, () =>
            {
                gate.RuleFor(x => x.ONVIFCameraSetting!.UrlRequest)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage(ErrorMessage.Gate_ONVIF_UrlRequest_Required);

                gate.RuleFor(x => x.ONVIFCameraSetting!.UserName)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage(ErrorMessage.Gate_ONVIF_UserName_Required);

                gate.RuleFor(x => x.ONVIFCameraSetting!.Password)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage(ErrorMessage.Gate_ONVIF_Password_Required);
            });

            gate.When(x => x.HasQrPrinterName, () =>
            {
                gate.RuleFor(x => x.QrPrinterName)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage(ErrorMessage.Gate_QrPrinterName_RequiredWhenHasQrPrinterName);
            });

            gate.RuleFor(x => x.QrPrinterName)
                .MaximumLength(PrinterNameMaxLength)
                .WithMessage(ErrorMessage.Gate_QrPrinterName_MaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.QrPrinterName));

            gate.When(x => x.HasReceiptPrinterName, () =>
            {
                gate.RuleFor(x => x.ReceiptPrinterName)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage(ErrorMessage.Gate_ReceiptPrinterName_RequiredWhenHasReceiptPrinterName);
            });

            gate.RuleFor(x => x.ReceiptPrinterName)
                .MaximumLength(PrinterNameMaxLength)
                .WithMessage(ErrorMessage.Gate_ReceiptPrinterName_MaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.ReceiptPrinterName));
        }

        private static bool HaveUniqueLprIps(UpdateParkingConfigurationCommand command)
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
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            return IPAddress.TryParse(ip.Trim(), out _);
        }
    }
}