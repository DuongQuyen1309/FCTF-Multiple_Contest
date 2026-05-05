using ContestantBE.Interfaces;
using ContestantBE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceShared.Models;
using ResourceShared.Utils;

namespace ContestantBE.Controllers;

[Authorize]
public class ConfigController : BaseController
{
    private readonly CtfTimeHelper _ctfTimeHelper;
    private readonly ConfigHelper _configHelper;
    private readonly AppDbContext _dbContext;
    private readonly ContestContext _contestContext;

    public ConfigController(
        IUserContext userContext,
        CtfTimeHelper ctfTimeHelper,
        ConfigHelper configHelper,
        AppDbContext dbContext,
        ContestContext contestContext) : base(userContext)
    {
        _ctfTimeHelper = ctfTimeHelper;
        _configHelper = configHelper;
        _dbContext = dbContext;
        _contestContext = contestContext;
    }

    private long ToLong(object val)
    {
        if (val == null) return 0;
        if (long.TryParse(val.ToString(), out var result))
            return result;
        return 0;
    }

    [HttpGet("get_date_config")]
    public async Task<IActionResult> GetDateTimeConfig()
    {
        var startFromConfig = ToLong(_configHelper.GetConfig("start"));
        var endFromConfig = ToLong(_configHelper.GetConfig("end"));
        if (_ctfTimeHelper.CtfEnded())
        {
            return Ok(new { isSuccess = true, message = "CTF has ended" });
        }
        if (_ctfTimeHelper.CtfTime())
        {
            return Ok(new
            {
                isSuccess = true,
                message = "CTFd has been started",
                start_date = startFromConfig,
                end_date = endFromConfig
            });
        }
        else
        {
            return Ok(new
            {
                isSuccess = true,
                message = "CTFd is coming...",
                start_date = startFromConfig,
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("get_public_config")]
    public IActionResult GetPublicConfig()
    {
        var logo = _configHelper.GetConfig<string?>("ctf_logo", null);
        var icon = _configHelper.GetConfig<string?>("ctf_small_icon", null);
        var name = _configHelper.GetConfig<string>("ctf_name", "FCTF") ?? "FCTF";
        var bracketViewOther = _configHelper.GetConfig<bool>("bracket_view_other", false);
        var contestantRegistrationEnabled = _configHelper.GetConfig<bool>("contestant_registration_enabled", false);
        return Ok(new
        {
            isSuccess = true,
            ctf_logo = logo,
            ctf_small_icon = icon,
            ctf_name = name,
            bracket_view_other = bracketViewOther,
            contestant_registration_enabled = contestantRegistrationEnabled,
        });
    }

    /// <summary>
    /// Trả về trạng thái truy cập challenge dựa trên thời gian của contest đang chọn.
    /// Nếu không có contest context, fallback về global CTF config.
    /// </summary>
    [HttpGet("contest_access")]
    public async Task<IActionResult> GetContestAccess()
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
                {
                    var viewAfter = _ctfTimeHelper.ViewAfterCtf();
                    return Ok(new
                    {
                        isSuccess = true,
                        canAccess = viewAfter,
                        reason = viewAfter ? "ended_view_allowed" : "ended"
                    });
                }

                var now = DateTime.UtcNow;
                bool started = !contest.StartTime.HasValue || now >= contest.StartTime.Value;
                bool ended = contest.EndTime.HasValue && now > contest.EndTime.Value;
                bool active = started && !ended;

                string reason;
                if (active)
                    reason = "active";
                else if (ended && _ctfTimeHelper.ViewAfterCtf())
                    reason = "ended_view_allowed";
                else if (ended)
                    reason = "ended";
                else
                    reason = "not_started";

                return Ok(new
                {
                    isSuccess = true,
                    canAccess = active || (ended && _ctfTimeHelper.ViewAfterCtf()),
                    reason
                });
            }
        }

        // Fallback: global CTF config
        var globalCanAccess = _ctfTimeHelper.CtfTime() ||
                              (_ctfTimeHelper.CtfEnded() && _ctfTimeHelper.ViewAfterCtf());
        string globalReason;
        if (_ctfTimeHelper.CtfTime())
            globalReason = "active";
        else if (_ctfTimeHelper.CtfEnded() && _ctfTimeHelper.ViewAfterCtf())
            globalReason = "ended_view_allowed";
        else if (_ctfTimeHelper.CtfEnded())
            globalReason = "ended";
        else
            globalReason = "not_started";

        return Ok(new { isSuccess = true, canAccess = globalCanAccess, reason = globalReason });
    }
}
