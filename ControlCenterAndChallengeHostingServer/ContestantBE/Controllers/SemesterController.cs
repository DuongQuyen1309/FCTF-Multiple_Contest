using ContestantBE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceShared.DTOs.Semester;
using ResourceShared.Models;

namespace ContestantBE.Controllers;

public class SemesterController : BaseController
{
    private readonly AppDbContext _db;

    public SemesterController(IUserContext userContext, AppDbContext db) : base(userContext)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/semester
    /// Danh sách tất cả kỳ học (public).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetSemesters()
    {
        var semesters = await _db.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.Id)
            .Select(s => new SemesterDTO
            {
                Id = s.Id,
                SemesterName = s.SemesterName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                ContestCount = s.Contests.Count(c => c.State != "hidden")
            })
            .ToListAsync();

        return Ok(new { success = true, data = semesters });
    }

    /// <summary>
    /// GET /api/semester/{id}
    /// Chi tiết kỳ học kèm danh sách contests.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSemesterDetail(int id)
    {
        var sem = await _db.Semesters
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SemesterDetailDTO
            {
                Id = s.Id,
                SemesterName = s.SemesterName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Contests = s.Contests
                    .Where(c => c.State != "hidden")
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new ContestSummaryDTO
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug,
                        Description = c.Description,
                        State = c.State,
                        UserMode = c.UserMode,
                        StartTime = c.StartTime,
                        EndTime = c.EndTime,
                        ParticipantCount = c.Participants.Count
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (sem == null)
            return NotFound(new { success = false, message = "Kỳ học không tồn tại." });

        return Ok(new { success = true, data = sem });
    }

    /// <summary>
    /// GET /api/semester/contests
    /// Danh sách tất cả contests (không ẩn), có thể filter theo semester_id.
    /// </summary>
    [HttpGet("contests")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContests([FromQuery] int? semesterId, [FromQuery] string? state)
    {
        var query = _db.Contests
            .AsNoTracking()
            .Where(c => c.State != "hidden");

        if (semesterId.HasValue)
        {
            var sem = await _db.Semesters.AsNoTracking()
                .Where(s => s.Id == semesterId.Value)
                .Select(s => s.SemesterName)
                .FirstOrDefaultAsync();

            if (sem != null)
                query = query.Where(c => c.SemesterName == sem);
        }

        if (!string.IsNullOrEmpty(state))
            query = query.Where(c => c.State == state);

        var contests = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ContestSummaryDTO
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                State = c.State,
                UserMode = c.UserMode,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                ParticipantCount = c.Participants.Count
            })
            .ToListAsync();

        return Ok(new { success = true, data = contests });
    }

    /// <summary>
    /// GET /api/semester/contests/{id}
    /// Chi tiết một contest.
    /// </summary>
    [HttpGet("contests/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContestDetail(int id)
    {
        var userId = UserContext.UserId;

        var contest = await _db.Contests
            .AsNoTracking()
            .Where(c => c.Id == id && c.State != "hidden")
            .Select(c => new ContestDetailDTO
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                State = c.State,
                UserMode = c.UserMode,
                SemesterName = c.SemesterName,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                FreezeScoreboardAt = c.FreezeScoreboardAt,
                ParticipantCount = c.Participants.Count,
                IsParticipant = userId > 0 && c.Participants.Any(p => p.UserId == userId)
            })
            .FirstOrDefaultAsync();

        if (contest == null)
            return NotFound(new { success = false, message = "Contest không tồn tại." });

        return Ok(new { success = true, data = contest });
    }

    /// <summary>
    /// GET /api/semester/contests/by-slug/{slug}
    /// Chi tiết contest theo slug.
    /// </summary>
    [HttpGet("contests/by-slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContestBySlug(string slug)
    {
        var userId = UserContext.UserId;

        var contest = await _db.Contests
            .AsNoTracking()
            .Where(c => c.Slug == slug && c.State != "hidden")
            .Select(c => new ContestDetailDTO
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                State = c.State,
                UserMode = c.UserMode,
                SemesterName = c.SemesterName,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                FreezeScoreboardAt = c.FreezeScoreboardAt,
                ParticipantCount = c.Participants.Count,
                IsParticipant = userId > 0 && c.Participants.Any(p => p.UserId == userId)
            })
            .FirstOrDefaultAsync();

        if (contest == null)
            return NotFound(new { success = false, message = "Contest không tồn tại." });

        return Ok(new { success = true, data = contest });
    }
}
