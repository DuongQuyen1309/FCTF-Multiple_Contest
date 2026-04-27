using Microsoft.EntityFrameworkCore;
using ResourceShared.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ResourceShared.Utils
{
    public static class DynamicChallengeHelper
    {
        private static async Task<int> GetSolveCount(AppDbContext context, int contestChallengeId, int contestId)
        {
            return await context.Solves
                .Join(context.Users,
                    solve => solve.UserId,
                    user => user.Id,
                    (solve, user) => new { solve, user })
                .Where(x => x.solve.ContestChallengeId == contestChallengeId
                    && x.solve.ContestId == contestId
                    && x.user.Hidden == false
                    && x.user.Banned == false)
                .CountAsync();
        }

        private static int Linear(DynamicChallenge dynamicChallenge, int solveCount)
        {
            if (solveCount != 0) solveCount -= 1;
            int value = (dynamicChallenge.Initial ?? 0) - ((dynamicChallenge.Decay ?? 0) * solveCount);
            value = (int)Math.Ceiling((double)value);
            if (value < dynamicChallenge.Minimum) value = dynamicChallenge.Minimum ?? 0;
            return value;
        }

        private static int Logarithmic(DynamicChallenge dynamicChallenge, int solveCount)
        {
            if (solveCount != 0) solveCount -= 1;
            int decay = dynamicChallenge.Decay ?? 1;
            if (decay == 0) decay = 1;
            int initial = dynamicChallenge.Initial ?? 0;
            int minimum = dynamicChallenge.Minimum ?? 0;
            double decaySquared = Math.Pow(decay, 2);
            double solveCountSquared = Math.Pow(solveCount, 2);
            double value = ((minimum - initial) / decaySquared) * solveCountSquared + initial;
            int finalValue = (int)Math.Ceiling(value);
            if (finalValue < minimum) finalValue = minimum;
            return finalValue;
        }

        public static async Task<int> RecalculateDynamicChallengeValue(
            AppDbContext context,
            int contestChallengeId,
            int contestId,
            RedisLockHelper? redisLockHelper = null)
        {
            try
            {
                string lockKey = $"challenge:dynamic:recalc:{contestId}:{contestChallengeId}";
                string lockToken = Guid.NewGuid().ToString();
                bool lockAcquired = redisLockHelper == null;
                if (redisLockHelper != null)
                {
                    var lockWaitTimeout = TimeSpan.FromSeconds(5);
                    var lockWaitStart = DateTime.UtcNow;
                    bool timeoutLogged = false;
                    while (!lockAcquired)
                    {
                        lockAcquired = await redisLockHelper.AcquireLock(lockKey, lockToken, TimeSpan.FromSeconds(10));
                        if (!lockAcquired)
                        {
                            if (DateTime.UtcNow - lockWaitStart > lockWaitTimeout && !timeoutLogged)
                            {
                                await Console.Error.WriteLineAsync($"[DynamicChallengeHelper] Lock wait exceeded for cc {contestChallengeId}, continuing.");
                                timeoutLogged = true;
                            }
                            await Task.Delay(100);
                        }
                    }
                }

                try
                {
                    var cc = await context.ContestsChallenges
                        .Include(c => c.BankChallenge!.DynamicChallenge)
                        .FirstOrDefaultAsync(c => c.Id == contestChallengeId);

                    if (cc?.BankChallenge?.DynamicChallenge == null)
                        return cc?.Value ?? 0;

                    var dynamicChallenge = cc.BankChallenge.DynamicChallenge;
                    var solveCount = await GetSolveCount(context, contestChallengeId, contestId);

                    string function = dynamicChallenge.Function ?? "logarithmic";
                    int newValue = function.ToLower() == "linear"
                        ? Linear(dynamicChallenge, solveCount)
                        : Logarithmic(dynamicChallenge, solveCount);

                    cc.Value = newValue;
                    context.ContestsChallenges.Update(cc);
                    await context.SaveChangesAsync();
                    return newValue;
                }
                finally
                {
                    if (redisLockHelper != null && lockAcquired)
                        await redisLockHelper.ReleaseLock(lockKey, lockToken);
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"[DynamicChallengeHelper] Error: {ex.Message}");
                throw;
            }
        }
    }
}
