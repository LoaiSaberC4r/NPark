using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using CRM.Api.Attribute;
using CRM.Application.Feature.SupportManagement.Command.Add;
using CRM.Application.Feature.SupportManagement.Command.CloseTicket;
using CRM.Application.Feature.SupportManagement.Command.Reject;
using CRM.Application.Feature.SupportManagement.Query.Activities;
using CRM.Application.Feature.SupportManagement.Query.Paginate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers
{
    [Route("api/[controller]")]
    public class SupportController : ControllerTemplate
    {
        public SupportController(ISender sender) : base(sender)
        {
        }

        [Permission("Platform:Support:Create")]
        [HttpPost]
        public async Task<IActionResult> AddSupport([FromBody] AddSupportCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [Permission("Support:Ticket:Write")]
        [HttpPost(nameof(CloseTicket))]
        public async Task<IActionResult> CloseTicket([FromBody] CloseTicketTicketCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [Permission("Support:Ticket:Write")]
        [HttpPost(nameof(RejectTicket))]
        public async Task<IActionResult> RejectTicket([FromBody] RejectSupporterCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [Authorize]
        [HttpGet(nameof(GetAll))]
        public async Task<IActionResult> GetAll([FromQuery] PaginateSupporterQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }

        [Authorize]
        [HttpGet(nameof(Activities))]
        public async Task<IActionResult> Activities([FromQuery] ActivitiesSupportQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }
    }
}