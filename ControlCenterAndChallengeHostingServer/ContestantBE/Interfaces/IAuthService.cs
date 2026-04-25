using ResourceShared.DTOs;
using ResourceShared.DTOs.Auth;

namespace ContestantBE.Interfaces;

public interface IAuthService
{
    Task<BaseResponseDTO<AuthResponseDTO>> LoginContestant(LoginDTO loginDto);
    Task<BaseResponseDTO<SelectContestResponseDTO>> SelectContest(int userId, SelectContestDTO dto);
    Task<BaseResponseDTO<RegistrationMetadataDTO>> GetRegistrationMetadata();
    Task<BaseResponseDTO<string>> RegisterContestant(RegisterContestantDTO registerContestantDto);
    Task<BaseResponseDTO<string>> Logout(int userId);
    Task<BaseResponseDTO<string>> ChangePassword(int userId, ChangePasswordDTO changePasswordDto);
    
    // New method for generating JWT with contestId
    string GenerateJwtToken(int userId, string username, string email, string userType, int contestId, int? teamId);
}
