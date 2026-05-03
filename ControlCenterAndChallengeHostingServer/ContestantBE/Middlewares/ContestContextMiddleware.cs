using System.Security.Claims;
using ContestantBE.Services;

namespace ContestantBE.Middlewares;

/// <summary>
/// Middleware to populate ContestContext from JWT claims
/// </summary>
public class ContestContextMiddleware
{
    private readonly RequestDelegate _next;

    public ContestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ContestContext contestContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Extract contestId from JWT
            var contestIdStr = context.User.FindFirstValue("contestId");
            if (int.TryParse(contestIdStr, out var contestId))
            {
                contestContext.ContestId = contestId;
            }

            // Extract userId
            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                contestContext.UserId = userId;
            }

            // Extract teamId
            var teamIdStr = context.User.FindFirstValue("teamId");
            if (int.TryParse(teamIdStr, out var teamId))
            {
                contestContext.TeamId = teamId;
            }

            // Extract userType
            contestContext.UserType = context.User.FindFirstValue(ClaimTypes.Role);
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to register middleware
/// </summary>
public static class ContestContextMiddlewareExtensions
{
    public static IApplicationBuilder UseContestContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ContestContextMiddleware>();
    }
}
