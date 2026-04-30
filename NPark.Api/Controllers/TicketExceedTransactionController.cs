using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create;

namespace NPark.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public sealed class TicketExceedTransactionController : ControllerTemplate
    {
        public TicketExceedTransactionController(ISender sender)
            : base(sender)
        {
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(
            [FromBody] CreateTicketExceedTransactionCommand command,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult(cancellationToken);
        }

        [HttpPost("collect-exceed-transactions")]
        public async Task<IActionResult> CollectTicketExceedTransactions(
          CancellationToken cancellationToken)
        {
            var result = await sender.Send(
                new CollectTicketExceedTransactionCommand(),
                cancellationToken);

            return result.ToIActionResult(cancellationToken);
        }
    }
}