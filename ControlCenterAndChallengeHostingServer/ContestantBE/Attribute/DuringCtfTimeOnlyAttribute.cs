using ContestantBE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ResourceShared.Models;
using ResourceShared.Utils;
using System.Security.Claims;

namespace ContestantBE.Attribute;

public class DuringCtfTimeOnlyAttribute : TypeFilterAttribute
{
    public DuringCtfTimeOnlyAttribute()
        : base(typeof(DuringCtfTimeOnlyFilter))
    {
    }
}

public class DuringCtfTimeOnlyFilter : IAsyncActionFilter
{
    private readonly CtfTimeHelper _ctfTimeHelper;
    private readonly ConfigHelper _configHelper;
    private readonly AppDbContext _dbContext;
    private readonly ContestContext _contestContext;

    public DuringCtfTimeOnlyFilter(
        CtfTimeHelper ctfTimeHelper,
        ConfigHelper configHelper,
        AppDbContext dbContext,
        ContestContext contestContext)
    {
        _ctfTimeHelper = ctfTimeHelper;
        _configHelper = configHelper;
        _dbContext = dbContext;
        _contestContext = contestContext;
    }

    private async Task<(bool isActive, bool hasEnded, bool hasStarted)> ResolveContestTime()
    {
        var contestId = _contestContext.ContestId;
        if (contestId > 0)
        {
            var contest = await _dbContext.Contests
                .AsNoTracking()
                .Where(c => c.Id == contestId)
                .Select(c => new { c.StartTime, c.EndTime, c.State })
                .FirstOrDefaultAsync();

            if (contest != null)
            {
                // Nếu state là "ended", coi như đã kết thúc
                if (contest.State == "ended")
                    return (false, true, true);

                var now = DateTime.UtcNow;
                bool started = !contest.StartTime.HasValue || now >= contest.StartTime.Value;
                bool ended = contest.EndTime.HasValue && now > contest.EndTime.Value;
                bool active = started && !ended;
                return (active, ended, started);
            }
        }

        // Fallback: dùng global CTF config (cho trường hợp không có contest context)
        return (_ctfTimeHelper.CtfTime(), _ctfTimeHelper.CtfEnded(), _ctfTimeHelper.CtfStarted());
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var (isActive, hasEnded, hasStarted) = await ResolveContestTime();

        if (isActive)
        {
            await next();
            return;
        }

        if (hasEnded)
        {
            context.Result = new JsonResult(new { error = $"{_configHelper.CtfName()} has ended" }) { StatusCode = 403 };
            return;
        }

        if (!hasStarted)
        {
            context.Result = new JsonResult(new { error = $"{_configHelper.CtfName()} has not started yet" }) { StatusCode = 403 };
            return;
        }

        if (_configHelper.IsTeamsMode()
            && context.HttpContext.User.FindFirstValue("teamId") == null)
        {
            context.Result = new JsonResult(new { error = "You must join a team to participate in this CTF" }) { StatusCode = 403 };
            return;
        }

        await next();
    }
}

/// <summary>
/// Allows access during CTF time OR after CTF ended when view_after_ctf is enabled.
/// Use this on read-only endpoints (view challenge, hints) — not on attempt/submit.
/// </summary>
public class DuringCtfTimeAndAfterOnlyAttribute : TypeFilterAttribute
{
    public DuringCtfTimeAndAfterOnlyAttribute()
        : base(typeof(ViewOrDuringCtfTimeOnlyFilter))
    {
    }
}

public class ViewOrDuringCtfTimeOnlyFilter : IAsyncActionFilter
{
    private readonly CtfTimeHelper _ctfTimeHelper;
    private readonly ConfigHelper _configHelper;
    private readonly AppDbContext _dbContext;
    private readonly ContestContext _contestContext;

    public ViewOrDuringCtfTimeOnlyFilter(
        CtfTimeHelper ctfTimeHelper,
        ConfigHelper configHelper,
        AppDbContext dbContext,
        ContestContext contestContext)
    {
        _ctfTimeHelper = ctfTimeHelper;
        _configHelper = configHelper;
        _dbContext = dbContext;
        _contestContext = contestContext;
    }

    private async Task<(bool isActive, bool hasEnded, bool hasStarted)> ResolveContestTime()
    {
        var contestId = _contestContext.ContestId;
        if (contestId > 0)
        {
            var contest = await _dbContext.Contests
                .AsNoTracking()
                .Where(c => c.Id == contestId)
                .Select(c => new { c.StartTime, c.EndTime, c.State })
                .FirstOrDefaultAsync();

            if (contest != null)
            {
                if (contest.State == "ended")
                    return (false, true, true);

                var now = DateTime.UtcNow;
                bool started = !contest.StartTime.HasValue || now >= contest.StartTime.Value;
                bool ended = contest.EndTime.HasValue && now > contest.EndTime.Value;
                bool active = started && !ended;
                return (active, ended, started);
            }
        }

        return (_ctfTimeHelper.CtfTime(), _ctfTimeHelper.CtfEnded(), _ctfTimeHelper.CtfStarted());
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var (isActive, hasEnded, hasStarted) = await ResolveContestTime();

        if (isActive)
        {
            await next();
            return;
        }

        // view_after_ctf: cho phép xem challenge sau khi contest kết thúc
        if (hasEnded && _ctfTimeHelper.ViewAfterCtf())
        {
            await next();
            return;
        }

        if (hasEnded)
        {
            context.Result = new JsonResult(new { error = $"{_configHelper.CtfName()} has ended" }) { StatusCode = 403 };
            return;
        }

        if (!hasStarted)
        {
            context.Result = new JsonResult(new { error = $"{_configHelper.CtfName()} has not started yet" }) { StatusCode = 403 };
            return;
        }

        if (_configHelper.IsTeamsMode()
            && context.HttpContext.User.FindFirstValue("teamId") == null)
        {
            context.Result = new JsonResult(new { error = "You must join a team to participate in this CTF" }) { StatusCode = 403 };
            return;
        }

        await next();
    }
}
