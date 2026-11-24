using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Application.Services;
using NPark.Application.Specifications.OrderPriceSchemaSpecification;
using NPark.Application.Specifications.ParkingSystemConfigurationSpec;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.CalculateSalary
{
    public sealed class CalculateSalaryCommandHandler(IUnitOfWork db, IGenericRepository<OrderPricingSchema> schemas,
        ILogger<CalculateSalaryCommandHandler> logger, ParkingBusinessClock clock, SanctionCatalogSelection catalog,
        ITicketExitEligibilityService eligibility, IHttpContextAccessor http, ITokenReader tokens)
        : ICommandHandler<CalculateSalaryCommand, CalculateSalaryCommandResponse>
    {
        public async Task<Result<CalculateSalaryCommandResponse>> Handle(CalculateSalaryCommand request, CancellationToken ct)
        {
            var ticket = await db.Repository<Ticket>().GetByIdAsync(request.TicketId, ct);
            if (ticket is null)
                return Result<CalculateSalaryCommandResponse>.Fail(TicketExitEligibilityService.AsError("TicketNotFound"));
            if (ticket.EndDate.HasValue)
                return Result<CalculateSalaryCommandResponse>.Fail(TicketExitEligibilityService.AsError("TicketAlreadyExited"));

            if (!ticket.IsCashierCollected)
            {
                if (await TicketExitEligibilityService.FindReservationAsync(db, ticket.Id, ct) is { })
                    return Result<CalculateSalaryCommandResponse>.Fail(TicketExitEligibilityService.AsError("GateOperationPending"));

                var configuration = await db.FirstOrDefaultWithSpecAsync(
                    new GetParkingSystemConfigurationForUpdateSpecification(), ct);
                if (configuration is null)
                    return Result<CalculateSalaryCommandResponse>.Fail(
                        new Error("Configuration.NotFound", ErrorMessage.Configuration_NotFound, ErrorType.NotFound));
                if ((ticket.PricingModeSnapshot ?? configuration.PriceType) == PriceType.Exit)
                {
                    var duration = clock.LocalNow - ticket.StartDate;
                    decimal? price = IsWithinGracePeriod(duration, configuration.GracePeriod) ? 0 :
                        await CalculateTotalSalaryAsync(duration, ct);
                    if (price is null)
                        return Result<CalculateSalaryCommandResponse>.Fail(new Error("PricingSchema.NotConfigured",
                            ErrorMessage.PricingSchema_NotFound, ErrorType.Conflict));
                    ticket.SetPrice(price.Value);
                }
            }

            var selection = await catalog.LoadAsync(request.SanctionIds, ct);
            if (selection.IsFailure)
                return Result<CalculateSalaryCommandResponse>.Fail(selection.Errors);

            var assessment = await eligibility.EvaluateAsync(ticket, http.HttpContext?.User.GetGateId(tokens), ct);
            return Result<CalculateSalaryCommandResponse>.Ok(
                CalculateSalaryCommandResponse.From(ticket, assessment, selection.Value.Sum(x => x.Amount)));
        }

        private async Task<decimal?> CalculateTotalSalaryAsync(
    TimeSpan duration,
    CancellationToken cancellationToken)
        {
            if (duration <= TimeSpan.Zero)
            {
                return 0m;
            }

            var orderPricingSchemas = await schemas
                .ListWithSpecAsync(new OrderPriceSchemaSpec(), cancellationToken);

            if (orderPricingSchemas is null || !orderPricingSchemas.Any())
            {
                logger.LogWarning("No order pricing schemas found while calculating salary.");
                return null;
            }

            var orderedSchemas = orderPricingSchemas
                .Where(o => o?.PricingScheme?.TotalHours is not null &&
                            o.PricingScheme.TotalHours > 0)
                .OrderBy(o => o.Count) // ترتيب الشرائح
                .ToList();

            if (!orderedSchemas.Any())
            {
                logger.LogWarning("No valid pricing schemas (with TotalHours > 0) found.");
                return null;
            }

            decimal totalSalary = 0m;
            var remaining = duration;

            // لو فيه شريحة واحدة بس → نكرر نفس الشريحة لحد ما الـ duration يخلص
            if (orderedSchemas.Count == 1)
            {
                var single = orderedSchemas[0];
                var hours = single.PricingScheme.TotalHours!.Value;
                var salary = single.PricingScheme.Salary;

                if (hours <= 0)
                {
                    logger.LogWarning("Single pricing schema has non-positive TotalHours.");
                    return 0m;
                }

                var slotsCount = (int)Math.Ceiling(remaining.TotalHours / hours);
                totalSalary += slotsCount * salary;

                return totalSalary;
            }

            // 1) نطبق كل الشرائح ما عدا آخر شريحة مرة واحدة بالترتيب
            for (var i = 0; i < orderedSchemas.Count - 1 && remaining > TimeSpan.Zero; i++)
            {
                var schema = orderedSchemas[i];

                var hours = schema.PricingScheme.TotalHours!.Value;
                var salary = schema.PricingScheme.Salary;

                var slot = TimeSpan.FromHours(hours);

                remaining -= slot;
                totalSalary += salary;
            }

            // لو غطينا المدة كلها في الشرائح الأولى → خلاص
            if (remaining <= TimeSpan.Zero)
            {
                return totalSalary;
            }

            // 2) آخر شريحة: تتكرر لحد ما الـ remaining يخلص
            var lastSchema = orderedSchemas[^1];
            var lastHours = lastSchema.PricingScheme.TotalHours!.Value;
            var lastSalary = lastSchema.PricingScheme.Salary;

            if (lastHours <= 0)
            {
                logger.LogWarning("Last pricing schema has non-positive TotalHours.");
                return totalSalary;
            }

            var remainingHours = remaining.TotalHours;

            // عدد مرات تكرار آخر شريحة
            var lastSlotsCount = (int)Math.Ceiling(remainingHours / lastHours);

            totalSalary += lastSlotsCount * lastSalary;

            return totalSalary;
        }

        private static bool IsWithinGracePeriod(TimeSpan duration, TimeSpan? gracePeriod)
        {
            // لو مفيش GracePeriod اعتبره مفيش إعفاء
            if (gracePeriod is null) return false;

            // لو StartDate في المستقبل لأي سبب، متحسبش فلوس
            if (duration <= TimeSpan.Zero) return true;

            return duration <= gracePeriod.Value;
        }
    }
}
