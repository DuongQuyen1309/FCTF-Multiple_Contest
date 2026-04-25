using System;
using System.Collections.Generic;

namespace ResourceShared.Models;

/// <summary>
/// Instance của một challenge trong một contest cụ thể.
/// Table: contests_challenges
///
/// Mối quan hệ:
///   BankId  → challenges.id  (template gốc)
///   ContestId → contests.id
///   UserId → users.id (người upload challenge vào contest)
///
/// Tất cả dữ liệu runtime (submissions, solves, deploy histories,
/// start tracking, comments) đều FK vào ContestChallengeId (id của bảng này).
/// </summary>
public partial class ContestsChallenge
{
    public int Id { get; set; }

    // --- Contest relationship ---
    public int ContestId { get; set; }

    /// <summary>FK → challenges.id (bank template)</summary>
    public int? BankId { get; set; }

    // --- Challenge properties (same as Challenge table) ---
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Category { get; set; }

    public string? Type { get; set; }

    public int? Difficulty { get; set; }

    public string? Requirements { get; set; }

    /// <summary>FK → users.id (người upload challenge vào contest)</summary>
    public int? UserId { get; set; }

    // --- Deploy configuration ---
    public string? ImageLink { get; set; }

    public string? DeployFile { get; set; }

    public int? CpuLimit { get; set; }

    public int? CpuRequest { get; set; }

    public int? MemoryLimit { get; set; }

    public int? MemoryRequest { get; set; }

    public bool? UseGvisor { get; set; }

    public bool? HardenContainer { get; set; }

    public bool? SharedInstant { get; set; } = false;

    public int? MaxDeployCount { get; set; } = 0;

    // --- Metadata ---
    public bool IsPublic { get; set; } = false;

    public int ImportCount { get; set; } = 0;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    // --- Challenge configuration ---
    public int? MaxAttempts { get; set; } = 0;

    public int? Value { get; set; }

    /// <summary>visible | hidden</summary>
    public string State { get; set; } = "visible";

    public int? TimeLimit { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? TimeFinished { get; set; }

    public int? Cooldown { get; set; } = 0;

    public bool RequireDeploy { get; set; } = false;

    public string? DeployStatus { get; set; } = "CREATED";

    public string? ConnectionProtocol { get; set; } = "http";

    public string? ConnectionInfo { get; set; }

    /// <summary>Self-reference: chuỗi challenge trong contest</summary>
    public int? NextId { get; set; }

    // Navigation properties
    public virtual Contest Contest { get; set; } = null!;

    public virtual Challenge? BankChallenge { get; set; }

    /// <summary>Người upload challenge vào contest (FK: UserId)</summary>
    public virtual User? Creator { get; set; }

    public virtual ContestsChallenge? Next { get; set; }

    public virtual ICollection<ContestsChallenge> InverseNext { get; set; } = new List<ContestsChallenge>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<Solf> Solves { get; set; } = new List<Solf>();

    public virtual ICollection<DeployHistory> DeployHistories { get; set; } = new List<DeployHistory>();

    public virtual ICollection<ChallengeStartTracking> StartTrackings { get; set; } = new List<ChallengeStartTracking>();

    public virtual ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();

    public virtual ICollection<AwardBadge> AwardBadges { get; set; } = new List<AwardBadge>();
}
