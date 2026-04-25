using Microsoft.EntityFrameworkCore;
using ResourceShared.Models;

namespace ResourceShared.Utils;

/// <summary>
/// Helper methods for multi-contest architecture
/// </summary>
public static class MultiContestHelper
{
    /// <summary>
    /// Get team of user in specific contest
    /// </summary>
    public static async Task<Team?> GetUserTeamInContest(
        this AppDbContext context, 
        int userId, 
        int contestId)
    {
        return await context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => 
                t.ContestId == contestId && 
                context.Set<UserTeam>().Any(ut => ut.UserId == userId && ut.TeamId == t.Id));
    }

    /// <summary>
    /// Get team ID of user in specific contest
    /// </summary>
    public static async Task<int?> GetUserTeamIdInContest(
        this AppDbContext context, 
        int userId, 
        int contestId)
    {
        var team = await GetUserTeamInContest(context, userId, contestId);
        return team?.Id;
    }

    /// <summary>
    /// Check if user is in a team in specific contest
    /// </summary>
    public static async Task<bool> IsUserInTeamInContest(
        this AppDbContext context, 
        int userId, 
        int contestId)
    {
        return await context.Teams
            .AnyAsync(t => 
                t.ContestId == contestId && 
                context.Set<UserTeam>().Any(ut => ut.UserId == userId && ut.TeamId == t.Id));
    }

    /// <summary>
    /// Get contest challenge from bank challenge ID
    /// </summary>
    public static async Task<ContestsChallenge?> GetContestChallengeFromBank(
        this AppDbContext context,
        int contestId,
        int bankChallengeId)
    {
        return await context.ContestsChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(cc => 
                cc.ContestId == contestId && 
                cc.BankId == bankChallengeId);
    }

    /// <summary>
    /// Get all teams of user across all contests
    /// </summary>
    public static async Task<List<Team>> GetUserTeams(
        this AppDbContext context,
        int userId)
    {
        var teamIds = await context.Set<UserTeam>()
            .Where(ut => ut.UserId == userId)
            .Select(ut => ut.TeamId)
            .ToListAsync();

        return await context.Teams
            .Where(t => teamIds.Contains(t.Id))
            .ToListAsync();
    }

    /// <summary>
    /// Add user to team
    /// </summary>
    public static async Task AddUserToTeam(
        this AppDbContext context,
        int userId,
        int teamId)
    {
        var exists = await context.Set<UserTeam>()
            .AnyAsync(ut => ut.UserId == userId && ut.TeamId == teamId);

        if (!exists)
        {
            context.Set<UserTeam>().Add(new UserTeam
            {
                UserId = userId,
                TeamId = teamId,
                JoinedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Remove user from team
    /// </summary>
    public static async Task RemoveUserFromTeam(
        this AppDbContext context,
        int userId,
        int teamId)
    {
        var userTeam = await context.Set<UserTeam>()
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId);

        if (userTeam != null)
        {
            context.Set<UserTeam>().Remove(userTeam);
            await context.SaveChangesAsync();
        }
    }
}
