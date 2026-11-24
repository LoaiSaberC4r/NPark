using BuildingBlock.Api;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Services;

namespace NPark.Api.Contracts;

internal static class GateOperationHttp
{
    internal static IActionResult ToOperationResponse(this Result<GateOperationResponse> result)
    {
        if (result.IsFailure)
            return result.ToIActionResult();

        var operation = result.Value;
        return operation.Status switch
        {
            "Completed" or "Failed" => new OkObjectResult(operation),
            _ => new AcceptedResult($"/api/Gate/operations/{operation.OperationId}", operation)
        };
    }
}
