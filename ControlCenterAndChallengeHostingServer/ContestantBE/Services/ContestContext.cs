namespace ContestantBE.Services;

/// <summary>
/// Scoped service to hold current contest context
/// </summary>
public class ContestContext
{
    public int ContestId { get; set; }
    public int UserId { get; set; }
    public int? TeamId { get; set; }
    public string? UserType { get; set; }
}

public interface IContestContext
{
    int ContestId { get; set; }
    int UserId { get; set; }
    int? TeamId { get; set; }
    string? UserType { get; set; }
}
