﻿using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Command.Add
{
    public sealed record AddTicketCommand : ICommand<byte[]>
    {
    }
}