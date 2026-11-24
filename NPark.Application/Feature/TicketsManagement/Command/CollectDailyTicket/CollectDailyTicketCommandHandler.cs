using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using MediatR;
using NPark.Application.Services;

namespace NPark.Application.Feature.TicketsManagement.Command.CollectDailyTicket;

public sealed class CollectDailyTicketCommandHandler(ISender sender, SettlementAudit audit)
    : ICommandHandler<CollectDailyTicketCommand, CollectDailyTicketResponse>
{
    public async Task<Result<CollectDailyTicketResponse>> Handle(CollectDailyTicketCommand request, CancellationToken ct)
    {
        var result = await sender.Send(new SettleFinancialRecords(SettlementKind.Daily), ct);
        if (result.IsFailure)
            return Result<CollectDailyTicketResponse>.Fail(result.Errors);
        var batch = result.Value;
        var transaction = batch.Transaction;
        await audit.WriteAsync(batch, SettlementKind.Daily);
        return Result<CollectDailyTicketResponse>.Ok(new()
        {
            TransactionId = transaction.Id,
            TotalPriceCollected = transaction.TotalAmountCollected,
            TotalTicketsCollected = transaction.TotalItemsCollected,
            TicketCollectDetails = transaction.Tickets.Select(x => new TicketCollectDetails
            {
                TicketNumber = x.TicketIdSnapshot, CollectedAt = transaction.CollectedAt, Price = x.AmountCollected
            }).ToList()
        });
    }
}
