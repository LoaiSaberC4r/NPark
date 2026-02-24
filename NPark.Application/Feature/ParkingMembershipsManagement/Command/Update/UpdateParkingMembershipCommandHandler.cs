using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.Media;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Application.Specifications.ParkingMembershipSpecification;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Command.Update
{
    public sealed class UpdateParkingMembershipCommandHandler
        : ICommandHandler<UpdateParkingMembershipCommand>
    {
        private readonly IGenericRepository<ParkingMemberships> _parkingRepo;
        private readonly IGenericRepository<PricingScheme> _pricingRepo;
        private readonly IMediaService _mediaService;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateParkingMembershipCommandHandler> _logger;

        public UpdateParkingMembershipCommandHandler(
            IGenericRepository<ParkingMemberships> parkingRepo,
            IGenericRepository<PricingScheme> pricingRepo,
            IMediaService mediaService,
            IAuditLogger auditLogger,
            ILogger<UpdateParkingMembershipCommandHandler> logger)
        {
            _parkingRepo = parkingRepo ?? throw new ArgumentNullException(nameof(parkingRepo));
            _pricingRepo = pricingRepo ?? throw new ArgumentNullException(nameof(pricingRepo));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(UpdateParkingMembershipCommand request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // 1) Load membership with attachments + scheme
                var membership = await _parkingRepo.FirstOrDefaultWithSpecAsync(
                    new ParkingMembershipByIdForUpdateSpec(request.Id), ct);

                if (membership is null)
                {
                    return Result.Fail(new Error(
                        Code: "Membership.NotFound",
                        Message: ErrorMessage.Membership_NotFound,
                        Type: ErrorType.NotFound));
                }

                // 2) Pricing scheme exists
                var pricing = await _pricingRepo.GetByIdAsync(request.PricingSchemeId, ct);
                if (pricing is null)
                {
                    return Result.Fail(new Error(
                        Code: "PricingScheme.NotFound",
                        Message: ErrorMessage.PricingSchema_NotFound,
                        Type: ErrorType.NotFound));
                }

                // 3) Uniqueness checks (exclude same record)
                var phoneExists = await _parkingRepo.IsExistAsync(
                    x => x.Phone == request.Phone && x.Id != request.Id, ct);

                if (phoneExists)
                {
                    return Result.Fail(new Error(
                        Code: "Membership.Phone.Exists",
                        Message: ErrorMessage.MembershipPhoneExists,
                        Type: ErrorType.Conflict));
                }

                var nationalIdExists = await _parkingRepo.IsExistAsync(
                    x => x.NationalId == request.NationalId && x.Id != request.Id, ct);

                if (nationalIdExists)
                {
                    return Result.Fail(new Error(
                        Code: "Membership.NationalId.Exists",
                        Message: ErrorMessage.Unique_NationalId,
                        Type: ErrorType.Conflict));
                }

                // 4) Calculate end date (renew from NOW)
                var now = DateTime.UtcNow;
                var endDate = CalculateEndDate(now, pricing);

                // 5) Update fields
                membership.UpdateName(request.Name);
                membership.UpdatePhone(request.Phone);
                membership.UpdateNationalId(request.NationalId);
                membership.UpdateVehicleNumber(request.VehicleNumber);
                membership.UpdateCardNumber(request.CardNumber);
                membership.UpdatePricingSchemeId(request.PricingSchemeId);
                membership.UpdateEndDate(endDate);

                // 6) Remove attachments
                if (request.RemoveAttachmentIds is { Count: > 0 })
                {
                    foreach (var attachmentId in request.RemoveAttachmentIds.Distinct())
                    {
                        membership.RemoveAttachment(attachmentId);
                    }
                }

                // 7) Add new images
                if (request.NewVehicleImages is { Count: > 0 })
                {
                    foreach (var file in request.NewVehicleImages)
                    {
                        var path = await _mediaService.SaveAsync(file, FileNames.ParkingMemberships);
                        membership.AddAttachment(path);
                    }
                }

                await _parkingRepo.SaveChangesAsync(ct);
                if (request.RemoveAttachmentIds is { Count: > 0 })
                {
                    foreach (var attachmentId in request.RemoveAttachmentIds.Distinct())
                    {
                        var attachment = membership.Attachments.FirstOrDefault(a => a.Id == attachmentId);
                        if (attachment is null) continue;
                        _mediaService.Remove(attachment.FilePath);
                    }
                }

                // 8) Audit
                await SafeAuditAsync(membership, pricing, ct);

                return Result.Ok();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("UpdateParkingMembership operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating parking membership.");
                return Result.Fail(new Error(
                    Code: "Membership.Update.Unexpected",
                    Message: "حدث خطأ غير متوقع أثناء تعديل الاشتراك (Unexpected error while updating membership).",
                    Type: ErrorType.Infrastructure));
            }
        }

        private static DateTime CalculateEndDate(DateTime nowUtc, PricingScheme pricing)
        {
            return pricing.DurationType switch
            {
                DurationType.Days => nowUtc.AddDays(pricing.TotalDays ?? 0),
                DurationType.Hours => nowUtc.AddHours(pricing.TotalHours ?? 0),
                DurationType.Years => nowUtc.AddYears(1),
                _ => nowUtc
            };
        }

        private async Task SafeAuditAsync(ParkingMemberships membership, PricingScheme pricing, CancellationToken ct)
        {
            try
            {
                await _auditLogger.LogAsync(
                    new AuditLogEntry(
                        EventName: "ParkingMembershipUpdated",
                        EventCategory: "ParkingMembership",
                        IsSuccess: true,
                        StatusCode: StatusCodes.Status200OK,
                        UserId: null,
                        GateId: null,
                        Extra: new
                        {
                            MembershipId = membership.Id,
                            membership.Name,
                            membership.Phone,
                            membership.NationalId,
                            membership.VehicleNumber,
                            membership.CardNumber,
                            membership.CreatedAt,
                            membership.EndDate,
                            PricingSchemeId = pricing.Id,
                            pricing.Salary,
                            UpdatedAt = DateTime.UtcNow
                        }),
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit failed for ParkingMembershipUpdated. MembershipId: {Id}", membership.Id);
            }
        }
    }
}