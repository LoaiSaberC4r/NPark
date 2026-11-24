using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using MediatR;
using NPark.Application.Services;

namespace NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets;

public sealed class CollectEnterTicketsCommandHandler(ISender sender, SettlementAudit audit)
    : ICommandHandler<CollectEnterTicketsCommand, CollectEnterTicketsResponse>
{
    public async Task<Result<CollectEnterTicketsResponse>> Handle(CollectEnterTicketsCommand request, CancellationToken ct)
    {
        var result = await sender.Send(new SettleFinancialRecords(SettlementKind.Enter), ct);
        if (result.IsFailure)
            return Result<CollectEnterTicketsResponse>.Fail(result.Errors);
        var batch = result.Value;
        var transaction = batch.Transaction;
        await audit.WriteAsync(batch, SettlementKind.Enter);

        return Result<CollectEnterTicketsResponse>.Ok(new()
        {
            TransactionId = transaction.Id,
            GateId = transaction.GateId,
            CollectedBy = transaction.CollectedBy,
            CollectedName = batch.CollectorName,
            CollectedAt = transaction.CollectedAt,
            TotalTicketsCollected = transaction.TotalItemsCollected,
            TotalOriginalPriceCollected = transaction.TotalAmountCollected,
            Lines = transaction.Lines.OrderByDescending(x => x.TotalAmount).Select(x => new CollectedEnterTicketLine
            {
                ResponsibleUserId = x.ResponsibleUserId,
                ResponsibleUserName = x.ResponsibleUserId is Guid id ? batch.CashierNames.GetValueOrDefault(id) ?? string.Empty : string.Empty,
                TicketsCount = x.ItemsCount,
                TotalOriginalPrice = x.TotalAmount
            }).ToList(),
            Tickets = transaction.Tickets.OrderByDescending(x => x.TicketStartDate).Select(x => new CollectedEnterTicketDetails
            {
                TicketId = x.TicketIdSnapshot,
                TicketUniqueCode = x.TicketUniqueCode,
                Source = x.Source.ToString(),
                ResponsibleUserId = x.ResponsibleUserId,
                StartDate = x.TicketStartDate,
                EndDate = x.TicketEndDate,
                CollectedAt = transaction.CollectedAt,
                OriginalPrice = x.AmountCollected,
                VehicleNumber = x.VehicleNumber,
                CardNumber = x.CardNumber,
                IsSubscriber = x.IsSubscriber,
                GateId = x.TicketGateId,
                ExitGateId = x.TicketExitGateId
            }).ToList()
        });
    }
}
