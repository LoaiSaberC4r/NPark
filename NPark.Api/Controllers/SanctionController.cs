using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.SanctionManagement.Command.Add;
using NPark.Application.Feature.SanctionManagement.Command.Delete;
using NPark.Application.Feature.SanctionManagement.Command.Update;
using NPark.Application.Feature.SanctionManagement.Query;
using NPark.Application.Feature.SanctionManagement.Query.GetAll;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class SanctionController : ControllerTemplate
    {
        public SanctionController(ISender sender) : base(sender)
        {
        }

        [HttpPost(nameof(Add))]
        public async Task<IActionResult> Add(
            [FromBody] AddSanctionCommand command,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpPut(nameof(Update))]
        public async Task<IActionResult> Update(
            [FromBody] UpdateSanctionCommand command,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpDelete(nameof(Delete) + "/{id:guid}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new DeleteSanctionCommand { Id = id }, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpGet(nameof(GetAll))]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetAllSanctionsQuery(), cancellationToken);
            return result.ToIActionResult();
        }

        [HttpGet(nameof(GetById) + "/{id:guid}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetSanctionByIdQuery { Id = id }, cancellationToken);
            return result.ToIActionResult();
        }
    }
}