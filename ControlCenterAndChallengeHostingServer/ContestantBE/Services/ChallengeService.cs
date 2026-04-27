using ContestantBE.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ResourceShared;
using ResourceShared.DTOs;
using ResourceShared.DTOs.Challenge;
using ResourceShared.DTOs.File;
using ResourceShared.DTOs.Topic;
using ResourceShared.Logger;
using ResourceShared.Models;
using ResourceShared.Utils;
using RestSharp;
using System.Net;

namespace ContestantBE.Services;

public interface IChallengeService
{
    Task<ChallengeDeployResponeDTO> ChallengeStart(ContestsChallenge cc, Challenge bank, User user, int teamId, int contestId);
    Task<ChallengeDeployResponeDTO> ForceStopChallenge(int contestChallengeId, int teamId, User user, int contestId);
    Task<ChallengeDeployResponeDTO> CheckChallengeStart(int contestChallengeId, int teamId, int contestId);
    Task<BaseResponseDTO<ChallengeByIdDTO>> GetById(int contestChallengeId, int teamId, User user, int contestId);
    Task<List<TopicDTO>> GetTopic(int teamId, int contestId);
    Task<List<ChallengeByCategoryDTO>> GetChallengeByCategories(string category_name, int teamId, int contestId);
    Task<List<ChallengeInstanceDTO>> GetAllInstances(int teamId, int contestId);
}

public class ChallengeService : IChallengeService
{
    private readonly AppDbContext _dbContext;
    private readonly RedisHelper _redisHelper;
    private readonly RedisLockHelper _redisLockHelper;
    private readonly ConfigHelper _configHelper;
    private readonly AppLogger _logger;
    private readonly MultiServiceConnector _multiServiceConnector;

    public ChallengeService(
        AppDbContext dbContext,
        RedisHelper redisHelper,
        RedisLockHelper redisLockHelper,
        ConfigHelper configHelper,
        AppLogger logger,
        MultiServiceConnector multiServiceConnector)
    {
        _dbContext = dbContext;
        _redisHelper = redisHelper;
        _redisLockHelper = redisLockHelper;
        _configHelper = configHelper;
        _logger = logger;
        _multiServiceConnector = multiServiceConnector;
    }

    private ChallengeRequirementsDTO? TryParseRequirements(string? requirementsJson, int contestChallengeId, int? teamId)
    {
        if (string.IsNullOrWhiteSpace(requirementsJson)) return null;
        try
        {
            return JsonConvert.DeserializeObject<ChallengeRequirementsDTO>(requirementsJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, null, teamId, new { contestChallengeId, requirements = requirementsJson });
            return null;
        }
    }

    private static bool IsUnlockedByPrerequisites(
        ChallengeRequirementsDTO? requirements,
        HashSet<int> solvedIds,
        HashSet<int> allIds)
    {
        var prerequisites = requirements?.prerequisites;
        if (prerequisites == null || prerequisites.Count == 0) return true;
        foreach (var prereqId in prerequisites)
        {
            if (!allIds.Contains(prereqId)) continue;
            if (!solvedIds.Contains(prereqId)) return false;
        }
        return true;
    }

    public async Task<BaseResponseDTO<ChallengeByIdDTO>> GetById(int contestChallengeId, int teamId, User user, int contestId)
    {
        // var contestChallengeIds = await _dbContext.ContestsChallenges
        //     .AsNoTracking()
        //     .Where(c => c.ContestId == contestId && c.State != "hidden")
        //     .Select(c => c.Id)
        //     .ToListAsync();

        var cc = await _dbContext.ContestsChallenges
            .AsNoTracking()
            .Include(c => c.BankChallenge!.Files)
            .FirstOrDefaultAsync(c => c.Id == contestChallengeId && c.ContestId == contestId);

        if (cc == null || cc.BankChallenge == null)
            return new BaseResponseDTO<ChallengeByIdDTO> { HttpStatusCode = HttpStatusCode.NotFound, Message = "Challenge not found" };

        var bank = cc.BankChallenge;

        if (cc.State == "hidden")
            return new BaseResponseDTO<ChallengeByIdDTO> { HttpStatusCode = HttpStatusCode.NotFound, Message = "Challenge is not available" };

        var requirementsObj = TryParseRequirements(bank.Requirements, contestChallengeId, teamId);

        // Get all contest challenge IDs for this contest first
        var contestChallengeIds = await _dbContext.ContestsChallenges
            .AsNoTracking()
            .Where(c => c.ContestId == contestId && c.State != "hidden")
            .Select(c => c.Id)
            .ToListAsync();

        // Prerequisites use bank IDs (requirements JSON references bank challenge IDs)
        var solvedBankIds = teamId > 0
            ? (await _dbContext.Solves
                .AsNoTracking()
                .Where(s => contestChallengeIds.Contains(s.ContestChallengeId) && s.TeamId == teamId && s.ContestChallenge.BankId.HasValue)
                .Select(s => s.ContestChallenge.BankId!.Value)
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        var allBankIds = (await _dbContext.ContestsChallenges
            .AsNoTracking()
            .Where(c => c.ContestId == contestId && c.BankId.HasValue)
            .Select(c => c.BankId!.Value)
            .ToListAsync()).ToHashSet();

        var isUnlocked = IsUnlockedByPrerequisites(requirementsObj, solvedBankIds, allBankIds);
        if (!isUnlocked && requirementsObj?.anonymize != true)
            return new BaseResponseDTO<ChallengeByIdDTO>
            {
                HttpStatusCode = HttpStatusCode.Forbidden,
                Message = "You don't have the permission to access this challenge. Complete the required challenges first."
            };

        var solveId = await _dbContext.Solves
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ContestChallengeId == contestChallengeId && s.TeamId == teamId);

        var attempts = await _dbContext.Submissions
            .AsNoTracking()
            .CountAsync(s => s.ContestChallengeId == contestChallengeId && s.TeamId == teamId);

        var deployedCount = await _dbContext.ChallengeStartTrackings
            .AsNoTracking()
            .CountAsync(d => d.ContestChallengeId == contestChallengeId && d.TeamId == teamId);

        var files = new List<object>();
        foreach (var file in bank.Files)
        {
            var token = new FileTokenDTOs { user_id = user.Id, team_id = teamId, file_id = file.Id };
            var fileUrl = $"/files?path={file.Location}&token={ItsDangerousCompatHelper.Dumps(token)}";
            files.Add(fileUrl);
        }

        var captainOnlyStart = _configHelper.GetConfig<bool>("captain_only_start_challenge", true);
        var captainOnlySubmit = _configHelper.GetConfig<bool>("captain_only_submit_challenge", true);
        var difficultyVisible = _configHelper.GetConfig<string>("challenge_difficulty_visibility", "disabled") == "enabled";

        string? nextName = null;
        if (cc.NextId.HasValue)
        {
            var nextCc = await _dbContext.ContestsChallenges
                .AsNoTracking()
                .Where(c => c.Id == cc.NextId.Value)
                .Select(c => new { c.Name, BankName = c.BankChallenge != null ? c.BankChallenge.Name : null })
                .FirstOrDefaultAsync();
            nextName = nextCc?.Name ?? nextCc?.BankName;
        }

        int? captainId = null;
        if (teamId > 0)
        {
            captainId = await _dbContext.Teams
                .AsNoTracking()
                .Where(t => t.Id == teamId)
                .Select(t => t.CaptainId)
                .FirstOrDefaultAsync();
        }

        var challengeData = new ChallengeDataDto
        {
            id = cc.Id,
            name = cc.Name ?? bank.Name ?? string.Empty,
            description = ChallengeHelper.ModifyDescription(bank),
            max_attempts = cc.MaxAttempts,
            attemps = attempts,
            max_deploy_count = cc.MaxDeployCount ?? bank.MaxDeployCount,
            deployed_count = deployedCount,
            category = bank.Category,
            time_limit = cc.TimeLimit,
            require_deploy = cc.RequireDeploy,
            connection_protocol = string.IsNullOrWhiteSpace(cc.ConnectionProtocol)
                ? (string.IsNullOrWhiteSpace(bank.ConnectionProtocol) ? "http" : bank.ConnectionProtocol)
                : cc.ConnectionProtocol,
            type = bank.Type,
            next_id = cc.NextId,
            next_name = nextName,
            solve_by_myteam = solveId != null,
            files = files,
            is_captain = captainId.HasValue && user.Id == captainId.Value,
            captain_only_start = captainOnlyStart,
            captain_only_submit = captainOnlySubmit,
            difficulty = difficultyVisible ? bank.Difficulty : null,
            shared_instance = bank.SharedInstant ?? false // Convert bool? to bool
        };

        int effectiveTeamId = teamId;
        if (bank.SharedInstant ?? false) effectiveTeamId = -2; // Convert bool? to bool

        var cacheKey = ChallengeHelper.GetCacheKey(contestId, contestChallengeId, effectiveTeamId);
        if (await _redisHelper.KeyExistsAsync(cacheKey))
        {
            var cachedValue = await _redisHelper.GetFromCacheAsync<ChallengeDeploymentCacheDTO>(cacheKey);
            if (cachedValue == null)
                return new BaseResponseDTO<ChallengeByIdDTO>
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    Data = new ChallengeByIdDTO { challenge = challengeData, is_started = false }
                };

            var userChal = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == cachedValue.user_id);

            if (cachedValue.challenge_id == contestChallengeId)
            {
                var timeRemaining = cachedValue.time_finished - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (timeRemaining < 0) timeRemaining = 0;
                return new BaseResponseDTO<ChallengeByIdDTO>
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    Message = $"Challenge was started by: {userChal?.Name}",
                    Data = new ChallengeByIdDTO
                    {
                        challenge = challengeData,
                        is_started = true,
                        challenge_url = cachedValue.challenge_url,
                        time_remaining = timeRemaining,
                        pod_status = cachedValue.status
                    }
                };
            }
        }

        return new BaseResponseDTO<ChallengeByIdDTO>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Data = new ChallengeByIdDTO { success = true, challenge = challengeData, is_started = false }
        };
    }

    public async Task<List<ChallengeByCategoryDTO>> GetChallengeByCategories(string category_name, int teamId, int contestId)
    {
        var contestChallenges = await _dbContext.ContestsChallenges
            .AsNoTracking()
            .Include(c => c.BankChallenge)
            .Where(c => c.ContestId == contestId
                && c.State != "hidden"
                // Match theo cc.Category (nếu có) hoặc BankChallenge.Category
                && (c.Category == category_name
                    || (c.Category == null && c.BankChallenge != null && c.BankChallenge.Category == category_name)))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.NextId,
                c.MaxAttempts,
                c.Value,
                c.TimeLimit,
                c.ConnectionProtocol,
                c.RequireDeploy,
                c.BankId,
                BankName = c.BankChallenge != null ? c.BankChallenge.Name : null,
                BankCategory = c.BankChallenge != null ? c.BankChallenge.Category : c.Category,
                BankType = c.BankChallenge != null ? c.BankChallenge.Type : c.Type,
                BankRequirements = c.BankChallenge != null ? c.BankChallenge.Requirements : c.Requirements,
                BankDifficulty = c.BankChallenge != null ? c.BankChallenge.Difficulty : c.Difficulty,
                BankConnectionProtocol = c.BankChallenge != null ? c.BankChallenge.ConnectionProtocol : c.ConnectionProtocol,
            })
            .ToListAsync();

        var difficultyVisible = _configHelper.GetConfig<string>("challenge_difficulty_visibility", "disabled") == "enabled";

        // Get all contest challenge IDs for this contest
        var contestChallengeIds = contestChallenges.Select(c => c.Id).ToList();

        var solvedBankIds = teamId > 0
            ? (await _dbContext.Solves
                .AsNoTracking()
                .Where(s => contestChallengeIds.Contains(s.ContestChallengeId) && s.TeamId == teamId && s.ContestChallenge.BankId.HasValue)
                .Select(s => s.ContestChallenge.BankId!.Value)
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        var solvedCcIds = teamId > 0
            ? (await _dbContext.Solves
                .AsNoTracking()
                .Where(s => contestChallengeIds.Contains(s.ContestChallengeId) && s.TeamId == teamId)
                .Select(s => s.ContestChallengeId)
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        var allBankIds = (await _dbContext.ContestsChallenges
            .AsNoTracking()
            .Where(c => c.ContestId == contestId && c.BankId.HasValue)
            .Select(c => c.BankId!.Value)
            .ToListAsync()).ToHashSet();

        var deployKeys = contestChallenges
            .Where(c => c.RequireDeploy && teamId > 0)
            .Select(c => ChallengeHelper.GetCacheKey(contestId, c.Id, teamId))
            .ToList();

        var deploymentCaches = deployKeys.Count != 0
            ? await _redisHelper.GetManyAsync<ChallengeDeploymentCacheDTO>(deployKeys)
            : new Dictionary<string, ChallengeDeploymentCacheDTO?>();

        var result = new List<ChallengeByCategoryDTO>();

        foreach (var cc in contestChallenges)
        {
            var requirementsObj = TryParseRequirements(cc.BankRequirements, cc.Id, teamId);
            var isUnlocked = IsUnlockedByPrerequisites(requirementsObj, solvedBankIds, allBankIds);
            if (!isUnlocked && requirementsObj?.anonymize != true) continue;

            string? podStatus = null;
            if (cc.RequireDeploy && teamId > 0)
            {
                var key = ChallengeHelper.GetCacheKey(contestId, cc.Id, teamId);
                if (deploymentCaches.TryGetValue(key, out var cache))
                    podStatus = cache?.status;
            }

            result.Add(new ChallengeByCategoryDTO
            {
                id = cc.Id,
                name = cc.Name ?? cc.BankName ?? string.Empty,
                next_id = cc.NextId,
                max_attempts = cc.MaxAttempts,
                value = cc.Value,
                category = cc.BankCategory,
                time_limit = cc.TimeLimit,
                connection_protocol = string.IsNullOrWhiteSpace(cc.ConnectionProtocol)
                    ? (cc.BankConnectionProtocol ?? "http")
                    : cc.ConnectionProtocol,
                type = cc.BankType,
                requirements = requirementsObj,
                solve_by_myteam = solvedCcIds.Contains(cc.Id),
                pod_status = podStatus,
                difficulty = difficultyVisible ? cc.BankDifficulty : null,
            });
        }

        return result;
    }

    public async Task<List<TopicDTO>> GetTopic(int teamId, int contestId)
    {
        // Bước 1: Lấy danh sách contest_challenge_id từ contest_id
        var contestChallengeIds = await _dbContext.ContestsChallenges
            .AsNoTracking()
            .Where(c => c.ContestId == contestId && c.State != "hidden")
            .Select(c => c.Id)
            .ToListAsync();

        // Bước 2: Dùng cc.Category trước (do C# portal set), fallback về BankChallenge.Category (do Python admin set)
        var challengeStats = await _dbContext.ContestsChallenges
            .AsNoTracking()
            .Where(c => c.ContestId == contestId && c.State != "hidden")
            .Select(c => new
            {
                EffectiveCategory = c.Category != null ? c.Category : (c.BankChallenge != null ? c.BankChallenge.Category : null)
            })
            .Where(x => x.EffectiveCategory != null)
            .GroupBy(x => x.EffectiveCategory!)
            .Select(g => new { Category = g.Key, Total = g.Count() })
            .ToListAsync();

        // Bước 3: Query solves chỉ với các contest_challenge_id hợp lệ
        var solvedStats = await _dbContext.Solves
            .AsNoTracking()
            .Where(s => contestChallengeIds.Contains(s.ContestChallengeId) 
                && s.TeamId == teamId
                && s.ContestChallenge != null
                && s.ContestChallenge.State != "hidden")
            .Select(s => new
            {
                s.ContestChallengeId,
                EffectiveCategory = s.ContestChallenge.Category != null
                    ? s.ContestChallenge.Category
                    : (s.ContestChallenge.BankChallenge != null ? s.ContestChallenge.BankChallenge.Category : null)
            })
            .Where(x => x.EffectiveCategory != null)
            .GroupBy(x => x.EffectiveCategory!)
            .Select(g => new { Category = g.Key, Solved = g.Select(x => x.ContestChallengeId).Distinct().Count() })
            .ToListAsync();

        var solvedDict = solvedStats.ToDictionary(x => x.Category, x => x.Solved);
        var topics = new List<TopicDTO>(challengeStats.Count);

        foreach (var stat in challengeStats)
        {
            var solved = solvedDict.TryGetValue(stat.Category, out var s) ? s : 0;
            topics.Add(new TopicDTO
            {
                topic_name = stat.Category,
                challenge_count = stat.Total,
                cleared = solved >= stat.Total
            });
        }
        return topics;
    }

    public async Task<ChallengeDeployResponeDTO> ChallengeStart(ContestsChallenge cc, Challenge bank, User user, int teamId, int contestId)
    {
        try
        {
            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var parameters = new ChallengeStartStopReqDTO
            {
                challengeId = bank.Id,
                contestChallengeId = cc.Id,
                contestId = contestId,
                teamId = teamId,
                userId = user.Id,
                unixTime = unixTime.ToString()
            };
            var data = new Dictionary<string, string>
            {
                { "challengeId", bank.Id.ToString() },
                { "teamId", teamId.ToString() },
                { "userId", user.Id.ToString() },
            };
            string secretKey = SecretKeyHelper.CreateSecretKey(unixTime, data);
            var headers = new Dictionary<string, string> { { "SecretKey", secretKey } };

            var body = await _multiServiceConnector.ExecuteRequest(
                ContestantBEConfigHelper.DeploymentCenterAPI,
                "/api/challenge/start",
                Method.Post,
                parameters,
                headers);

            if (body == null)
                return new ChallengeDeployResponeDTO
                {
                    status = (int)HttpStatusCode.BadRequest,
                    success = false,
                    message = "Deployment service is not responding. Please try again later."
                };

            var result = JsonConvert.DeserializeObject<ChallengeDeployResponeDTO>(body);
            if (result == null)
            {
                await Console.Out.WriteLineAsync("Failed to deserialize response");
                return new ChallengeDeployResponeDTO
                {
                    status = (int)HttpStatusCode.InternalServerError,
                    success = false,
                    message = "Error processing deployment data. Please contact support."
                };
            }
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex);
            return new ChallengeDeployResponeDTO
            {
                status = (int)HttpStatusCode.BadGateway,
                success = false,
                message = "Unable to connect to deployment server. Please contact support."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, user?.Id, teamId, new { bankChallengeId = bank.Id, contestChallengeId = cc.Id });
            return new ChallengeDeployResponeDTO
            {
                status = (int)HttpStatusCode.InternalServerError,
                success = false,
                message = "An unexpected error occurred."
            };
        }
    }

    public async Task<ChallengeDeployResponeDTO> ForceStopChallenge(int contestChallengeId, int teamId, User user, int contestId)
    {
        if (teamId <= 0)
            return new ChallengeDeployResponeDTO
            {
                status = (int)HttpStatusCode.BadRequest,
                success = false,
                message = "User team not found"
            };

        var lockKey = $"challenge:stop:team:{teamId}:challenge:{contestChallengeId}";
        var lockToken = Guid.NewGuid().ToString("N");
        var lockExpiry = TimeSpan.FromSeconds(30);
        var lockAcquired = false;

        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var data = new Dictionary<string, string>
        {
            { "challengeId", contestChallengeId.ToString() },
            { "teamId", teamId.ToString() },
        };
        var parameters = new ChallengeStartStopReqDTO
        {
            challengeId = contestChallengeId,
            contestChallengeId = contestChallengeId,
            contestId = contestId,
            teamId = teamId,
            unixTime = unixTime.ToString()
        };
        var secretKey = SecretKeyHelper.CreateSecretKey(unixTime, data);
        var headers = new Dictionary<string, string> { { "SecretKey", secretKey } };

        try
        {
            lockAcquired = await _redisLockHelper.AcquireLock(lockKey, lockToken, lockExpiry);
            if (!lockAcquired)
                return new ChallengeDeployResponeDTO
                {
                    status = (int)HttpStatusCode.Conflict,
                    success = false,
                    message = "Stop challenge request is already in progress"
                };

            var cacheKey = ChallengeHelper.GetCacheKey(contestId, contestChallengeId, teamId);
            if (!await _redisHelper.KeyExistsAsync(cacheKey))
                return new ChallengeDeployResponeDTO
                {
                    status = (int)HttpStatusCode.BadRequest,
                    success = false,
                    message = "Challenge not started or already stopped"
                };

            var body = await _multiServiceConnector.ExecuteRequest(
                ContestantBEConfigHelper.DeploymentCenterAPI,
                "/api/challenge/stop",
                Method.Post,
                parameters,
                headers);

            if (body == null)
                return new ChallengeDeployResponeDTO
                {
                    status = (int)HttpStatusCode.BadRequest,
                    success = false,
                    message = "No response from server when stopping challenge"
                };

            var result = JsonConvert.DeserializeObject<ChallengeDeployResponeDTO>(body);
            if (result == null)
            {
                await Console.Out.WriteLineAsync("Failed to deserialize response");
                return new ChallengeDeployResponeDTO
                {
                    status = (int)HttpStatusCode.InternalServerError,
                    success = false,
                    message = "Failed to parse server response"
                };
            }
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex);
            return new ChallengeDeployResponeDTO
            {
                status = (int)HttpStatusCode.BadGateway,
                success = false,
                message = "Connection url failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, user?.Id, teamId, new { contestChallengeId });
            return new ChallengeDeployResponeDTO
            {
                status = (int)HttpStatusCode.InternalServerError,
                success = false,
                message = "Unexpected error occurred while stopping challenge"
            };
        }
        finally
        {
            if (lockAcquired) await _redisLockHelper.ReleaseLock(lockKey, lockToken);
        }
    }

    public async Task<List<ChallengeInstanceDTO>> GetAllInstances(int teamId, int contestId)
    {
        var deployments = await _redisHelper
            .GetCacheByPatternAsync<ChallengeDeploymentCacheDTO>($"contest:{contestId}:deploy_challenge_*_{teamId}");

        if (deployments.Count == 0) return [];

        var contestChallengeIds = deployments.Select(x => x.challenge_id).Distinct().ToList();

        var contestChallenges = await _dbContext.ContestsChallenges
            .AsNoTracking()
            .Where(c => contestChallengeIds.Contains(c.Id) && c.ContestId == contestId)
            .Select(c => new
            {
                c.Id,
                EffectiveName = c.Name ?? (c.BankChallenge != null ? c.BankChallenge.Name : null),
                Category = c.BankChallenge != null ? c.BankChallenge.Category : null
            })
            .ToListAsync();

        var ccDict = contestChallenges.ToDictionary(c => c.Id);
        var result = new List<ChallengeInstanceDTO>(deployments.Count);

        foreach (var instance in deployments)
        {
            if (!ccDict.TryGetValue(instance.challenge_id, out var cc)) continue;
            result.Add(new ChallengeInstanceDTO
            {
                challenge_id = instance.challenge_id,
                challenge_name = cc.EffectiveName ?? string.Empty,
                category = cc.Category ?? string.Empty,
                status = instance.status ?? string.Empty,
                challenge_url = instance.challenge_url ?? "N/A",
                ready = instance.ready,
                age = instance.time_finished.ToString()
            });
        }
        return result;
    }

    public async Task<ChallengeDeployResponeDTO> CheckChallengeStart(int contestChallengeId, int teamId, int contestId)
    {
        try
        {
            var deploymentKey = ChallengeHelper.GetCacheKey(contestId, contestChallengeId, teamId);
            var deploymentCache = await _redisHelper.GetFromCacheAsync<ChallengeDeploymentCacheDTO>(deploymentKey);

            if (deploymentCache == null)
                return new ChallengeDeployResponeDTO
                {
                    success = false,
                    message = "No deployment info found.",
                    status = (int)HttpStatusCode.OK,
                    pod_status = Enums.DeploymentStatusEnum.NOT_FOUND,
                };

            var cc = await _dbContext.ContestsChallenges
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contestChallengeId && c.ContestId == contestId);

            if (cc == null)
                return new ChallengeDeployResponeDTO
                {
                    success = false,
                    message = "Challenge not found.",
                    status = (int)HttpStatusCode.NotFound,
                    pod_status = Enums.DeploymentStatusEnum.Failed
                };

            if (deploymentCache.status == Enums.DeploymentStatus.PENDING_DEPLOY)
                return new ChallengeDeployResponeDTO
                {
                    success = false,
                    message = "Challenge is waiting to deploy",
                    status = (int)HttpStatusCode.OK,
                    pod_status = Enums.DeploymentStatusEnum.PENDING_DEPLOY
                };

            if (deploymentCache.status == Enums.DeploymentStatus.PENDING)
                return new ChallengeDeployResponeDTO
                {
                    success = false,
                    message = "Challenge is currently deploying",
                    status = (int)HttpStatusCode.OK,
                    pod_status = Enums.DeploymentStatusEnum.Pending
                };

            if (deploymentCache.status == Enums.DeploymentStatus.RUNING && deploymentCache.ready)
                return new ChallengeDeployResponeDTO
                {
                    status = (int)HttpStatusCode.OK,
                    success = true,
                    message = "Pod is running.",
                    challenge_url = deploymentCache.challenge_url,
                    time_limit = cc.TimeLimit ?? -1,
                    pod_status = Enums.DeploymentStatusEnum.Running
                };

            if (deploymentCache.status == Enums.DeploymentStatus.DELETING)
                return new ChallengeDeployResponeDTO
                {
                    status = (int)HttpStatusCode.OK,
                    success = true,
                    message = "Pod is deleting.",
                    pod_status = Enums.DeploymentStatusEnum.Deleting
                };

            return new ChallengeDeployResponeDTO
            {
                success = false,
                message = "Pod is not running.",
                status = (int)HttpStatusCode.NotFound,
                pod_status = Enums.DeploymentStatusEnum.Failed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, null, teamId, new { contestChallengeId });
            return new ChallengeDeployResponeDTO
            {
                success = false,
                message = "Error during status check.",
                status = (int)HttpStatusCode.InternalServerError,
                pod_status = Enums.DeploymentStatusEnum.Failed
            };
        }
    }
}
