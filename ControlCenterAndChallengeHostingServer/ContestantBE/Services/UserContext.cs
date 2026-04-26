using ContestantBE.Interfaces;
using System.Security.Claims;

namespace ContestantBE.Services;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId => int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    public int TeamId
    {
        get
        {
            var teamIdClaim = _httpContextAccessor.HttpContext!.User.FindFirstValue("teamId");
            return string.IsNullOrEmpty(teamIdClaim) ? 0 : int.Parse(teamIdClaim);
        }
    }
    
    public int ContestId
    {
        get
        {
            var contestIdClaim = _httpContextAccessor.HttpContext!.User.FindFirstValue("contestId");
            return string.IsNullOrEmpty(contestIdClaim) ? 0 : int.Parse(contestIdClaim);
        }
    }
}
