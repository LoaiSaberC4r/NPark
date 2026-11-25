using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Services;

public sealed class TicketIssuanceService(ParkingBusinessClock clock, IRealtimeNotifier realtime,
    ILogger<TicketIssuanceService> logger)
{
    public Ticket CreateVisitor(Guid gateId, Guid userId, PriceType pricing, decimal price,
        DateTime issuedAtUtc, string? vehicleNumber)
    {
        var start = clock.ToBusinessTime(issuedAtUtc);
        var ticket = Ticket.Create(start, price, gateId, userId, DateOnly.FromDateTime(start), pricing);
        if (!string.IsNullOrWhiteSpace(vehicleNumber))
            ticket.SetVehicleNumber(vehicleNumber);
        return ticket;
    }

    public async Task NotifyTicketAddedAsync(Ticket ticket, ParkingGate gate, bool isAutomatic = false)
    {
        var gateNumber = gate.GateType == GateType.Entrance ? ErrorMessage.EntranceGate : ErrorMessage.ExitGate;
        try
        {
            await realtime.NotifyTicketAddedAsync(new TicketAddedNotification
            {
                TicketId = ticket.Id,
                GateId = ticket.GateId,
                StartDate = ticket.StartDate,
                Price = ticket.Price,
                IsSubscriber = ticket.IsSubscriber,
                VehicleNumber = ticket.VehicleNumber,
                GateNumber = $"{gateNumber} {gate.GateNumber}",
                IsAutomatic = isAutomatic
            });
        }
        catch (Exception error)
        {
            logger.LogError(error, "Failed to send realtime notification for ticket {TicketId}", ticket.Id);
        }
    }
}
