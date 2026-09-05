using Microsoft.AspNetCore.Diagnostics;

namespace CombatSimulator.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return false;

        bool invalidConfiguration = exception is ArgumentException or KeyNotFoundException;
        bool tooLarge = exception is BadHttpRequestException
        {
            StatusCode: StatusCodes.Status413PayloadTooLarge,
        };
        if (!invalidConfiguration && !tooLarge)
        {
            logger.LogError(exception, "Unhandled API exception");
        }

        int status = invalidConfiguration
            ? StatusCodes.Status400BadRequest
            : tooLarge
                ? StatusCodes.Status413PayloadTooLarge
                : StatusCodes.Status500InternalServerError;
        string title = invalidConfiguration
            ? "Invalid battle configuration"
            : tooLarge
                ? "Request body is too large"
                : "Unexpected server error";
        string detail = invalidConfiguration
            ? exception.Message
            : tooLarge
                ? $"Request body cannot exceed {ApiLimits.MaximumRequestBytes} bytes."
                : "The battle could not be simulated.";
        await Results.Problem(
            statusCode: status,
            title: title,
            detail: detail)
            .ExecuteAsync(httpContext).ConfigureAwait(false);
        return true;
    }
}
