using System.ComponentModel.DataAnnotations;

namespace ResourceShared.DTOs.Contest;

public class CreateContestDTO
{
    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens")]
    public string Slug { get; set; } = string.Empty;

    public string? SemesterName { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string UserMode { get; set; } = "users"; // users | teams
}

public class ContestDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public string? SemesterName { get; set; }
    public string State { get; set; } = "draft";
    public string UserMode { get; set; } = "users";
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ParticipantCount { get; set; }
    public int ChallengeCount { get; set; }
}

public class PullChallengesDTO
{
    [Required]
    public List<PullChallengeItemDTO> Challenges { get; set; } = new();
}

public class PullChallengeItemDTO
{
    [Required]
    public int BankChallengeId { get; set; }

    // Override fields (optional - if not provided, use bank values)
    
    // Basic properties
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Type { get; set; }
    public int? Difficulty { get; set; }
    public string? Requirements { get; set; }
    
    // Deploy configuration
    public string? ImageLink { get; set; }
    public string? DeployFile { get; set; }
    public int? CpuLimit { get; set; }
    public int? CpuRequest { get; set; }
    public int? MemoryLimit { get; set; }
    public int? MemoryRequest { get; set; }
    public bool? UseGvisor { get; set; }
    public int? MaxDeployCount { get; set; }
    
    // Metadata
    public bool? IsPublic { get; set; }
    
    // Challenge configuration
    public int? MaxAttempts { get; set; }
    public int? Value { get; set; }
    public string? State { get; set; } // visible | hidden
    public int? TimeLimit { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? TimeFinished { get; set; }
    public int? Cooldown { get; set; }
    public bool? RequireDeploy { get; set; }
    public string? ConnectionProtocol { get; set; }
    public string? ConnectionInfo { get; set; }
}

public class ImportParticipantsDTO
{
    [Required]
    public List<string> Emails { get; set; } = new();

    public string Role { get; set; } = "contestant"; // contestant | jury | challenge_writer
}

public class ImportParticipantsResultDTO
{
    public int TotalEmails { get; set; }
    public int NewUsersCreated { get; set; }
    public int ExistingUsersAdded { get; set; }
    public int AlreadyParticipants { get; set; }
    public List<string> FailedEmails { get; set; } = new();
}

public class BankChallengeDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Type { get; set; }
    public int? Difficulty { get; set; }
    public string? Requirements { get; set; }
    public int? AuthorId { get; set; }
    
    // Deploy configuration
    public string? ImageLink { get; set; }
    public string? DeployFile { get; set; }
    public int? CpuLimit { get; set; }
    public int? CpuRequest { get; set; }
    public int? MemoryLimit { get; set; }
    public int? MemoryRequest { get; set; }
    public bool? UseGvisor { get; set; }
    public int? MaxDeployCount { get; set; }
    public string? ConnectionProtocol { get; set; }
    
    // Metadata
    public bool IsPublic { get; set; }
    public int ImportCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    
    // Default challenge configuration
    public int? MaxAttempts { get; set; }
    public int? Value { get; set; }
    public string State { get; set; } = "visible";
    public int? TimeLimit { get; set; }
    public int? Cooldown { get; set; }
    public bool RequireDeploy { get; set; }
    public string? ConnectionInfo { get; set; }
}

public class ContestChallengeDTO
{
    public int Id { get; set; }
    public int ContestId { get; set; }
    public int? BankId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Type { get; set; }
    public int? Difficulty { get; set; }
    public string? Requirements { get; set; }
    
    // Deploy configuration
    public string? ImageLink { get; set; }
    public string? DeployFile { get; set; }
    public int? CpuLimit { get; set; }
    public int? CpuRequest { get; set; }
    public int? MemoryLimit { get; set; }
    public int? MemoryRequest { get; set; }
    public bool? UseGvisor { get; set; }
    public int? MaxDeployCount { get; set; }
    public string? ConnectionProtocol { get; set; }
    public string? ConnectionInfo { get; set; }
    
    // Challenge configuration
    public int? MaxAttempts { get; set; }
    public int? Value { get; set; }
    public string State { get; set; } = "visible";
    public int? TimeLimit { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? TimeFinished { get; set; }
    public int? Cooldown { get; set; }
    public bool RequireDeploy { get; set; }
    public string? DeployStatus { get; set; }
    
    // Metadata
    public bool IsPublic { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int SolveCount { get; set; }
}
