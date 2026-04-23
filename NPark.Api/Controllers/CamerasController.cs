using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.CameraManagement.Query;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public sealed class CamerasController : ControllerTemplate
    {
        public CamerasController(ISender sender) : base(sender)
        {
        }

        [HttpGet("snapshot")]
        public async Task<IActionResult> GetSnapshot(CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetCameraSnapshotQuery(), cancellationToken);

            if (result.IsFailure)
                return result.ToIActionResult(cancellationToken);

            return File(
                result.Value.Content,
                result.Value.ContentType,
                result.Value.FileName);
        }
    }
}