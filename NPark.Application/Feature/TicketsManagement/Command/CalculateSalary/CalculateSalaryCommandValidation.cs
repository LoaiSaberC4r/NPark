using FluentValidation;

namespace NPark.Application.Feature.TicketsManagement.Command.CalculateSalary
{
    public class CalculateSalaryCommandValidation : AbstractValidator<CalculateSalaryCommand>
    {
        public CalculateSalaryCommandValidation()
        {

        }
    }
}