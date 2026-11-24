using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Domain.Entities
{
    public sealed class TicketCollectionTransactionTicket : Entity<Guid>
    {
        private TicketCollectionTransactionTicket()
        {
        }

        public Guid TicketCollectionTransactionId { get; private set; }

        public TicketCollectionTransaction Transaction { get; private set; } = null!;

        /// <summary>
        /// In EnterTickets: Ticket.UserId.
        /// In ExceedTransactions: TicketExceedTransaction.UserCollectId.
        /// </summary>
        public Guid? ResponsibleUserId { get; private set; }

        public User? ResponsibleUser { get; private set; }

        public TicketCollectionItemSource Source { get; private set; }

        // -----------------------------
        // Ticket Snapshot
        // -----------------------------
        public Guid SourceRecordId { get; private set; }

        public FinancialItemType FinancialItemType { get; private set; }

        public Guid TicketIdSnapshot { get; private set; }

        public string TicketUniqueCode { get; private set; } = string.Empty;

        public DateTime TicketStartDate { get; private set; }

        public DateTime? TicketEndDate { get; private set; }

        public decimal OriginalPrice { get; private set; }

        public decimal ExceedAmount { get; private set; }

        /// <summary>
        /// EnterTickets => OriginalPrice.
        /// ExceedTransactions => ExceedAmount.
        /// </summary>
        public decimal AmountCollected { get; private set; }

        public string? VehicleNumber { get; private set; }

        public string? CardNumber { get; private set; }

        public bool IsSubscriber { get; private set; }

        public string? SubscriberNationalId { get; private set; }

        public Guid TicketGateId { get; private set; }

        public Guid? TicketExitGateId { get; private set; }

        public static TicketCollectionTransactionTicket CreateFromTicket(
            Guid transactionId,
            Ticket ticket)
        {
            ArgumentNullException.ThrowIfNull(ticket);

            return Create(
                transactionId: transactionId,
                responsibleUserId: ticket.BasePaymentCashierId,
                sourceRecordId: ticket.Id,
                source: TicketCollectionItemSource.Ticket,
                ticketIdSnapshot: ticket.Id,
                ticketUniqueCode: ticket.UniqueCode,
                ticketStartDate: ticket.StartDate,
                ticketEndDate: ticket.EndDate,
                originalPrice: ticket.Price,
                exceedAmount: 0,
                amountCollected: ticket.Price,
                vehicleNumber: ticket.VehicleNumber,
                cardNumber: ticket.CardNumber,
                isSubscriber: ticket.IsSubscriber,
                subscriberNationalId: ticket.SubscriberNationalId,
                ticketGateId: ticket.GateId,
                ticketExitGateId: ticket.ExitGateId);
        }

        public static TicketCollectionTransactionTicket CreateFromTicketArchive(
            Guid transactionId,
            TicketArchive ticketArchive)
        {
            ArgumentNullException.ThrowIfNull(ticketArchive);

            return Create(
                transactionId: transactionId,
                responsibleUserId: ticketArchive.BasePaymentCashierId,
                sourceRecordId: ticketArchive.Id,
                source: TicketCollectionItemSource.TicketArchive,
                ticketIdSnapshot: ticketArchive.Id,
                ticketUniqueCode: ticketArchive.UniqueCode,
                ticketStartDate: ticketArchive.StartDate,
                ticketEndDate: ticketArchive.EndDate,
                originalPrice: ticketArchive.Price,
                exceedAmount: 0,
                amountCollected: ticketArchive.Price,
                vehicleNumber: ticketArchive.VehicleNumber,
                cardNumber: ticketArchive.CardNumber,
                isSubscriber: ticketArchive.IsSubscriber,
                subscriberNationalId: ticketArchive.SubscriberNationalId,
                ticketGateId: ticketArchive.GateId,
                ticketExitGateId: ticketArchive.ExitGateId == Guid.Empty
                    ? null
                    : ticketArchive.ExitGateId);
        }

        public static TicketCollectionTransactionTicket CreateFromExceedTransaction(
            Guid transactionId,
            TicketExceedTransaction exceedTransaction)
        {
            ArgumentNullException.ThrowIfNull(exceedTransaction);

            return Create(
                transactionId: transactionId,
                responsibleUserId: exceedTransaction.PaymentCashierId ?? exceedTransaction.UserCollectId,
                sourceRecordId: exceedTransaction.Id,
                source: TicketCollectionItemSource.TicketExceedTransaction,
                ticketIdSnapshot: exceedTransaction.TicketIdSnapshot,
                ticketUniqueCode: exceedTransaction.TicketUniqueCode,
                ticketStartDate: exceedTransaction.TicketStartDate,
                ticketEndDate: exceedTransaction.TicketEndDate,
                originalPrice: exceedTransaction.TicketPrice,
                exceedAmount: exceedTransaction.ExceedAmount,
                amountCollected: exceedTransaction.ExceedAmount,
                vehicleNumber: exceedTransaction.VehicleNumber,
                cardNumber: exceedTransaction.CardNumber,
                isSubscriber: exceedTransaction.IsSubscriber,
                subscriberNationalId: exceedTransaction.SubscriberNationalId,
                ticketGateId: exceedTransaction.TicketGateId,
                ticketExitGateId: exceedTransaction.TicketExitGateId);
        }

        private static TicketCollectionTransactionTicket Create(
            Guid transactionId,
            Guid? responsibleUserId,
            Guid sourceRecordId,
            TicketCollectionItemSource source,
            Guid ticketIdSnapshot,
            string ticketUniqueCode,
            DateTime ticketStartDate,
            DateTime? ticketEndDate,
            decimal originalPrice,
            decimal exceedAmount,
            decimal amountCollected,
            string? vehicleNumber,
            string? cardNumber,
            bool isSubscriber,
            string? subscriberNationalId,
            Guid ticketGateId,
            Guid? ticketExitGateId)
        {
            if (transactionId == Guid.Empty)
                throw new ArgumentException("Transaction id is required.", nameof(transactionId));

            if (responsibleUserId == Guid.Empty)
                throw new ArgumentException("Responsible user id must be valid when known.", nameof(responsibleUserId));

            if (ticketIdSnapshot == Guid.Empty)
                throw new ArgumentException("Ticket snapshot id is required.", nameof(ticketIdSnapshot));

            if (string.IsNullOrWhiteSpace(ticketUniqueCode))
                throw new ArgumentException("Ticket unique code is required.", nameof(ticketUniqueCode));

            if (originalPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(originalPrice), "Original price cannot be negative.");

            if (exceedAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(exceedAmount), "Exceed amount cannot be negative.");

            if (amountCollected < 0)
                throw new ArgumentOutOfRangeException(nameof(amountCollected), "Amount collected cannot be negative.");

            if (ticketGateId == Guid.Empty)
                throw new ArgumentException("Ticket gate id is required.", nameof(ticketGateId));

            return new TicketCollectionTransactionTicket
            {
                Id = Guid.NewGuid(),
                TicketCollectionTransactionId = transactionId,
                ResponsibleUserId = responsibleUserId,
                Source = source,
                SourceRecordId = sourceRecordId,
                FinancialItemType = source == TicketCollectionItemSource.TicketExceedTransaction
                    ? FinancialItemType.ExceedPayment : FinancialItemType.BaseTicketPayment,
                TicketIdSnapshot = ticketIdSnapshot,
                TicketUniqueCode = ticketUniqueCode,
                TicketStartDate = ticketStartDate,
                TicketEndDate = ticketEndDate,
                OriginalPrice = originalPrice,
                ExceedAmount = exceedAmount,
                AmountCollected = amountCollected,
                VehicleNumber = vehicleNumber,
                CardNumber = cardNumber,
                IsSubscriber = isSubscriber,
                SubscriberNationalId = subscriberNationalId,
                TicketGateId = ticketGateId,
                TicketExitGateId = ticketExitGateId
            };
        }
    }
}