using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceShared.DTOs.Challenge
{
    public class ChallengCheckStatusReqDTO
    {
        /// <summary>contestChallengeId (contests_challenges.id)</summary>
        public int challengeId { get; set; }
        public int contestId { get; set; }
        public int teamId { get; set; }
        public string? unixTime { get; set; }
    }
}
