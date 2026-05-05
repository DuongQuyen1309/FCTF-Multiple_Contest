using ContestantBE.Attribute;
using ContestantBE.Interfaces;
using ContestantBE.Services;
using ContestantBE.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceShared;
using ResourceShared.DTOs.ActionLogs;
using ResourceShared.DTOs.Challenge;
using ResourceShared.Logger;
using ResourceShared.Models;
using ResourceShared.Utils;
using System.Net;
using static ResourceShared.Enums;

namespace ContestantBE.Controllers;

[Authorize]
[RequireContest] // Require contest selection for all challenge endpoints
public class ChallengeController : BaseController
{
    private readonly AppDbContext _context;
    private readonly ConfigHelper _configHelper;
    private readonly UserHelper _userHelper;
    private readonly IChallengeService _challengeServices;
    private readonly RedisHelper _redisHelper;
    private readonly RedisLockHelper _redisLockHelper;
    private readonly AppLogger _userBehaviorLogger;
    private readonly IActionLogsServices _actionLogsServices;

    public ChallengeController(
        IUserContext userContext,
        AppDbContext context,
        ConfigHelper configHelper,
        UserHelper userHelper,
        IChallengeService challengeService,
        RedisHelper redisHelper,
        RedisLockHelper redisLockHelper,
        AppLogger userBehaviorLogger,
        IActionLogsServices actionLogsServices) : base(userContext)
    {
        _context = context;
        _configHelper = configHelper;
        _userHelper = userHelper;
        _challengeServices = challengeService;
        _redisHelper = redisHelper;
        _redisLockHelper = redisLockHelper;
        _userBehaviorLogger = userBehaviorLogger;
        _actionLogsServices = actionLogsServices;
    }

    private static bool IsDuplicateKey(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique", StringComparison.OrdinalIgnoreCase)
            || message.Contains("constraint", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(bool exceeded, int current)> CheckAndIncrementKpmAsync(int userId, int limit)
    {
        if (limit <= 0) return (false, 0);

        var kpmKey = $"kpm_check_{userId}";
        var currentMinute = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        var kpmWithMinuteKey = $"{kpmKey}_{currentMinute}";

        var redis = await _redisHelper.GetDatabaseAsync();
        var newCount = await redis.StringIncrementAsync(kpmWithMinuteKey);

        var ttl = await redis.KeyTimeToLiveAsync(kpmWithMinuteKey);
        if (!ttl.HasValue || ttl.Value.TotalSeconds < 0)
            await redis.KeyExpireAsync(kpmWithMinuteKey, TimeSpan.FromSeconds(90));

        if (newCount > limit) return (true, (int)newCount);
        return (false, (int)newCount);
    }

    [HttpGet("{id}")]
    [DuringCtfTimeAndAfterOnly]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var userId = UserContext.UserId;
            var teamId = UserContext.TeamId;
            var contestId = UserContext.ContestId;

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { error = "User not found" });

            _userBehaviorLogger.Log("VIEW_CHALLENGE", user.Id, teamId, new { contestChallengeId = id });

            var result = await _challengeServices.GetById(id, teamId, user, contestId);

            if (result.HttpStatusCode != HttpStatusCode.OK || result.Data == null)
                return StatusCode((int)result.HttpStatusCode, new { success = false, message = result.Message });

            if (result.Data.is_started)
                return StatusCode((int)result.HttpStatusCode, new
                {
                    message = result.Message,
                    data = result.Data.challenge,
                    result.Data.pod_status,
                    result.Data.is_started,
                    result.Data.challenge_url,
                    result.Data.time_remaining
                });

            return StatusCode((int)result.HttpStatusCode, new
            {
                message = result.Data.success,
                data = result.Data.challenge,
                result.Data.is_started,
                result.Data.pod_status
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"An error occurred {ex.Message}" });
        }
    }

    [HttpGet("by-topic")]
    [DuringCtfTimeAndAfterOnly]
    public async Task<IActionResult> GetByTopic()
    {
        var userId = UserContext.UserId;
        var teamId = UserContext.TeamId;
        var contestId = UserContext.ContestId;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { error = "User not found" });

        try
        {
            _userBehaviorLogger.Log("VIEW_All_TOPIC", user.Id, teamId, null);
            var result = await _challengeServices.GetTopic(teamId, contestId);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("list_challenge/{category_name}")]
    [DuringCtfTimeAndAfterOnly]
    public async Task<IActionResult> ListChallengesByCategoryName([FromRoute] string category_name)
    {
        var userId = UserContext.UserId;
        var teamId = UserContext.TeamId;
        var contestId = UserContext.ContestId;
        _userBehaviorLogger.Log("VIEW_CHALLENGES_BY_CATEGORY", userId, teamId, new { category = category_name });
        var challenges = await _challengeServices.GetChallengeByCategories(category_name, teamId, contestId);
        return Ok(new { success = true, data = challenges });
    }

    [HttpGet("instances")]
    [DuringCtfTimeAndAfterOnly]
    public async Task<IActionResult> GetAllInstances()
    {
        try
        {
            var userId = UserContext.UserId;
            var teamId = UserContext.TeamId;
            var contestId = UserContext.ContestId;

            _userBehaviorLogger.Log("VIEW_TEAM_CHALLENGE_INSTANCES", userId, teamId, null);
            var instances = await _challengeServices.GetAllInstances(teamId, contestId);
            return Ok(new { success = true, data = instances });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [DuringCtfTimeOnly]
    [HttpPost("attempt")]
    public async Task<IActionResult> Attempt([FromBody] ChallengeAttemptRequest request)
    {
        var userId = UserContext.UserId;
        var teamId = UserContext.TeamId;
        var contestId = UserContext.ContestId;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (request.ChallengeId == 0)
            return BadRequest(new { error = "ChallengeId is required" });

        // Load ContestsChallenge (cc) with bank challenge flags for flag comparison
        var cc = await _context.ContestsChallenges
            .Include(c => c.BankChallenge!.Flags)
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId && c.ContestId == contestId);

        if (cc == null || cc.BankChallenge == null)
            return NotFound(new { error = "Challenge not found" });

        var bank = cc.BankChallenge;

        await Console.Out.WriteLineAsync($"[Attempt] User {userId} : Team {teamId} : Challenge {cc.Name ?? bank.Name} with flag {request.Submission}");

        _userBehaviorLogger.Log("ATTEMPT_CHALLENGE", user?.Id, teamId, new { contestChallengeId = request.ChallengeId, flag = request.Submission });

        if (_configHelper.GetConfig("paused", false))
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = true,
                data = new { status = "paused", message = $"{_configHelper.CtfName()} is paused" }
            });

        if (!_configHelper.IsUserMode() && teamId == 0)
            return Forbid();

        request.Submission = request.Submission?.Trim();

        if (string.IsNullOrEmpty(request.Submission))
            return BadRequest(new { success = false, data = new { status = "invalid", message = "Submission cannot be empty" } });

        if (request.Submission.Length > 1000)
            return BadRequest(new { success = false, data = new { status = "invalid", message = "Submission exceeds maximum length of 1000 characters" } });

        // Captain-only submit check
        var captainOnlySubmit = _configHelper.GetConfig("captain_only_submit_challenge", false);
        if (captainOnlySubmit && teamId > 0)
        {
            var captainId = await _context.Teams
                .AsNoTracking()
                .Where(t => t.Id == teamId)
                .Select(t => t.CaptainId)
                .FirstOrDefaultAsync();
            if (captainId.HasValue && captainId.Value != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    data = new { status = "forbidden", message = "Only the team captain has permission to submit flags for challenges." }
                });
        }

        // Cooldown check
        var cooldownSeconds = cc.Cooldown ?? 0;
        if (cooldownSeconds > 0)
        {
            var cooldownKey = $"contest:{contestId}:submission_cooldown_{cc.Id}_{teamId}";
            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cooldownCheck = await _redisHelper.CheckAndUpdateCooldownAsync(cooldownKey, nowSeconds, cooldownSeconds, ttlSeconds: 600);
            if (cooldownCheck >= 0)
            {
                var timeElapsed = nowSeconds - cooldownCheck;
                if (timeElapsed < cooldownSeconds)
                {
                    var remainingCooldown = (int)(cooldownSeconds - timeElapsed);
                    _userBehaviorLogger.Log("CHALLENGE_SUBMISSION_RATE_LIMITED", user?.Id, teamId, new { contestChallengeId = request.ChallengeId, remainingCooldown }, LogLevel.Warning);
                    return StatusCode(StatusCodes.Status429TooManyRequests, new
                    {
                        success = true,
                        data = new { status = "ratelimited", message = $"Please wait {remainingCooldown} seconds before submitting again.", cooldown = remainingCooldown }
                    });
                }
            }
        }

        if (cc.State == "hidden") return NotFound();
        if (cc.State == "locked") return Forbid();

        // Prerequisite check using bank IDs
        if (!string.IsNullOrEmpty(bank.Requirements))
        {
            try
            {
                var requirementsObj = System.Text.Json.JsonSerializer.Deserialize<ChallengeRequirementsDTO>(bank.Requirements);
                if (requirementsObj?.prerequisites != null && requirementsObj.prerequisites.Count > 0)
                {
                    // Get all contest challenge IDs first
                    var contestChallengeIds = await _context.ContestsChallenges
                        .AsNoTracking()
                        .Where(c => c.ContestId == contestId)
                        .Select(c => c.Id)
                        .ToListAsync();

                    var solvedBankIds = (await _context.Solves
                        .AsNoTracking()
                        .Where(s => contestChallengeIds.Contains(s.ContestChallengeId) && s.TeamId == teamId && s.ContestChallenge.BankId.HasValue)
                        .Select(s => s.ContestChallenge.BankId!.Value)
                        .ToListAsync()).ToHashSet();

                    var allBankIds = (await _context.ContestsChallenges
                        .AsNoTracking()
                        .Where(c => c.ContestId == contestId && c.BankId.HasValue)
                        .Select(c => c.BankId!.Value)
                        .ToListAsync()).ToHashSet();

                    var prereqs = requirementsObj.prerequisites
                        .Where(allBankIds.Contains)
                        .ToHashSet();

                    if (!solvedBankIds.IsSupersetOf(prereqs))
                        return StatusCode(StatusCodes.Status403Forbidden, new
                        {
                            success = false,
                            message = "You don't have the permission to access this challenge. Complete the required challenges first."
                        });
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error parsing requirements for cc {cc.Id}: {ex.Message}");
            }
        }

        // Pre-check 1: already solved
        var solvePreCheck = await _context.Solves
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ContestChallengeId == cc.Id && s.TeamId == teamId);
        if (solvePreCheck != null)
            return Ok(new { success = true, data = new { status = "already_solved", message = "You or your teammate already solved this" } });

        // Pre-check 2: max attempts
        int? currentFailsPreCheck = null;
        if (cc.MaxAttempts.HasValue && cc.MaxAttempts.Value > 0)
        {
            currentFailsPreCheck = await _context.Submissions
                .AsNoTracking()
                .Where(s => s.ContestChallengeId == cc.Id && s.TeamId == teamId && s.Type == "incorrect")
                .CountAsync();

            if (currentFailsPreCheck >= cc.MaxAttempts.Value)
                return BadRequest(new { success = true, data = new { status = "incorrect", message = "You have 0 tries remaining" } });
        }

        // Flag comparison — flags are on the bank challenge
        AttemptDTO attempt = await ChallengeHelper.Attempt(_context, bank, request);
        var deploymentKey = ChallengeHelper.GetCacheKey(contestId, cc.Id, teamId);

        if (attempt.status)
        {
            var solveCheck = await _context.Solves
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ContestChallengeId == cc.Id && s.TeamId == teamId);
            if (solveCheck != null)
                return Ok(new { success = true, data = new { status = "already_solved", message = "You or your teammate already solved this" } });

            try
            {
                var submission = new Submission
                {
                    ContestId = contestId,
                    ContestChallengeId = cc.Id,
                    UserId = user?.Id,
                    TeamId = teamId,
                    Ip = _userHelper.GetIP(HttpContext),
                    Provided = request.Submission,
                    Type = SubmissionTypes.CORRECT,
                    Solf = new Solf
                    {
                        // ContestId is NotMapped - don't set it
                        ContestChallengeId = cc.Id,
                        UserId = user?.Id,
                        TeamId = teamId,
                    }
                };
                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                return Ok(new { success = true, data = new { status = "already_solved", message = "You or your teammate already solved this" } });
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"[Error] Submission save failed for cc {cc.Id}, team {teamId}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, error = "Failed to record submission. Please try again." });
            }

            if (bank.Type == "dynamic")
            {
                _ = await DynamicChallengeHelper.RecalculateDynamicChallengeValue(_context, cc.Id, contestId, _redisLockHelper);
            }

            if (cc.RequireDeploy && await _redisHelper.KeyExistsAsync(deploymentKey))
            {
                try { _ = await _challengeServices.ForceStopChallenge(cc.Id, teamId, user!, contestId); }
                catch (Exception ex) { await Console.Error.WriteLineAsync($"Error stopping cc {cc.Id} for team {teamId}: {ex.Message}"); }
            }

            try
            {
                await _actionLogsServices.SaveActionLogs(new ActionLogsReq
                {
                    ActionType = 3,
                    ActionDetail = $"Nộp cờ đúng cho thử thách {cc.Name ?? bank.Name}",
                    ChallengeId = cc.Id,
                }, user?.Id ?? userId);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"[ActionLog] Failed to save CORRECT_FLAG log for cc {cc.Id}: {ex.Message}");
            }

            return Ok(new
            {
                success = true,
                data = new { status = "correct", attempt.message, value = cc.Value }
            });
        }

        // Incorrect attempt
        var kpm_limit = _configHelper.GetConfig("incorrect_submissions_per_min", 10);

        var (kpmExceeded, kpmCount) = await CheckAndIncrementKpmAsync(userId, kpm_limit);
        if (kpmExceeded)
        {
            _userBehaviorLogger.Log("CHALLENGE_SUBMISSION_RATE_LIMITED", userId, teamId, new { contestChallengeId = request.ChallengeId, kpmCount, kpm_limit }, LogLevel.Warning);
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                success = true,
                data = new { status = "ratelimited", message = $"You're submitting flags too fast. Slow down. ({kpmCount}/{kpm_limit} attempts in last minute)", cooldown = 0 }
            });
        }

        var hasMaxAttempts = cc.MaxAttempts.HasValue && cc.MaxAttempts.Value > 0;
        if (hasMaxAttempts)
        {
            var attemptKey = $"contest:{contestId}:attempt_count_{cc.Id}_{teamId}";
            var smartSyncThreshold = (long)(cc.MaxAttempts!.Value * 1.5);
            var actualDbCount = await _context.Submissions
                .AsNoTracking()
                .Where(s => s.ContestChallengeId == cc.Id && s.TeamId == teamId && s.Type == "incorrect")
                .CountAsync();

            var result = await _redisHelper.CheckAndIncrementAttemptsAsync(attemptKey, cc.MaxAttempts.Value, smartSyncThreshold, actualDbCount);
            if (result == -1)
                return BadRequest(new { success = true, data = new { status = "incorrect", message = "You have 0 tries remaining" } });

            currentFailsPreCheck = (int)result;
        }

        var summitFail = new Submission
        {
            ContestId = contestId,
            ContestChallengeId = cc.Id,
            UserId = user?.Id,
            TeamId = teamId,
            Ip = _userHelper.GetIP(HttpContext),
            Provided = request.Submission,
            Type = Enums.SubmissionTypes.INCORRECT,
        };
        _context.Submissions.Add(summitFail);
        await _context.SaveChangesAsync();

        try
        {
            await _actionLogsServices.SaveActionLogs(new ActionLogsReq
            {
                ActionType = 4,
                ActionDetail = $"Nộp cờ sai cho thử thách {cc.Name ?? bank.Name}",
                ChallengeId = cc.Id,
            }, user?.Id ?? userId);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[ActionLog] Failed to save INCORRECT_FLAG log for cc {cc.Id}: {ex.Message}");
        }

        if (!cc.MaxAttempts.HasValue || cc.MaxAttempts.Value <= 0)
            return Ok(new { success = true, data = new { status = "incorrect", attempt.message, cooldown = cc.Cooldown ?? 0 } });

        var failsCount = currentFailsPreCheck ?? await _context.Submissions
            .Where(s => s.ContestChallengeId == cc.Id && s.TeamId == teamId && s.Type == "incorrect")
            .CountAsync();
        var attemptsLeft = cc.MaxAttempts!.Value - failsCount;
        var triesStr = attemptsLeft == 1 ? "try" : "tries";
        var message = attempt.message;
        if (!string.IsNullOrEmpty(message) && !"!().;?[]{}".Contains(message[^1])) message += ".";

        if (attemptsLeft <= 0 && cc.RequireDeploy && await _redisHelper.KeyExistsAsync(deploymentKey))
        {
            try { await _challengeServices.ForceStopChallenge(cc.Id, teamId, user!, contestId); }
            catch (Exception ex) { await Console.Error.WriteLineAsync($"Error stopping cc {cc.Id} for team {teamId}: {ex.Message}"); }
        }

        return Ok(new
        {
            success = true,
            data = new { status = "incorrect", message = $"{message} You have {attemptsLeft} {triesStr} remaining.", cooldown = cc.Cooldown ?? 0 }
        });
    }

    [HttpPost("start")]
    [DuringCtfTimeOnly]
    public async Task<IActionResult> StartChallenge([FromBody] ChallengeStartStopReqDTO challengeStartReq)
    {
        var userId = UserContext.UserId;
        var teamId = UserContext.TeamId;
        var contestId = UserContext.ContestId;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { error = "User not found" });

        if (teamId == 0)
            return NotFound(new { error = "Team not found" });

        _userBehaviorLogger.Log("START_CHALLENGE", userId, teamId, new { contestChallengeId = challengeStartReq.challengeId });

        // challengeId from client = contestChallengeId
        var cc = await _context.ContestsChallenges
            .AsNoTracking()
            .Include(c => c.BankChallenge)
            .FirstOrDefaultAsync(c => c.Id == challengeStartReq.challengeId && c.ContestId == contestId);

        if (cc == null || cc.BankChallenge == null)
            return NotFound(new { error = "Challenge not found" });

        var bank = cc.BankChallenge;

        if (!cc.RequireDeploy) return BadRequest(new { error = "This challenge does not require deploy" });
        if (cc.State == "hidden" || bank.SharedInstant == true) return BadRequest(new { error = "This challenge is not available for deployment" });

        // Prerequisite check
        if (!string.IsNullOrEmpty(bank.Requirements))
        {
            try
            {
                var requirementsObj = System.Text.Json.JsonSerializer.Deserialize<ChallengeRequirementsDTO>(bank.Requirements);
                if (requirementsObj?.prerequisites != null && requirementsObj.prerequisites.Count > 0)
                {
                    // Get all contest challenge IDs first
                    var contestChallengeIds = await _context.ContestsChallenges
                        .AsNoTracking()
                        .Where(c => c.ContestId == contestId)
                        .Select(c => c.Id)
                        .ToListAsync();

                    var solvedBankIds = (await _context.Solves
                        .AsNoTracking()
                        .Where(s => contestChallengeIds.Contains(s.ContestChallengeId) && s.TeamId == teamId && s.ContestChallenge.BankId.HasValue)
                        .Select(s => s.ContestChallenge.BankId!.Value)
                        .ToListAsync()).ToHashSet();

                    var allBankIds = (await _context.ContestsChallenges
                        .AsNoTracking()
                        .Where(c => c.ContestId == contestId && c.BankId.HasValue)
                        .Select(c => c.BankId!.Value)
                        .ToListAsync()).ToHashSet();

                    var prereqs = requirementsObj.prerequisites.Where(allBankIds.Contains).ToHashSet();
                    if (!solvedBankIds.IsSupersetOf(prereqs))
                        return StatusCode(StatusCodes.Status403Forbidden, new
                        {
                            error = "You don't have the permission to start this challenge. Please complete the required challenges first."
                        });
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error parsing requirements for cc {cc.Id}: {ex.Message}");
            }
        }

        if (cc.MaxAttempts.HasValue && cc.MaxAttempts.Value > 0)
        {
            var incorrectCount = await _context.Submissions
                .AsNoTracking()
                .Where(s => s.ContestChallengeId == cc.Id && s.TeamId == teamId && s.Type == SubmissionTypes.INCORRECT)
                .CountAsync();
            if (incorrectCount >= cc.MaxAttempts.Value)
                return BadRequest(new { error = "You have 0 tries remaining. You cannot start this challenge." });
        }

        if (cc.MaxDeployCount.HasValue && cc.MaxDeployCount.Value > 0)
        {
            var currentDeployCount = await _context.ChallengeStartTrackings
                .AsNoTracking()
                .Where(d => d.ContestChallengeId == cc.Id && d.TeamId == teamId)
                .CountAsync();
            if (currentDeployCount >= cc.MaxDeployCount.Value)
                return BadRequest(new { error = "You have reached the maximum number of deployments for this challenge." });
        }

        var solve = await _context.Solves
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ContestChallengeId == cc.Id && s.TeamId == teamId);
        if (solve != null)
            return BadRequest(new { error = "Your team has already solved this challenge. You cannot start this challenge." });

        var captainOnlyStart = _configHelper.GetConfig("captain_only_start_challenge", true);
        if (captainOnlyStart)
        {
            var captainId = await _context.Teams
                .AsNoTracking()
                .Where(t => t.Id == teamId)
                .Select(t => t.CaptainId)
                .FirstOrDefaultAsync();
            if (captainId.HasValue && captainId.Value != userId)
                return BadRequest(new { error = "Contact the organizers to select a team captain. Only the team captain has the permission to start the challenge." });
        }

        await Console.Out.WriteLineAsync($"[Start Challenge] User {userId} : Team {teamId} : Challenge {cc.Name ?? bank.Name}");

        var limitChallenges = _configHelper.LimitChallenges();
        var deploymentKey = ChallengeHelper.GetCacheKey(contestId, cc.Id, teamId);
        var teamIdStr = teamId.ToString();
        var ccIdStr = cc.Id.ToString();
        var cacheDto = new ChallengeDeploymentCacheDTO
        {
            challenge_id = cc.Id,
            contest_id = contestId,
            team_id = teamId,
            status = DeploymentStatus.INITIAL,
            user_id = userId,
        };
        string deploymentValue = System.Text.Json.JsonSerializer.Serialize(cacheDto);

        DeploymentCheckResult redisResult = await _redisHelper.AtomicCheckAndCreateDeploymentZSet(
            teamId: teamIdStr,
            deploymentKey: deploymentKey,
            challengeId: ccIdStr,
            maxLimit: limitChallenges,
            deploymentValue: deploymentValue,
            provisioningTtl: 300,
            contestId: contestId);

        switch (redisResult)
        {
            case DeploymentCheckResult.LimitExceeded:
                await Console.Out.WriteLineAsync($"Team {teamId} hit concurrent limit on cc {cc.Id}");
                return BadRequest(new { error = $"You have reached the maximum limit of {limitChallenges} concurrent challenges." });
            case DeploymentCheckResult.AlreadyExists:
                await Console.Out.WriteLineAsync($"Team {teamId} already has cc {cc.Id} deployed");
                var deploymentCache = await _redisHelper.GetFromCacheAsync<ChallengeDeploymentCacheDTO>(deploymentKey) ?? new ChallengeDeploymentCacheDTO();
                switch (deploymentCache.status)
                {
                    case DeploymentStatus.INITIAL:
                        return BadRequest(new { error = "Your previous challenge deployment is still in progress. Please wait until it is completed before starting a new one." });
                    case DeploymentStatus.PENDING:
                        return Ok(new ChallengeDeployResponeDTO { status = (int)HttpStatusCode.OK, success = true, message = "Challenge is deploying." });
                    case DeploymentStatus.DELETING:
                        return BadRequest(new { error = "Challenge is being deleted. Please wait a moment before starting again." });
                    case DeploymentStatus.RUNING:
                        int timeLeft = 0;
                        if (deploymentCache.time_finished > 0)
                        {
                            long remainSec = deploymentCache.time_finished - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            if (remainSec < 0) remainSec = 0;
                            timeLeft = (int)(remainSec / 60);
                        }
                        return Ok(new ChallengeDeployResponeDTO { status = (int)HttpStatusCode.OK, success = true, message = "Challenge is running.", challenge_url = deploymentCache.challenge_url, time_limit = timeLeft });
                    default:
                        return BadRequest(new { error = "You have already started this challenge." });
                }
            case DeploymentCheckResult.Pass:
                break;
            default:
                return StatusCode(500, new { error = "Unexpected Redis error." });
        }

        try
        {
            var response = await _challengeServices.ChallengeStart(cc, bank, user, teamId, contestId);
            if (response.status != (int)HttpStatusCode.OK)
            {
                await Console.Error.WriteLineAsync($"[Rollback] Team {teamId} start cc failed: {response.message}.");
                await _redisHelper.AtomicRemoveDeploymentZSet(teamIdStr, deploymentKey, ccIdStr, contestId);
            }
            else
            {
                try
                {
                    await _actionLogsServices.SaveActionLogs(new ActionLogsReq
                    {
                        ActionType = 2,
                        ActionDetail = $"Khởi động thử thách {cc.Name ?? bank.Name}",
                        ChallengeId = cc.Id,
                    }, userId);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"[ActionLog] Failed to save START_CHALLENGE log for cc {cc.Id}: {ex.Message}");
                }
            }
            return response.status switch
            {
                (int)HttpStatusCode.OK => Ok(response),
                (int)HttpStatusCode.BadRequest => BadRequest(response),
                (int)HttpStatusCode.NotFound => NotFound(response),
                _ => StatusCode((int)response.status, response)
            };
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"[Rollback] Exception during start challenge: {e.Message}. Reverting Redis for {deploymentKey}");
            await _redisHelper.AtomicRemoveDeploymentZSet(teamIdStr, deploymentKey, ccIdStr, contestId);
            return BadRequest(new { error = "Failed to connect to start API", error_detail = e.ToString() });
        }
    }

    [HttpPost("stop-by-user")]
    [DuringCtfTimeOnly]
    public async Task<IActionResult> StopChallengeByUser([FromBody] ChallengeStartStopReqDTO challengeStartReq)
    {
        if (challengeStartReq == null || challengeStartReq.challengeId <= 0)
            return BadRequest(new { error = "ChallengeId is required" });

        var userId = UserContext.UserId;
        var teamId = UserContext.TeamId;
        var contestId = UserContext.ContestId;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (teamId == 0)
            return BadRequest(new { error = "User no join team" });

        _userBehaviorLogger.Log("STOP_CHALLENGE", userId, teamId, new { challengeStartReq.challengeId });

        var cc = await _context.ContestsChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeStartReq.challengeId && c.ContestId == contestId);

        if (cc == null) return BadRequest(new { error = "Challenge not found" });

        var cacheKey = ChallengeHelper.GetCacheKey(contestId, cc.Id, teamId);
        if (!await _redisHelper.KeyExistsAsync(cacheKey))
            return BadRequest(new { error = "Challenge not started or already stopped, no active cache found." });

        try
        {
            await Console.Out.WriteLineAsync($"[Stop Challenge] User {userId} : Team {teamId} : Challenge {cc.Id}");
            var response = await _challengeServices.ForceStopChallenge(cc.Id, teamId, user!, contestId);
            return response.status switch
            {
                (int)HttpStatusCode.OK => Ok(response),
                (int)HttpStatusCode.BadRequest => BadRequest(response),
                (int)HttpStatusCode.NotFound => NotFound(response),
                _ => StatusCode((int)response.status, response)
            };
        }
        catch (HttpRequestException e)
        {
            await Console.Error.WriteLineAsync($"Error during stop challenge: {e.Message}");
            return BadRequest(new { error = "Failed to connect to stop API", error_detail = e.ToString() });
        }
    }

    [HttpPost("check-status")]
    [DuringCtfTimeOnly]
    public async Task<IActionResult> CheckChallengeStatus([FromBody] ChallengCheckStatusReqDTO statusReq)
    {
        if (statusReq == null || statusReq.challengeId <= 0)
            return BadRequest(new ChallengeDeployResponeDTO
            {
                success = false,
                message = "Invalid request parameters",
                status = (int)HttpStatusCode.BadRequest,
                pod_status = Enums.DeploymentStatusEnum.Failed
            });

        var userId = UserContext.UserId;
        var teamId = UserContext.TeamId;
        var contestId = UserContext.ContestId;

        if (teamId == 0)
            return BadRequest(new ChallengeDeployResponeDTO
            {
                success = false,
                message = "User no join team",
                status = (int)HttpStatusCode.BadRequest,
                pod_status = Enums.DeploymentStatusEnum.Failed
            });

        var cc = await _context.ContestsChallenges
            .AsNoTracking()
            .Include(c => c.BankChallenge)
            .FirstOrDefaultAsync(c => c.Id == statusReq.challengeId && c.ContestId == contestId);

        if (cc == null)
            return BadRequest(new ChallengeDeployResponeDTO
            {
                success = false,
                message = "Challenge not found",
                status = (int)HttpStatusCode.BadRequest,
                pod_status = Enums.DeploymentStatusEnum.Failed
            });

        int effectiveTeamId = teamId;
        if (cc.BankChallenge?.SharedInstant == true)
            effectiveTeamId = -2;

        var response = await _challengeServices.CheckChallengeStart(cc.Id, effectiveTeamId, contestId);
        return response.status switch
        {
            (int)HttpStatusCode.OK => Ok(response),
            (int)HttpStatusCode.BadRequest => BadRequest(response),
            (int)HttpStatusCode.NotFound => NotFound(response),
            _ => StatusCode((int)response.status, response)
        };
    }
}
