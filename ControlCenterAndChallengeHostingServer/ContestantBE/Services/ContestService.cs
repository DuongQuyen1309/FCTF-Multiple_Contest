using ContestantBE.Interfaces;
using Microsoft.EntityFrameworkCore;
using ResourceShared.DTOs;
using ResourceShared.DTOs.Contest;
using ResourceShared.Logger;
using ResourceShared.Models;
using ResourceShared.Utils;
using System.Net;

namespace ContestantBE.Services;

public class ContestService : IContestService
{
    private readonly AppDbContext _context;
    private readonly AppLogger _logger;

    public ContestService(AppDbContext context, AppLogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BaseResponseDTO<List<ContestDTO>>> GetAllContests(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new BaseResponseDTO<List<ContestDTO>>
                {
                    Success = false,
                    Message = "User not found",
                    HttpStatusCode = HttpStatusCode.NotFound
                };
            }

            // Get contests where user is a participant
            var participantContestIds = await _context.ContestParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.ContestId)
                .ToListAsync();

            // Admin sees all contests, teachers see their own + participated contests, users see only participated contests
            var query = _context.Contests.AsQueryable();

            if (user.Type == "admin")
            {
                // Admin sees all contests
                query = query;
            }
            else if (user.Type == "teacher")
            {
                // Teachers see contests they own or participate in
                query = query.Where(c => c.OwnerId == userId || participantContestIds.Contains(c.Id));
            }
            else
            {
                // Regular users see only contests they participate in
                query = query.Where(c => participantContestIds.Contains(c.Id));
            }

            var contests = await query
                .Include(c => c.Owner)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ContestDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Slug = c.Slug,
                    OwnerId = c.OwnerId,
                    OwnerName = c.Owner != null ? c.Owner.Name : null,
                    SemesterName = c.SemesterName,
                    State = c.State,
                    UserMode = c.UserMode,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    CreatedAt = c.CreatedAt,
                    ParticipantCount = c.Participants.Count,
                    ChallengeCount = c.ContestsChallenges.Count
                })
                .ToListAsync();

            return new BaseResponseDTO<List<ContestDTO>>
            {
                Success = true,
                Data = contests,
                HttpStatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId, null, new { action = "GetAllContests" });
            return new BaseResponseDTO<List<ContestDTO>>
            {
                Success = false,
                Message = "Failed to retrieve contests",
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<BaseResponseDTO<ContestDTO>> GetContestById(int contestId, int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new BaseResponseDTO<ContestDTO>
                {
                    Success = false,
                    Message = "User not found",
                    HttpStatusCode = HttpStatusCode.NotFound
                };
            }

            var contest = await _context.Contests
                .Include(c => c.Owner)
                .Include(c => c.Participants)
                .Include(c => c.ContestsChallenges)
                .FirstOrDefaultAsync(c => c.Id == contestId);

            if (contest == null)
            {
                return new BaseResponseDTO<ContestDTO>
                {
                    Success = false,
                    Message = "Contest not found",
                    HttpStatusCode = HttpStatusCode.NotFound
                };
            }

            // Check access permission
            if (user.Type != "admin" && contest.OwnerId != userId)
            {
                var isParticipant = await _context.ContestParticipants
                    .AnyAsync(cp => cp.ContestId == contestId && cp.UserId == userId);

                if (!isParticipant)
                {
                    return new BaseResponseDTO<ContestDTO>
                    {
                        Success = false,
                        Message = "Access denied",
                        HttpStatusCode = HttpStatusCode.Forbidden
                    };
                }
            }

            var contestDto = new ContestDTO
            {
                Id = contest.Id,
                Name = contest.Name,
                Description = contest.Description,
                Slug = contest.Slug,
                OwnerId = contest.OwnerId,
                OwnerName = contest.Owner?.Name,
                SemesterName = contest.SemesterName,
                State = contest.State,
                UserMode = contest.UserMode,
                StartTime = contest.StartTime,
                EndTime = contest.EndTime,
                CreatedAt = contest.CreatedAt,
                ParticipantCount = contest.Participants.Count,
                ChallengeCount = contest.ContestsChallenges.Count
            };

            return new BaseResponseDTO<ContestDTO>
            {
                Success = true,
                Data = contestDto,
                HttpStatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId, null, new { action = "GetContestById", contestId });
            return new BaseResponseDTO<ContestDTO>
            {
                Success = false,
                Message = "Failed to retrieve contest",
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<BaseResponseDTO<ContestDTO>> CreateContest(CreateContestDTO dto, int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || (user.Type != "admin" && user.Type != "teacher"))
            {
                return new BaseResponseDTO<ContestDTO>
                {
                    Success = false,
                    Message = "Only admin or teacher can create contests",
                    HttpStatusCode = HttpStatusCode.Forbidden
                };
            }

            // Check if slug already exists
            var existingContest = await _context.Contests
                .FirstOrDefaultAsync(c => c.Slug == dto.Slug);

            if (existingContest != null)
            {
                return new BaseResponseDTO<ContestDTO>
                {
                    Success = false,
                    Message = "Contest with this slug already exists",
                    HttpStatusCode = HttpStatusCode.BadRequest
                };
            }

            var contest = new Contest
            {
                Name = dto.Name,
                Description = dto.Description,
                Slug = dto.Slug,
                OwnerId = userId,
                SemesterName = dto.SemesterName,
                State = "draft",
                UserMode = dto.UserMode,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CreatedAt = DateTime.UtcNow
            };

            _context.Contests.Add(contest);
            await _context.SaveChangesAsync();

            var contestDto = new ContestDTO
            {
                Id = contest.Id,
                Name = contest.Name,
                Description = contest.Description,
                Slug = contest.Slug,
                OwnerId = contest.OwnerId,
                OwnerName = user.Name,
                SemesterName = contest.SemesterName,
                State = contest.State,
                UserMode = contest.UserMode,
                StartTime = contest.StartTime,
                EndTime = contest.EndTime,
                CreatedAt = contest.CreatedAt,
                ParticipantCount = 0,
                ChallengeCount = 0
            };

            return new BaseResponseDTO<ContestDTO>
            {
                Success = true,
                Data = contestDto,
                Message = "Contest created successfully",
                HttpStatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId, null, new { action = "CreateContest", dto });
            return new BaseResponseDTO<ContestDTO>
            {
                Success = false,
                Message = "Failed to create contest",
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<BaseResponseDTO<List<ContestChallengeDTO>>> PullChallengesToContest(
        int contestId,
        PullChallengesDTO dto,
        int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new BaseResponseDTO<List<ContestChallengeDTO>>
                {
                    Success = false,
                    Message = "User not found",
                    HttpStatusCode = HttpStatusCode.NotFound
                };
            }

            var contest = await _context.Contests
                .Include(c => c.ContestsChallenges)
                .FirstOrDefaultAsync(c => c.Id == contestId);

            if (contest == null)
            {
                return new BaseResponseDTO<List<ContestChallengeDTO>>
                {
                    Success = false,
                    Message = "Contest not found",
                    HttpStatusCode = HttpStatusCode.NotFound
                };
            }

            // Check permission
            if (user.Type != "admin" && contest.OwnerId != userId)
            {
                return new BaseResponseDTO<List<ContestChallengeDTO>>
                {
                    Success = false,
                    Message = "Only contest owner or admin can pull challenges",
                    HttpStatusCode = HttpStatusCode.Forbidden
                };
            }

            var bankChallengeIds = dto.Challenges.Select(c => c.BankChallengeId).ToList();
            var bankChallenges = await _context.Challenges
                .Where(c => bankChallengeIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            var addedChallenges = new List<ContestChallengeDTO>();

            foreach (var pullItem in dto.Challenges)
            {
                if (!bankChallenges.TryGetValue(pullItem.BankChallengeId, out var bankChallenge))
                {
                    // Skip if bank challenge not found
                    continue;
                }

                // Check if already pulled
                var existing = contest.ContestsChallenges
                    .FirstOrDefault(cc => cc.BankId == pullItem.BankChallengeId);

                if (existing != null)
                {
                    // Skip if already in contest
                    continue;
                }

                // Create contest challenge - copy all properties from bank challenge
                // If teacher configures properties, use configured values; otherwise use bank defaults
                var contestChallenge = new ContestsChallenge
                {
                    ContestId = contestId,
                    BankId = pullItem.BankChallengeId,
                    UserId = userId, // Teacher who pulled the challenge
                    
                    // Basic properties - use override or bank default
                    Name = pullItem.Name ?? bankChallenge.Name,
                    Description = pullItem.Description ?? bankChallenge.Description,
                    Category = pullItem.Category ?? bankChallenge.Category,
                    Type = pullItem.Type ?? bankChallenge.Type,
                    Difficulty = pullItem.Difficulty ?? bankChallenge.Difficulty,
                    Requirements = pullItem.Requirements ?? bankChallenge.Requirements,
                    
                    // Deploy configuration - copy from bank
                    ImageLink = pullItem.ImageLink ?? bankChallenge.ImageLink,
                    DeployFile = pullItem.DeployFile ?? bankChallenge.DeployFile,
                    CpuLimit = pullItem.CpuLimit ?? bankChallenge.CpuLimit,
                    CpuRequest = pullItem.CpuRequest ?? bankChallenge.CpuRequest,
                    MemoryLimit = pullItem.MemoryLimit ?? bankChallenge.MemoryLimit,
                    MemoryRequest = pullItem.MemoryRequest ?? bankChallenge.MemoryRequest,
                    UseGvisor = pullItem.UseGvisor ?? bankChallenge.UseGvisor,
                    MaxDeployCount = pullItem.MaxDeployCount ?? bankChallenge.MaxDeployCount ?? 0,
                    
                    // Metadata
                    IsPublic = pullItem.IsPublic ?? bankChallenge.IsPublic,
                    ImportCount = 0, // Reset for contest instance
                    CreatedAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                    
                    // Challenge configuration - use override or bank default
                    MaxAttempts = pullItem.MaxAttempts ?? bankChallenge.MaxAttempt ?? 0,
                    Value = pullItem.Value ?? bankChallenge.Value ?? 100,
                    State = pullItem.State ?? bankChallenge.State ?? "visible",
                    TimeLimit = pullItem.TimeLimit ?? bankChallenge.TimeLimit,
                    StartTime = pullItem.StartTime ?? bankChallenge.StartTime,
                    TimeFinished = pullItem.TimeFinished ?? bankChallenge.TimeFinished,
                    Cooldown = pullItem.Cooldown ?? bankChallenge.Cooldown ?? 0,
                    RequireDeploy = pullItem.RequireDeploy ?? bankChallenge.RequireDeploy,
                    DeployStatus = "CREATED",
                    ConnectionProtocol = pullItem.ConnectionProtocol ?? bankChallenge.ConnectionProtocol ?? "http",
                    ConnectionInfo = pullItem.ConnectionInfo ?? bankChallenge.ConnectionInfo
                };

                _context.ContestsChallenges.Add(contestChallenge);
                await _context.SaveChangesAsync();

                addedChallenges.Add(new ContestChallengeDTO
                {
                    Id = contestChallenge.Id,
                    ContestId = contestChallenge.ContestId,
                    BankId = contestChallenge.BankId,
                    Name = contestChallenge.Name ?? string.Empty,
                    Category = contestChallenge.Category,
                    Description = contestChallenge.Description,
                    Value = contestChallenge.Value,
                    State = contestChallenge.State,
                    MaxAttempts = contestChallenge.MaxAttempts,
                    RequireDeploy = contestChallenge.RequireDeploy,
                    SolveCount = 0
                });
            }

            return new BaseResponseDTO<List<ContestChallengeDTO>>
            {
                Success = true,
                Data = addedChallenges,
                Message = $"Successfully pulled {addedChallenges.Count} challenges to contest",
                HttpStatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId, null, new { action = "PullChallengesToContest", contestId, dto });
            return new BaseResponseDTO<List<ContestChallengeDTO>>
            {
                Success = false,
                Message = "Failed to pull challenges to contest",
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<BaseResponseDTO<ImportParticipantsResultDTO>> ImportParticipants(
        int contestId,
        ImportParticipantsDTO dto,
        int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new BaseResponseDTO<ImportParticipantsResultDTO>
                {
                    Success = false,
                    Message = "User not found",
                    HttpStatusCode = HttpStatusCode.NotFound
                };
            }

            var contest = await _context.Contests.FindAsync(contestId);
            if (contest == null)
            {
                return new BaseResponseDTO<ImportParticipantsResultDTO>
                {
                    Success = false,
                    Message = "Contest not found",
                    HttpStatusCode = HttpStatusCode.NotFound
                };
            }

            // Check permission
            if (user.Type != "admin" && contest.OwnerId != userId)
            {
                return new BaseResponseDTO<ImportParticipantsResultDTO>
                {
                    Success = false,
                    Message = "Only contest owner or admin can import participants",
                    HttpStatusCode = HttpStatusCode.Forbidden
                };
            }

            var result = new ImportParticipantsResultDTO
            {
                TotalEmails = dto.Emails.Count
            };

            // Get existing participants
            var existingParticipants = (await _context.ContestParticipants
                .Where(cp => cp.ContestId == contestId)
                .Select(cp => cp.UserId)
                .ToListAsync())
                .ToHashSet();

            foreach (var email in dto.Emails)
            {
                try
                {
                    var normalizedEmail = email.Trim().ToLower();
                    if (string.IsNullOrWhiteSpace(normalizedEmail))
                    {
                        result.FailedEmails.Add(email);
                        continue;
                    }

                    // Find or create user
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

                    User targetUser;
                    if (existingUser == null)
                    {
                        // Create new user with email only
                        targetUser = new User
                        {
                            Email = normalizedEmail,
                            Name = normalizedEmail.Split('@')[0], // Use email prefix as name
                            Password = SHA256Helper.HashPasswordPythonStyle(Guid.NewGuid().ToString()), // Random password
                            Type = "user",
                            Verified = false,
                            Hidden = false,
                            Banned = false,
                            Created = DateTime.UtcNow
                        };

                        _context.Users.Add(targetUser);
                        await _context.SaveChangesAsync();
                        result.NewUsersCreated++;
                    }
                    else
                    {
                        targetUser = existingUser;
                        result.ExistingUsersAdded++;
                    }

                    // Check if already participant
                    if (existingParticipants.Contains(targetUser.Id))
                    {
                        result.AlreadyParticipants++;
                        result.ExistingUsersAdded--; // Adjust count
                        continue;
                    }

                    // Add as participant
                    var participant = new ContestParticipant
                    {
                        ContestId = contestId,
                        UserId = targetUser.Id,
                        Role = dto.Role,
                        Score = 0,
                        JoinedAt = DateTime.UtcNow
                    };

                    _context.ContestParticipants.Add(participant);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, userId, null, new { action = "ImportParticipant", email });
                    result.FailedEmails.Add(email);
                }
            }

            return new BaseResponseDTO<ImportParticipantsResultDTO>
            {
                Success = true,
                Data = result,
                Message = $"Imported {result.NewUsersCreated + result.ExistingUsersAdded} participants",
                HttpStatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId, null, new { action = "ImportParticipants", contestId, dto });
            return new BaseResponseDTO<ImportParticipantsResultDTO>
            {
                Success = false,
                Message = "Failed to import participants",
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<BaseResponseDTO<List<BankChallengeDTO>>> GetBankChallenges(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || (user.Type != "admin" && user.Type != "teacher"))
            {
                return new BaseResponseDTO<List<BankChallengeDTO>>
                {
                    Success = false,
                    Message = "Access denied",
                    HttpStatusCode = HttpStatusCode.Forbidden
                };
            }

            var challenges = await _context.Challenges
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Name)
                .Select(c => new BankChallengeDTO
                {
                    Id = c.Id,
                    Name = c.Name ?? string.Empty,
                    Description = c.Description,
                    Category = c.Category,
                    Type = c.Type,
                    Difficulty = c.Difficulty,
                    Requirements = c.Requirements,
                    AuthorId = c.AuthorId,
                    
                    // Deploy configuration
                    ImageLink = c.ImageLink,
                    DeployFile = c.DeployFile,
                    CpuLimit = c.CpuLimit,
                    CpuRequest = c.CpuRequest,
                    MemoryLimit = c.MemoryLimit,
                    MemoryRequest = c.MemoryRequest,
                    UseGvisor = c.UseGvisor,
                    MaxDeployCount = c.MaxDeployCount,
                    ConnectionProtocol = c.ConnectionProtocol,
                    
                    // Metadata
                    IsPublic = c.IsPublic,
                    ImportCount = c.ImportCount,
                    CreatedAt = c.CreatedAt,
                    
                    // Default challenge configuration
                    MaxAttempts = c.MaxAttempt,
                    Value = c.Value,
                    State = c.State,
                    TimeLimit = c.TimeLimit,
                    Cooldown = c.Cooldown,
                    RequireDeploy = c.RequireDeploy,
                    ConnectionInfo = c.ConnectionInfo
                })
                .ToListAsync();

            return new BaseResponseDTO<List<BankChallengeDTO>>
            {
                Success = true,
                Data = challenges,
                HttpStatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId, null, new { action = "GetBankChallenges" });
            return new BaseResponseDTO<List<BankChallengeDTO>>
            {
                Success = false,
                Message = "Failed to retrieve bank challenges",
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<BaseResponseDTO<List<ContestChallengeDTO>>> GetContestChallenges(int contestId, int userId)
    {
        try
        {
            var contest = await _context.Contests.FindAsync(contestId);
            if (contest == null)
            {
                return new BaseResponseDTO<List<ContestChallengeDTO>>
                {
                    Success = false,
                    Message = "Contest not found",
                    HttpStatusCode = HttpStatusCode.NotFound
                };
            }

            var challenges = await _context.ContestsChallenges
                .Where(cc => cc.ContestId == contestId)
                .Include(cc => cc.BankChallenge)
                .Include(cc => cc.Solves)
                .OrderBy(cc => cc.Category)
                .ThenBy(cc => cc.Name)
                .Select(cc => new ContestChallengeDTO
                {
                    Id = cc.Id,
                    ContestId = cc.ContestId,
                    BankId = cc.BankId,
                    Name = cc.Name ?? string.Empty,
                    Description = cc.Description,
                    Category = cc.Category,
                    Type = cc.Type,
                    Difficulty = cc.Difficulty,
                    Requirements = cc.Requirements,
                    
                    // Deploy configuration
                    ImageLink = cc.ImageLink,
                    DeployFile = cc.DeployFile,
                    CpuLimit = cc.CpuLimit,
                    CpuRequest = cc.CpuRequest,
                    MemoryLimit = cc.MemoryLimit,
                    MemoryRequest = cc.MemoryRequest,
                    UseGvisor = cc.UseGvisor,
                    MaxDeployCount = cc.MaxDeployCount,
                    ConnectionProtocol = cc.ConnectionProtocol,
                    ConnectionInfo = cc.ConnectionInfo,
                    
                    // Challenge configuration
                    MaxAttempts = cc.MaxAttempts,
                    Value = cc.Value,
                    State = cc.State,
                    TimeLimit = cc.TimeLimit,
                    StartTime = cc.StartTime,
                    TimeFinished = cc.TimeFinished,
                    Cooldown = cc.Cooldown,
                    RequireDeploy = cc.RequireDeploy,
                    DeployStatus = cc.DeployStatus,
                    
                    // Metadata
                    IsPublic = cc.IsPublic,
                    CreatedAt = cc.CreatedAt,
                    SolveCount = cc.Solves.Count
                })
                .ToListAsync();

            return new BaseResponseDTO<List<ContestChallengeDTO>>
            {
                Success = true,
                Data = challenges,
                HttpStatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId, null, new { action = "GetContestChallenges", contestId });
            return new BaseResponseDTO<List<ContestChallengeDTO>>
            {
                Success = false,
                Message = "Failed to retrieve contest challenges",
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
        }
    }
}
