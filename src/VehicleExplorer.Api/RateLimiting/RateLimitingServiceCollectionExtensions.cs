using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace VehicleExplorer.Api.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the "api" policy: a fixed window per client IP address, so the public site cannot be used to hammer NHTSA.
    /// A rejected request gets 429 Too Many Requests with a Retry-After header and a ProblemDetails body.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiRateLimitOptions>(configuration.GetSection(ApiRateLimitOptions.SectionName));

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            rateLimiterOptions.AddPolicy(ApiRateLimitOptions.PolicyName, httpContext =>
            {
                var options = httpContext.RequestServices.GetRequiredService<IOptions<ApiRateLimitOptions>>().Value;
                var clientIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIpAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = options.Window,
                    QueueLimit = 0,
                });
            });

            rateLimiterOptions.OnRejected = async (context, _) =>
            {
                var httpContext = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "Please wait a moment and try again.",
                    },
                });
            };
        });

        return services;
    }
}
