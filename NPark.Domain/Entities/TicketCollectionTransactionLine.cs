using BuildingBlock.Domain.EntitiesHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Domain.Entities
{
    public sealed class TicketCollectionTransactionLine : Entity<Guid>
    {
        private TicketCollectionTransactionLine()
        {
        }

        public Guid TicketCollectionTransactionId { get; private set; }

        public TicketCollectionTransaction Transaction { get; private set; } = null!;

        /// <summary>
        /// In EnterTickets: user who created the ticket.
        /// In ExceedTransactions: user who created/collected the exceed transaction.
        /// </summary>
        public Guid ResponsibleUserId { get; private set; }

        public User ResponsibleUser { get; private set; } = null!;

        public int ItemsCount { get; private set; }

        public decimal TotalAmount { get; private set; }

        public static TicketCollectionTransactionLine Create(
            Guid transactionId,
            Guid responsibleUserId,
            int itemsCount,
            decimal totalAmount)
        {
            if (transactionId == Guid.Empty)
                throw new ArgumentException("Transaction id is required.", nameof(transactionId));

            if (responsibleUserId == Guid.Empty)
                throw new ArgumentException("Responsible user id is required.", nameof(responsibleUserId));

            if (itemsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemsCount), "Items count must be greater than zero.");

            if (totalAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total amount cannot be negative.");

            return new TicketCollectionTransactionLine
            {
                Id = Guid.NewGuid(),
                TicketCollectionTransactionId = transactionId,
                ResponsibleUserId = responsibleUserId,
                ItemsCount = itemsCount,
                TotalAmount = totalAmount
            };
        }
    }
}