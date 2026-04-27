using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceShared.DTOs.Challenge
{
    public class ChallengeStartStopReqDTO
    {
        /// <summary>Bank challenge ID — used by DeploymentCenter to look up container image.</summary>
        public int challengeId { get; set; }
        /// <summary>contests_challenges.id — used for scoped Redis cache key.</summary>
        public int contestChallengeId { get; set; }
        /// <summary>contests.id — used for scoped Redis cache key.</summary>
        public int contestId { get; set; }
        public string challengeName { get; set; } = string.Empty;
        public int teamId { get; set; }
        public int? userId { get; set; }
        public string? unixTime { get; set; }
        public string? ns { get; set; }
    }
}
