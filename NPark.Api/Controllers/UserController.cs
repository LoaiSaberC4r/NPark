using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Api.Attribute;
using NPark.Application.Abstraction;
using NPark.Application.Feature.UserManagement.Command.Add;
using NPark.Application.Feature.UserManagement.Command.Delete;
using NPark.Application.Feature.UserManagement.Command.Update;
using NPark.Application.Feature.UserManagement.Query.GetCashier;
using NPark.Application.Feature.UserManagement.Query.GetUsers;
using NPark.Application.Feature.UserManagement.Query.GetUsersByRoleId;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class UserController : ControllerTemplate
    {
        public UserController(ISender sender) : base(sender)
        {
        }

        [Permission("Read")]
        [HttpGet(nameof(GetUserSystem))]
        public async Task<IActionResult> GetUserSystem([FromQuery] GetSystemUserQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }

        [Permission("Update")]
        [HttpPut(nameof(update))]
        public async Task<IActionResult> update([FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [Permission("Create")]
        [HttpPost(nameof(Create))]
        public async Task<IActionResult> Create([FromBody] AddUserCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [Permission("Delete")]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] DeleteSystemUserCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpGet(nameof(GetCashier))]
        public async Task<IActionResult> GetCashier([FromQuery] GetCashierQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpGet("by-role/{roleId:guid}")]
        public async Task<IActionResult> GetByRoleId(
            [FromRoute] Guid roleId,
            [FromQuery] GetUsersByRoleIdQuery query,
            CancellationToken cancellationToken)
        {
            query ??= new GetUsersByRoleIdQuery();

            query.RoleId = roleId;
            query.SearchText ??= string.Empty;

            var result = await sender.Send(query, cancellationToken);

            return result.ToIActionResult(cancellationToken);
        }
    }
}