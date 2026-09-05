namespace CombatSimulator.Api;

public sealed class RequestBodyLimitMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > ApiLimits.MaximumRequestBytes)
        {
            await Results.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Request body is too large",
                detail: $"Request body cannot exceed {ApiLimits.MaximumRequestBytes} bytes.")
                .ExecuteAsync(context).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
