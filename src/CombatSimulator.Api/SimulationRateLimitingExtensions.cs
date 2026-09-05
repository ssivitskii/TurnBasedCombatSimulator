using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace CombatSimulator.Api;

public static class SimulationRateLimitingExtensions
{
    public const string PolicyName = "simulations";

    public static IServiceCollection AddSimulationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddConcurrencyLimiter(PolicyName, limiter =>
            {
                limiter.PermitLimit = ApiLimits.MaximumConcurrentSimulations;
                limiter.QueueLimit = ApiLimits.MaximumQueueLength;
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
            options.OnRejected = async (context, cancellationToken) =>
            {
                await Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Too many simulations",
                    detail: "At most four battle simulations can run at once. Try again shortly.")
                    .ExecuteAsync(context.HttpContext).ConfigureAwait(false);
            };
        });
        return services;
    }
}
