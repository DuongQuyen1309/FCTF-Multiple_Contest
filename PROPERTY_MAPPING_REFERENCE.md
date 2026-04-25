# Property Mapping Reference

## Quick Reference: Challenge vs ContestsChallenge

### Identical Properties (Both Tables)

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `Id` | int | Primary key | Auto-increment |
| `Name` | string? | Challenge name | null |
| `Description` | string? | Challenge description | null |
| `Category` | string? | Challenge category (Web, Crypto, etc.) | null |
| `Type` | string? | Challenge type | null |
| `Difficulty` | int? | Difficulty level (1-5) | null |
| `Requirements` | string? | Prerequisites | null |
| `ImageLink` | string? | Docker image URL | null |
| `DeployFile` | string? | Deployment configuration file | null |
| `CpuLimit` | int? | CPU limit (millicores) | null |
| `CpuRequest` | int? | CPU request (millicores) | null |
| `MemoryLimit` | int? | Memory limit (MB) | null |
| `MemoryRequest` | int? | Memory request (MB) | null |
| `UseGvisor` | bool? | Use gVisor sandbox | null |
| `MaxDeployCount` | int? | Max simultaneous deployments | 0 |
| `IsPublic` | bool | Public visibility | false |
| `ImportCount` | int | Times imported/pulled | 0 |
| `CreatedAt` | DateTime? | Creation timestamp | null |
| `UpdateAt` | DateTime? | Last update timestamp | null |
| `MaxAttempt` | int? | Maximum submission attempts | 0 |
| `Value` | int? | Point value | null |
| `State` | string | Visibility state (visible/hidden) | "visible" |
| `TimeLimit` | int? | Time limit (seconds) | null |
| `StartTime` | DateTime? | Challenge start time | null |
| `TimeFinished` | DateTime? | Challenge finish time | null |
| `Cooldown` | int? | Cooldown between attempts (seconds) | 0 |
| `RequireDeploy` | bool | Requires deployment | false |
| `DeployStatus` | string? | Deployment status | null/"CREATED" |
| `ConnectionProtocol` | string? | Connection protocol (http/tcp) | "http" |
| `ConnectionInfo` | string? | Connection information | null |

### Challenge-Specific Properties

| Property | Type | Description | FK Reference |
|----------|------|-------------|--------------|
| `AuthorId` | int? | User who uploaded to bank | users.id |

### ContestsChallenge-Specific Properties

| Property | Type | Description | FK Reference |
|----------|------|-------------|--------------|
| `ContestId` | int | Contest this challenge belongs to | contests.id |
| `BankId` | int? | Bank challenge template | challenges.id |
| `UserId` | int? | User who pulled to contest | users.id |
| `NextId` | int? | Next challenge in chain | contests_challenges.id |

## Property Usage Guide

### When Creating Bank Challenge (challenges table)

```csharp
var bankChallenge = new Challenge
{
    // Required
    Name = "SQL Injection 101",
    
    // Recommended
    Description = "Learn basic SQL injection",
    Category = "Web",
    Type = "standard",
    Difficulty = 2,
    AuthorId = teacherId,
    
    // Deploy configuration
    ImageLink = "registry.example.com/sql-challenge:v1",
    DeployFile = "k8s-deployment.yaml",
    CpuLimit = 500,
    CpuRequest = 100,
    MemoryLimit = 512,
    MemoryRequest = 128,
    UseGvisor = true,
    MaxDeployCount = 1,
    
    // Default challenge configuration
    Value = 100,
    MaxAttempt = 10,
    State = "visible",
    Cooldown = 30,
    RequireDeploy = true,
    ConnectionProtocol = "http",
    
    // Metadata
    IsPublic = true,
    CreatedAt = DateTime.UtcNow,
    UpdateAt = DateTime.UtcNow
};
```

### When Pulling Challenge to Contest (contests_challenges table)

#### Option 1: Use Bank Defaults (No Configuration)
```csharp
var contestChallenge = new ContestsChallenge
{
    ContestId = contestId,
    BankId = bankChallenge.Id,
    UserId = teacherId,
    
    // Copy ALL properties from bank
    Name = bankChallenge.Name,
    Description = bankChallenge.Description,
    Category = bankChallenge.Category,
    Type = bankChallenge.Type,
    Difficulty = bankChallenge.Difficulty,
    Requirements = bankChallenge.Requirements,
    ImageLink = bankChallenge.ImageLink,
    DeployFile = bankChallenge.DeployFile,
    CpuLimit = bankChallenge.CpuLimit,
    CpuRequest = bankChallenge.CpuRequest,
    MemoryLimit = bankChallenge.MemoryLimit,
    MemoryRequest = bankChallenge.MemoryRequest,
    UseGvisor = bankChallenge.UseGvisor,
    MaxDeployCount = bankChallenge.MaxDeployCount,
    IsPublic = bankChallenge.IsPublic,
    MaxAttempt = bankChallenge.MaxAttempt,
    Value = bankChallenge.Value,
    State = bankChallenge.State,
    TimeLimit = bankChallenge.TimeLimit,
    StartTime = bankChallenge.StartTime,
    TimeFinished = bankChallenge.TimeFinished,
    Cooldown = bankChallenge.Cooldown,
    RequireDeploy = bankChallenge.RequireDeploy,
    ConnectionProtocol = bankChallenge.ConnectionProtocol,
    ConnectionInfo = bankChallenge.ConnectionInfo,
    
    // Contest-specific
    DeployStatus = "CREATED",
    CreatedAt = DateTime.UtcNow,
    UpdateAt = DateTime.UtcNow
};
```

#### Option 2: Override Configuration
```csharp
var contestChallenge = new ContestsChallenge
{
    ContestId = contestId,
    BankId = bankChallenge.Id,
    UserId = teacherId,
    
    // Override specific properties
    Name = "SQL Injection 101 - Contest Edition",
    Value = 150, // More points for this contest
    MaxAttempt = 5, // Fewer attempts
    Cooldown = 60, // Longer cooldown
    State = "hidden", // Start hidden
    
    // Copy rest from bank
    Description = bankChallenge.Description,
    Category = bankChallenge.Category,
    // ... (all other properties)
};
```

## API Request Examples

### Pull Challenge with Default Configuration
```json
POST /api/Contest/{contestId}/pull-challenges
{
  "challenges": [
    {
      "bankChallengeId": 123
      // No other properties = use bank defaults
    }
  ]
}
```

### Pull Challenge with Custom Configuration
```json
POST /api/Contest/{contestId}/pull-challenges
{
  "challenges": [
    {
      "bankChallengeId": 123,
      "name": "Custom Challenge Name",
      "value": 200,
      "maxAttempt": 5,
      "cooldown": 60,
      "state": "hidden",
      "requireDeploy": true,
      "connectionProtocol": "tcp"
    }
  ]
}
```

### Pull Challenge with Full Configuration
```json
POST /api/Contest/{contestId}/pull-challenges
{
  "challenges": [
    {
      "bankChallengeId": 123,
      "name": "Advanced SQL Challenge",
      "description": "Contest-specific description",
      "category": "Web",
      "type": "advanced",
      "difficulty": 4,
      "requirements": "Complete SQL 101 first",
      "imageLink": "registry.example.com/sql-advanced:v2",
      "deployFile": "custom-deployment.yaml",
      "cpuLimit": 1000,
      "cpuRequest": 200,
      "memoryLimit": 1024,
      "memoryRequest": 256,
      "useGvisor": true,
      "maxDeployCount": 2,
      "isPublic": false,
      "maxAttempt": 3,
      "value": 300,
      "state": "visible",
      "timeLimit": 3600,
      "startTime": "2024-03-01T00:00:00Z",
      "timeFinished": "2024-03-31T23:59:59Z",
      "cooldown": 120,
      "requireDeploy": true,
      "connectionProtocol": "http",
      "connectionInfo": "https://challenge.example.com"
    }
  ]
}
```

## Common Scenarios

### Scenario 1: Easy Challenge for Beginners
```json
{
  "bankChallengeId": 1,
  "value": 50,
  "maxAttempt": 0,  // Unlimited attempts
  "cooldown": 0,    // No cooldown
  "difficulty": 1,
  "state": "visible"
}
```

### Scenario 2: Hard Challenge with Limited Attempts
```json
{
  "bankChallengeId": 2,
  "value": 500,
  "maxAttempt": 3,
  "cooldown": 300,  // 5 minutes
  "difficulty": 5,
  "state": "hidden"  // Unlock later
}
```

### Scenario 3: Timed Challenge
```json
{
  "bankChallengeId": 3,
  "value": 200,
  "timeLimit": 1800,  // 30 minutes
  "startTime": "2024-03-15T10:00:00Z",
  "timeFinished": "2024-03-15T12:00:00Z",
  "state": "visible"
}
```

### Scenario 4: Deployment Required Challenge
```json
{
  "bankChallengeId": 4,
  "value": 300,
  "requireDeploy": true,
  "maxDeployCount": 1,
  "connectionProtocol": "http",
  "cpuLimit": 500,
  "memoryLimit": 512,
  "useGvisor": true
}
```

## Property Validation Rules

### Required Properties
- `ContestId` (for ContestsChallenge)
- `BankId` (for ContestsChallenge)
- `Name` (recommended but nullable)

### Recommended Defaults
- `Value`: 100 points
- `MaxAttempt`: 0 (unlimited)
- `Cooldown`: 0 (no cooldown)
- `State`: "visible"
- `RequireDeploy`: false
- `ConnectionProtocol`: "http"
- `IsPublic`: false
- `MaxDeployCount`: 0

### Valid Values
- `State`: "visible" | "hidden"
- `ConnectionProtocol`: "http" | "tcp" | "udp"
- `DeployStatus`: "CREATED" | "PENDING" | "RUNNING" | "STOPPED" | "FAILED"
- `Difficulty`: 1-5 (1=Easy, 5=Hard)

## Database Relationships

```
User (author_id)
  └── Challenge (bank)
        ├── Flags
        ├── Hints
        ├── Files
        ├── Tags
        └── ContestsChallenge (bank_id)
              ├── Contest (contest_id)
              ├── User (user_id - who pulled)
              ├── Submissions
              ├── Solves
              ├── DeployHistories
              └── ChallengeStartTrackings
```

## Migration Checklist

### Adding New Property to Both Tables
1. Add property to `Challenge.cs` model
2. Add same property to `ContestsChallenge.cs` model
3. Add property to `PullChallengeItemDTO` (optional override)
4. Add property to `BankChallengeDTO` (for API response)
5. Add property to `ContestChallengeDTO` (for API response)
6. Update `PullChallengesToContest` method to copy property
7. Update `GetBankChallenges` method to return property
8. Update `GetContestChallenges` method to return property
9. Run database migration
10. Test thoroughly

---

**Last Updated:** 2024
**Version:** 1.0
**Status:** ✅ Reference Guide
