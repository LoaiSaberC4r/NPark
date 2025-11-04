using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.DespanserManagement.Command.ExitAck
{
    public class ExitAckCommand : ICommand
    {
        public int G { get; set; }
        public string C { get; set; } = string.Empty;
        public int S { get; set; }
    }
}