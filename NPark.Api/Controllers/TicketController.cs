using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.TicketsManagement.Command.Add;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class TicketController : ControllerTemplate
    {
        public TicketController(ISender sender) : base(sender)
        {
        }

        [HttpPost(nameof(AddTicket))]
        public async Task<IActionResult> AddTicket(AddTicketCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return File(result.Value, "image/png", "ticket-qr.png");

        }
    }
}