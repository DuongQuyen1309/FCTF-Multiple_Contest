using ResourceShared.DTOs;
using ResourceShared.DTOs.Contest;

namespace ContestantBE.Interfaces;

public interface IContestService
{
    Task<BaseResponseDTO<List<ContestDTO>>> GetAllContests(int userId);
    Task<BaseResponseDTO<ContestDTO>> GetContestById(int contestId, int userId);
    Task<BaseResponseDTO<ContestDTO>> CreateContest(CreateContestDTO dto, int userId);
    Task<BaseResponseDTO<List<ContestChallengeDTO>>> PullChallengesToContest(int contestId, PullChallengesDTO dto, int userId);
    Task<BaseResponseDTO<ImportParticipantsResultDTO>> ImportParticipants(int contestId, ImportParticipantsDTO dto, int userId);
    Task<BaseResponseDTO<List<BankChallengeDTO>>> GetBankChallenges(int userId);
    Task<BaseResponseDTO<List<ContestChallengeDTO>>> GetContestChallenges(int contestId, int userId);
}
