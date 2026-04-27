namespace ResourceShared.DTOs.Semester;

public class SemesterDTO
{
    public int Id { get; set; }
    public string SemesterName { get; set; } = null!;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int ContestCount { get; set; }
}

public class SemesterDetailDTO
{
    public int Id { get; set; }
    public string SemesterName { get; set; } = null!;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<ContestSummaryDTO> Contests { get; set; } = new();
}

public class ContestSummaryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string State { get; set; } = null!;
    public string UserMode { get; set; } = null!;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int ParticipantCount { get; set; }
}

public class ContestDetailDTO : ContestSummaryDTO
{
    public string? SemesterName { get; set; }
    public DateTime? FreezeScoreboardAt { get; set; }
    public bool IsParticipant { get; set; }
}
