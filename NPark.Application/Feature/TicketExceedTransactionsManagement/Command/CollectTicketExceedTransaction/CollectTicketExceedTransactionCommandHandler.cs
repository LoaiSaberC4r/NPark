using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using MediatR;
using NPark.Application.Services;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction;

public sealed class CollectTicketExceedTransactionCommandHandler(ISender sender, SettlementAudit audit)
    : ICommandHandler<CollectTicketExceedTransactionCommand, CollectTicketExceedTransactionResponse>
{
    public async Task<Result<CollectTicketExceedTransactionResponse>> Handle(CollectTicketExceedTransactionCommand request, CancellationToken ct)
    {
        var result = await sender.Send(new SettleFinancialRecords(SettlementKind.Exceed), ct);
        if (result.IsFailure)
            return Result<CollectTicketExceedTransactionResponse>.Fail(result.Errors);
        var batch = result.Value;
        var transaction = batch.Transaction;
        await audit.WriteAsync(batch, SettlementKind.Exceed);
        string Name(Guid? id) => id is Guid known ? batch.CashierNames.GetValueOrDefault(known) ?? string.Empty : string.Empty;
        return Result<CollectTicketExceedTransactionResponse>.Ok(new()
        {
            CollectionTransactionId = transaction.Id,
            GateId = transaction.GateId,
            CollectedBy = transaction.CollectedBy,
            CollectedName = batch.CollectorName,
            CollectedAt = transaction.CollectedAt,
            TotalTransactionsCollected = transaction.TotalItemsCollected,
            TotalExceedAmountCollected = transaction.TotalAmountCollected,
            Lines = transaction.Lines.OrderByDescending(x => x.TotalAmount).Select(x => new TicketExceedCollectLine
            {
                ResponsibleUserId = x.ResponsibleUserId,
                ResponsibleUserName = Name(x.ResponsibleUserId),
                TransactionsCount = x.ItemsCount,
                TotalExceedAmount = x.TotalAmount
            }).ToList(),
            TicketExceedCollectDetails = batch.Exceeds.Select(x => new TicketExceedCollectDetails
            {
                TransactionId = x.Id, TicketIdSnapshot = x.TicketIdSnapshot, TicketUniqueCode = x.TicketUniqueCode,
                ResponsibleUserId = x.PaymentCashierId ?? x.UserCollectId,
                ResponsibleUserName = Name(x.PaymentCashierId ?? x.UserCollectId),
                TransactionCreatedAt = x.CreatedAt, SupervisorCollectedAt = transaction.CollectedAt,
                TicketPrice = x.TicketPrice, TicketOldExceedPrice = x.TicketOldExceedPrice,
                ExceedAmount = x.ExceedAmount, TotalPriceAfterExceed = x.TotalPriceAfterExceed,
                VehicleNumber = x.VehicleNumber, CardNumber = x.CardNumber, IsSubscriber = x.IsSubscriber
            }).ToList()
        });
    }
}
