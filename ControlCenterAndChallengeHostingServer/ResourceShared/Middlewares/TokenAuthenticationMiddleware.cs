using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResourceShared.Logger;
using ResourceShared.Models;
using ResourceShared.Utils;
using System;
using System.Security.Claims;

namespace ResourceShared.Middlewares;

public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public TokenAuthenticationMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db, RedisHelper redis)
    {
        try
        {
            var endpoint = context.GetEndpoint();
            var authorizeAttribute = endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();

            if (authorizeAttribute != null && context.User.Identity?.IsAuthenticated == true)
            {
                var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var id))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid user token.");
                    return;
                }

                var teamIdStr = context.User.FindFirstValue("teamId");
                if (string.IsNullOrEmpty(teamIdStr) || !int.TryParse(teamIdStr, out var claimTeamId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid user token.");
                    return;
                }

                // Validate contestId claim — bắt buộc phải có trong token
                var contestIdStr = context.User.FindFirstValue("contestId");
                if (string.IsNullOrEmpty(contestIdStr) || !int.TryParse(contestIdStr, out var claimContestId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid user token: missing contestId.");
                    return;
                }

                // Allow certain endpoints to work with contestId = 0 (temporary token after login)
                var path = context.Request.Path.Value?.ToLower() ?? "";
                var allowedPathsWithoutContest = new[]
                {
                    "/api/contest",           // GET /api/Contest (list contests)
                    "/api/auth/select-contest", // POST /api/Auth/select-contest
                    "/api/users/profile",     // GET /api/Users/profile
                    "/api/config"             // GET /api/Config/* (all config endpoints)
                };

                var requiresContest = !allowedPathsWithoutContest.Any(p => path.StartsWith(p));

                if (requiresContest && claimContestId <= 0)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid user token: missing contestId.");
                    return;
                }

                // If contestId is 0 and endpoint allows it, skip contest-specific validation
                if (claimContestId == 0)
                {
                    // Basic user validation only (no contest-specific checks)
                    var basicUserInfo = await db.Users
                        .AsNoTracking()
                        .Where(u => u.Id == id)
                        .Select(u => new
                        {
                            u.Verified,
                            u.Banned,
                            u.Hidden
                        })
                        .FirstOrDefaultAsync();

                    if (basicUserInfo == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("User not found.");
                        return;
                    }

                    var basicTokenValue = await db.Tokens
                        .AsNoTracking()
                        .Where(t => t.UserId == id && t.Type == Enums.UserType.User)
                        .Select(t => t.Value)
                        .FirstOrDefaultAsync();

                    var tokenUuidFromClaim = context.User.FindFirstValue("tokenUuid");
                    if (string.IsNullOrEmpty(basicTokenValue) || !basicTokenValue.Equals(tokenUuidFromClaim))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Invalid user token");
                        return;
                    }

                    if (basicUserInfo.Verified != true)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Account not verified.");
                        return;
                    }

                    if (basicUserInfo.Banned == true)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Account banned.");
                        return;
                    }

                    if (basicUserInfo.Hidden == true)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Account hidden.");
                        return;
                    }

                    // Allow request to proceed
                    await _next(context);
                    return;
                }

                // Cache key scoped theo contest để tránh collision giữa các contest
                var cacheKey = $"contest:{claimContestId}:auth:user:{id}";
                AuthInfoCacheDTO? authInfoCache = null;
                try
                {
                    authInfoCache = await redis.GetFromCacheAsync<AuthInfoCacheDTO>(cacheKey);
                }
                catch
                {
                    // ignore cache errors and fallback to DB
                }

                if (authInfoCache != null)
                {
                    var tokenUuidFromClaim = context.User.FindFirstValue("tokenUuid");
                    if (string.IsNullOrEmpty(authInfoCache.TokenValueFromDb)
                        || !authInfoCache.TokenValueFromDb.Equals(tokenUuidFromClaim))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Invalid user token");
                        return;
                    }

                    // Validate contestId trong cache khớp với claim
                    if (authInfoCache.ContestId != claimContestId)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Token not valid for this contest.");
                        return;
                    }

                    if (authInfoCache.Verified != true)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Account not verified.");
                        return;
                    }

                    if (authInfoCache.Banned == true)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Account banned.");
                        return;
                    }

                    if (authInfoCache.Hidden == true)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Account hidden.");
                        return;
                    }

                    if (authInfoCache.TeamBanned == true)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Your team has been banned.");
                        return;
                    }

                    await _next(context);
                    return;
                }

                // Cache miss: read from DB — lấy token hiện tại của user cho contest này
                var tokenValueFromDb = await db.Tokens
                    .AsNoTracking()
                    .Where(t => t.UserId == id && t.Type == Enums.UserType.User)
                    .Select(t => t.Value)
                    .FirstOrDefaultAsync();

                var userInfo = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        u.Verified,
                        u.Banned,
                        u.Hidden,
                        TeamBanned = u.Teams.Any(t => t.Banned == true),
                    })
                    .FirstOrDefaultAsync();

                if (userInfo == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("User not found.");
                    return;
                }

                var tokenUuidFromClaim2 = context.User.FindFirstValue("tokenUuid");
                if (string.IsNullOrEmpty(tokenValueFromDb) || !tokenValueFromDb.Equals(tokenUuidFromClaim2))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid user token");
                    return;
                }

                // Validate user vẫn còn là participant của contest
                var isParticipant = await db.ContestParticipants
                    .AsNoTracking()
                    .AnyAsync(p => p.ContestId == claimContestId && p.UserId == id);

                if (!isParticipant)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("You are not a participant of this contest.");
                    return;
                }

                if (userInfo.Verified != true)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Account not verified.");
                    return;
                }

                if (userInfo.Banned == true)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Account banned.");
                    return;
                }

                if (userInfo.Hidden == true)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Account hidden.");
                    return;
                }

                if (userInfo.TeamBanned == true)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Your team has been banned.");
                    return;
                }

                // Populate cache (short TTL, scoped theo contest)
                try
                {
                    var dto = new AuthInfoCacheDTO
                    {
                        TokenValueFromDb = tokenValueFromDb,
                        ContestId = claimContestId,
                        Verified = userInfo.Verified,
                        Banned = userInfo.Banned,
                        Hidden = userInfo.Hidden,
                        TeamBanned = userInfo.TeamBanned
                    };
                    var ttlSeconds = 60;
                    _ = await redis.SetCacheAsync(cacheKey, dto, TimeSpan.FromSeconds(ttlSeconds));
                }
                catch
                {
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<AppLogger>();
                var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                _ = int.TryParse(userId, out var id);
                logger.LogError(ex, id > 0 ? id : null, data: new { path = context.Request.Path });
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("An error occurred while processing your request.");
        }
    }

    private class AuthInfoCacheDTO
    {
        public string? TokenValueFromDb { get; set; }
        public int ContestId { get; set; }
        public bool? Verified { get; set; }
        public bool? Banned { get; set; }
        public bool? Hidden { get; set; }
        public bool? TeamBanned { get; set; }
    }
}

