using CombatSimulator.Application;
using CombatSimulator.Application.Replay;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CombatSimulator.Api.Controllers;

[ApiController]
[Route("api/battles")]
public sealed class BattlesController(BattleSimulationService simulation) : ControllerBase
{
    [HttpGet("catalog")]
    public IActionResult Catalog() => Ok(new
    {
        creatures = BattleCatalog.Creatures,
        modifiers = BattleCatalog.Modifiers,
    });

    [HttpPost("run")]
    [EnableRateLimiting(SimulationRateLimitingExtensions.PolicyName)]
    [RequestSizeLimit(ApiLimits.MaximumRequestBytes)]
    [ProducesResponseType<ReplayDocument>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<ReplayDocument> Run(BattleRunRequest request, CancellationToken cancellationToken)
    {
        if (request.Configuration is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid battle configuration",
                detail: "Configuration is required.");
        }

        if (request.Configuration.RoundLimit > ApiLimits.MaximumRoundLimit)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid battle configuration",
                detail: $"Round limit cannot exceed {ApiLimits.MaximumRoundLimit}.");
        }

        return Ok(simulation.Run(request.Configuration, request.Seed, cancellationToken));
    }
}
