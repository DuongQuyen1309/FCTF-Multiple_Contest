using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceShared.Models;

/// <summary>
/// Solve thành công. Inherits từ Submission.
/// Table: solves
/// Unique constraint đã đổi: (challenge_id, user_id) → (contest_challenge_id, user_id)
/// NOTE: ContestId property exists in model but NOT in database - used for business logic only
/// </summary>
public partial class Solf
{
    public int Id { get; set; }
    
    /// <summary>
    /// NOT MAPPED to database! Used for business logic only.
    /// To get contest_id, use: ContestChallenge.ContestId
    /// </summary>
    [NotMapped]
    public int ContestId { get; set; }

    /// <summary>FK → contests_challenges.id</summary>
    public int ContestChallengeId { get; set; }

    public int? UserId { get; set; }

    public int? TeamId { get; set; }

    /// <summary>
    /// NOT MAPPED to database! Use ContestChallenge.Contest instead.
    /// </summary>
    [NotMapped]
    public virtual Contest? Contest { get; set; }

    public virtual ContestsChallenge ContestChallenge { get; set; } = null!;

    public virtual Submission IdNavigation { get; set; } = null!;

    public virtual Team? Team { get; set; }

    public virtual User? User { get; set; }
}
