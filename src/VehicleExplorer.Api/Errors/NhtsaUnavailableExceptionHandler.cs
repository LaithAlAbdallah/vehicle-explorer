using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Core.Vehicles;

namespace VehicleExplorer.Api.Errors;

/// <summary>Turns <see cref="NhtsaUnavailableException"/> into 503 Service Unavailable with a ProblemDetails body.</summary>
/// <remarks>Any other exception is left to the default handler, which returns 500 with a ProblemDetails body.</remarks>
public sealed partial class NhtsaUnavailableExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<NhtsaUnavailableExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not NhtsaUnavailableException)
        {
            return false;
        }

        // A handled exception is not logged by the framework, so it is logged here.
        LogNhtsaUnavailable(exception);

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Vehicle data is temporarily unavailable.",
                Detail = "The NHTSA vehicle database did not respond. Please try again in a moment.",
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "NHTSA is unavailable; returning 503.")]
    private partial void LogNhtsaUnavailable(Exception exception);
}
