namespace ResourceShared.DTOs.Auth
{
    /// <summary>
    /// DTO for selecting a contest after initial login
    /// </summary>
    public class SelectContestDTO
    {
        public int ContestId { get; set; }
    }

    /// <summary>
    /// Response after selecting a contest
    /// </summary>
    public class SelectContestResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public int ContestId { get; set; }
        public string ContestName { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
    }
}
