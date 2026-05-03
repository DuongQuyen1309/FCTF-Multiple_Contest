using ContestantBE.Interfaces;
using ContestantBE.Utils;
using ResourceShared.DTOs.File;
using ResourceShared.Models;
using ResourceShared.Utils;
using ResourceShared.Logger;
using Microsoft.EntityFrameworkCore;

namespace ContestantBE.Services;

public class FileService : IFileService
{
    private readonly string _nfsMountPath;
    private readonly AppDbContext _context;
    private readonly AppLogger _logger;
    private readonly ContestContext _contestContext;
    
    public FileService(AppDbContext context, AppLogger logger, ContestContext contestContext)
    {
        _nfsMountPath = ContestantBEConfigHelper.NFS_MOUNT_PATH;
        _context = context;
        _logger = logger;
        _contestContext = contestContext;
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" => "application/vnd.ms-word",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".csv" => "text/csv",
            ".zip" => "application/zip",
            ".tar" => "application/x-tar",
            ".gz" => "application/gzip",
            _ => "application/octet-stream"
        };
    }
    public async Task<FileResult> GetFileAsync(string path, string token, int user_id)
    {
        try
        {
            // Validate token 
            var fileToken = ItsDangerousCompatHelper.Loads<FileTokenDTOs>(token);
            if (fileToken == null || fileToken.user_id != user_id)
            {
                await Console.Out.WriteLineAsync("Token validation failed - user_id mismatch");
                return new FileResult { Success = false, Message = "Invalid or expired token" };
            }

            //  Basic path guards 
            if (string.IsNullOrWhiteSpace(path))
                return new FileResult { Success = false, Message = "File path is required" };

            // Only allow paths that start with "file/" — block everything else
            if (!path.StartsWith("file/", StringComparison.OrdinalIgnoreCase))
                return new FileResult { Success = false, Message = "Access denied" };

            //  Look up the file record and verify token.file_id matches 
            var file = await _context.Files
                .AsNoTracking()
                .Where(f => f.Location == path && f.Id == fileToken.file_id)
                .FirstOrDefaultAsync();

            if (file == null)
                return new FileResult { Success = false, Message = "File not found" };

            //Challenge-file access control
            if (string.Equals(file.Type, "challenge", StringComparison.OrdinalIgnoreCase))
            {
                if (file.ChallengeId == null)
                    return new FileResult { Success = false, Message = "Access denied" };

                var challenge = await _context.Challenges
                    .AsNoTracking()
                    .Where(c => c.Id == file.ChallengeId)
                    .FirstOrDefaultAsync();

                if (challenge == null)
                    return new FileResult { Success = false, Message = "Access denied" };

                // Check if challenge is visible in current contest
                var contestChallenge = await _context.ContestsChallenges
                    .FirstOrDefaultAsync(cc => cc.BankId == file.ChallengeId && cc.ContestId == _contestContext.ContestId);

                // Block files of hidden challenges
                if (contestChallenge == null || string.Equals(contestChallenge.State, "hidden", StringComparison.OrdinalIgnoreCase))
                    return new FileResult { Success = false, Message = "Access denied" };

                // Check prerequisite challenges have been solved by the team
                if (!string.IsNullOrWhiteSpace(challenge.Requirements))
                {
                    if (fileToken.team_id == null)
                        return new FileResult { Success = false, Message = "Access denied" };

                    List<int>? prerequisites = null;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(challenge.Requirements);
                        if (doc.RootElement.TryGetProperty("prerequisites", out var prereqEl))
                        {
                            prerequisites = prereqEl.EnumerateArray()
                                .Select(e => e.GetInt32())
                                .ToList();
                        }
                    }
                    catch
                    {
                        // Malformed requirements JSON — deny access to be safe
                        return new FileResult { Success = false, Message = "Access denied" };
                    }

                    if (prerequisites is { Count: > 0 })
                    {
                        // Convert bank challenge IDs to contest challenge IDs
                        var contestChallengeIds = await _context.ContestsChallenges
                            .Where(cc => cc.ContestId == _contestContext.ContestId && cc.BankId.HasValue && prerequisites.Contains(cc.BankId.Value))
                            .Select(cc => cc.Id)
                            .ToListAsync();

                        var solvedIds = await _context.Solves
                            .AsNoTracking()
                            .Where(s => s.TeamId == fileToken.team_id
                                        && contestChallengeIds.Contains(s.ContestChallengeId)) // ContestChallengeId is int, not int?
                            .Select(s => s.ContestChallengeId) // No .Value needed
                            .Distinct()
                            .ToListAsync();

                        if (!contestChallengeIds.All(ccId => solvedIds.Contains(ccId)))
                            return new FileResult { Success = false, Message = "Access denied" };
                    }
                }
            }
            // type == "standard" → no extra access control, fall through

            //  Resolve physical path
            var fullPath = Path.GetFullPath(Path.Combine(_nfsMountPath, path));

            if (!fullPath.StartsWith(_nfsMountPath, StringComparison.OrdinalIgnoreCase))
                return new FileResult { Success = false, Message = "Invalid file path" };

            if (!System.IO.File.Exists(fullPath))
            {
                await Console.Out.WriteLineAsync($"File does not exist at: {fullPath}");
                return new FileResult { Success = false, Message = "File not found" };
            }

            var fileInfo = new FileInfo(fullPath);
            var fileName = fileInfo.Name;
            var contentType = GetContentType(fileName);
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

            return new FileResult
            {
                Success = true,
                FileStream = fileStream,
                FileName = fileName,
                ContentType = contentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, user_id, data: new { path, token });
            return new FileResult { Success = false, Message = $"Error retrieving file: {ex.Message}" };
        }
    }

}


