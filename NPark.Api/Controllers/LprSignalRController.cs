using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected;

namespace NPark.Api.Controllers
{
    [Route("api/lpr-signalr")]
    public sealed class LprSignalRController : ControllerTemplate
    {
        public LprSignalRController(ISender sender) : base(sender)
        {
        }

        [HttpPost("vehicle-detected")]
        public async Task<IActionResult> VehicleDetected(
            [FromBody] LprVehicleDetectedCommand command,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult(cancellationToken);
        }
    }
}