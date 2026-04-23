using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    public sealed record ValidateExitByTextCommand : ICommand<ValidateExitByTextCommandResponse>
    {
        public string Text { get; init; } = string.Empty;
    }
}