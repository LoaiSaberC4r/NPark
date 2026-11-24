using NPark.Application.Services;
using NPark.Domain.Entities;
namespace NPark.Application.Feature.TicketsManagement.Command.CalculateSalary
{
    public sealed record CalculateSalaryCommandResponse
    {
        public decimal BasePrice { get; init; }
        public decimal ExceedPrice { get; init; }
        public decimal TotalPrice { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal OutstandingAmount { get; init; }
        public decimal QuotedSanctions { get; init; }
        public decimal RequiredOvernightAmount { get; init; }
        public DateOnly ValidExitDate { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public bool RequiresOvernightPenalty { get; init; }
        public bool IsValidForExit => IsExitValid;

        public static CalculateSalaryCommandResponse From(Ticket ticket, ExitEligibility assessment, decimal quote = 0) => new()
        {
            TicketId = ticket.Id,
            EnterDate = ticket.StartDate,
            TotalSalary = ticket.Price,
            BasePrice = ticket.Price,
            ExceedPrice = ticket.ExceedPrice,
            TotalPrice = ticket.TotalPrice,
            PaidAmount = assessment.PaidAmount,
            OutstandingAmount = assessment.OutstandingAmount,
            QuotedSanctions = quote,
            RequiredOvernightAmount = assessment.RequiredOvernightAmount,
            ValidExitDate = ticket.ValidExitDate,
            ReasonCode = assessment.ReasonCode,
            RequiresOvernightPenalty = assessment.RequiresOvernightPenalty,
            IsExitValid = assessment.IsValidForExit,
            IsCollectByCashier = ticket.IsCashierCollected
        };

        public Guid TicketId { get; init; }
        public decimal TotalSalary { get; init; }
        public DateTime EnterDate { get; init; }
        public bool IsExitValid { get; init; }
        public bool IsCollectByCashier { get; init; }
    }
}