using ContestantBE.Interfaces;
using Microsoft.EntityFrameworkCore;
using ResourceShared.DTOs;
using ResourceShared.DTOs.Team;
using ResourceShared.Logger;
using ResourceShared.Models;
using ResourceShared.Utils;

namespace ContestantBE.Services;

public class TeamService : ITeamService
{
    private readonly AppDbContext _context;
    private readonly ScoreHelper _scoreHelper;
    private readonly AppLogger _logger;
    private readonly ContestContext _contestContext;

    public TeamService(
        AppDbContext context,
        ScoreHelper scoreHelper,
        AppLogger logger,
        ContestContext contestContext)
    {
        _context = context;
        _scoreHelper = scoreHelper;
        _logger = logger;
        _contestContext = contestContext;
    }

    public async Task<TeamScoreDTO?> GetTeamScore(int userId)
    {
        try
        {
            var team = await _context.GetUserTeamInContest(userId, _contestContext.ContestId);
            var bracketId = team?.BracketId;
            if (team == null) return null;

            // Get team members
            var teamMemberIds = await _context.Set<UserTeam>()
                .Where(ut => ut.TeamId == team.Id)
                .Select(ut => ut.UserId)
                .ToListAsync();

            var teamMembers = await _context.Users
                .Where(u => teamMemberIds.Contains(u.Id))
                .ToListAsync();

            var usersScore = await _scoreHelper.GetUsersScore(teamMembers, true);

            var members = new List<TeamMemberDTO>();
            foreach (var u in teamMembers)
            {
                _ = usersScore.TryGetValue(u, out int score);
                members.Add(new TeamMemberDTO
                {
                    Name = u.Name ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Score = score
                });
            }

            var challenges = await _context.ContestsChallenges
                .AsNoTracking()
                .Where(c => c.ContestId == _contestContext.ContestId && c.State == "visible")
                .Select(c => new { c.Value })
                .ToListAsync();

            var totalTeamsQuery = _context.Teams
                .AsNoTracking()
                .Where(t => t.ContestId == _contestContext.ContestId && t.Banned == false && t.Hidden == false);
            if (bracketId.HasValue)
                totalTeamsQuery = totalTeamsQuery.Where(t => t.BracketId == bracketId.Value);
            var totalTeams = await totalTeamsQuery.CountAsync();

            return new TeamScoreDTO
            {
                Name = team.Name ?? string.Empty,
                Place = await _scoreHelper.GetTeamPlace(team, true, bracketId),
                Members = members,
                Score = await _scoreHelper.GetTeamScore(team, true),
                ChallengeTotalScore = challenges.Sum(c => c.Value ?? 0),
                TotalTeams = totalTeams
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId);
            return null;
        }
    }

    public async Task<List<SubmissionDto>> GetTeamSolves(int userId)
    {
        try
        {
            var team = await _context.GetUserTeamInContest(userId, _contestContext.ContestId);

            if (team == null) return [];

            return [.. (await _scoreHelper.GetTeamSolves(team, true))
                .Select(s => new SubmissionDto
                {
                    Id = s.Id,
                    ChallengeId = s.ContestChallengeId,
                    Challenge = new ChallengeDto
                    {
                        Id = s?.ContestChallenge?.Id ?? default,
                        Name = s?.ContestChallenge?.Name ?? string.Empty,
                        Category = s?.ContestChallenge?.BankChallenge?.Category ?? string.Empty,
                        Value = s?.ContestChallenge?.Value
                    },
                    User = new UserDto
                    {
                        Id = s?.User?.Id ?? default,
                        Name = s?.User?.Name ?? string.Empty
                    },
                    Team = new TeamDto
                    {
                        Id = team.Id,
                        Name = team.Name ?? string.Empty,
                    },
                    Date = s.IdNavigation.Date,
                    Type = s.IdNavigation.Type,
                    Provided = null,
                    Ip = null
                })];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, userId);
            return [];
        }
    }
}
