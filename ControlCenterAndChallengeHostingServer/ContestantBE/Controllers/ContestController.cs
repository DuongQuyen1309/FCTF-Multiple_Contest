using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceShared.DTOs;
using ResourceShared.Models;
using ContestantBE.Services;
using ContestantBE.Interfaces;

namespace ContestantBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;

    public ContestController(AppDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    /// <summary>
    /// Get all contests (for admin) or contests user can access
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetContests()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var userType = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        IQueryable<Contest> query = _context.Contests;

        // If not admin, only show contests user is participant of
        if (userType != "admin")
        {
            var participantContestIds = await _context.ContestParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.ContestId)
                .ToListAsync();

            query = query.Where(c => participantContestIds.Contains(c.Id));
        }

        var contests = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.StartTime,
                c.EndTime,
                c.CreatedAt,
                c.OwnerId,
                ParticipantCount = _context.ContestParticipants.Count(cp => cp.ContestId == c.Id),
                ChallengeCount = _context.ContestsChallenges.Count(cc => cc.ContestId == c.Id)
            })
            .ToListAsync();

        return Ok(BaseResponseDTO<object>.Ok(contests, "Contests retrieved successfully"));
    }

    /// <summary>
    /// Get contest by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetContest(int id)
    {
        var contest = await _context.Contests
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.StartTime,
                c.EndTime,
                c.CreatedAt,
                c.OwnerId,
                Owner = c.Owner != null ? c.Owner.Name : null,
                ParticipantCount = _context.ContestParticipants.Count(cp => cp.ContestId == c.Id),
                ChallengeCount = _context.ContestsChallenges.Count(cc => cc.ContestId == c.Id)
            })
            .FirstOrDefaultAsync();

        if (contest == null)
        {
            return NotFound(BaseResponseDTO<object>.Fail("Contest not found"));
        }

        return Ok(BaseResponseDTO<object>.Ok(contest, "Contest retrieved successfully"));
    }

    /// <summary>
    /// Create new contest (admin only)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateContest([FromBody] CreateContestRequest request)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var userType = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userType != "admin")
        {
            return Forbid();
        }

        var contest = new Contest
        {
            Name = request.Name,
            Description = request.Description,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Contests.Add(contest);
        await _context.SaveChangesAsync();

        return Ok(BaseResponseDTO<object>.Ok(new { contest.Id }, "Contest created successfully"));
    }

    /// <summary>
    /// Update contest (admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateContest(int id, [FromBody] UpdateContestRequest request)
    {
        var userType = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userType != "admin")
        {
            return Forbid();
        }

        var contest = await _context.Contests.FindAsync(id);
        if (contest == null)
        {
            return NotFound(BaseResponseDTO<object>.Fail("Contest not found"));
        }

        contest.Name = request.Name ?? contest.Name;
        contest.Description = request.Description ?? contest.Description;
        contest.StartTime = request.StartTime ?? contest.StartTime;
        contest.EndTime = request.EndTime ?? contest.EndTime;

        await _context.SaveChangesAsync();

        return Ok(BaseResponseDTO<object>.Ok(null, "Contest updated successfully"));
    }

    /// <summary>
    /// Delete contest (admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteContest(int id)
    {
        var userType = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userType != "admin")
        {
            return Forbid();
        }

        var contest = await _context.Contests.FindAsync(id);
        if (contest == null)
        {
            return NotFound(BaseResponseDTO<object>.Fail("Contest not found"));
        }

        _context.Contests.Remove(contest);
        await _context.SaveChangesAsync();

        return Ok(BaseResponseDTO<object>.Ok(null, "Contest deleted successfully"));
    }

    /// <summary>
    /// Select contest and get new JWT token with contestId
    /// </summary>
    [HttpPost("select")]
    [Authorize]
    public async Task<IActionResult> SelectContest([FromBody] SelectContestRequest request)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        // Check if contest exists
        var contest = await _context.Contests.FindAsync(request.ContestId);
        if (contest == null)
        {
            return NotFound(BaseResponseDTO<object>.Fail("Contest not found"));
        }

        // Check if user is participant (unless admin)
        var userType = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userType != "admin")
        {
            var isParticipant = await _context.ContestParticipants
                .AnyAsync(cp => cp.ContestId == request.ContestId && cp.UserId == userId);

            if (!isParticipant)
            {
                return Forbid();
            }
        }

        // Get user
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(BaseResponseDTO<object>.Fail("User not found"));
        }

        // Get user's team in this contest
        var team = await _context.Teams
            .FirstOrDefaultAsync(t => 
                t.ContestId == request.ContestId && 
                _context.Set<UserTeam>().Any(ut => ut.UserId == userId && ut.TeamId == t.Id));

        // Generate new JWT with contestId
        var jwt = _authService.GenerateJwtToken(
            user.Id,
            user.Name ?? "",
            user.Email ?? "",
            user.Type ?? "user",
            request.ContestId,
            team?.Id);

        return Ok(BaseResponseDTO<object>.Ok(new
        {
            token = jwt,
            contestId = request.ContestId,
            contestName = contest.Name,
            teamId = team?.Id,
            teamName = team?.Name
        }, "Contest selected successfully"));
    }
}

public class CreateContestRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class UpdateContestRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class SelectContestRequest
{
    public int ContestId { get; set; }
}
