# =============================================================
# seed_test_data.ps1
# Orchestrate toàn bộ: seed DB → register users → link contests → test
#
# Prerequisite:
#   - Docker đang chạy: docker compose -f docker-compose.dev.yml up -d
#   - ContestantBE đang chạy: dotnet run (http://localhost:5069)
#
# Chạy từ thư mục gốc repo:
#   .\Test\ContestAccess\seed_test_data.ps1
# =============================================================

$ErrorActionPreference = "Stop"

$API        = "http://localhost:5069/api"
$DB_PASS    = "root_password"
$DB_NAME    = "ctfd"
$CONTAINER  = "fctf-mariadb"
$SCRIPT_DIR = $PSScriptRoot
$PASSWORD   = "Test@1234"

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-OK($msg)   { Write-Host "  [OK]   $msg" -ForegroundColor Green }
function Write-FAIL($msg) { Write-Host "  [FAIL] $msg" -ForegroundColor Red }
function Write-SKIP($msg) { Write-Host "  [SKIP] $msg" -ForegroundColor Yellow }

# ------------------------------------------------------------------
# Helper: chạy SQL string trong container MariaDB
# ------------------------------------------------------------------
function Invoke-SQL($sql) {
    $output = docker exec -i $CONTAINER mariadb -uroot -p"$DB_PASS" $DB_NAME `
        --default-character-set=utf8mb4 `
        -e $sql 2>&1
    # Luôn trả về string, không phải array/char
    return ($output -join "`n")
}

function Invoke-SQLFile($path) {
    $sql = Get-Content $path -Raw
    docker exec -i $CONTAINER mariadb -uroot -p"$DB_PASS" $DB_NAME `
        --default-character-set=utf8mb4 `
        -e $sql 2>&1 | Out-Null
}

# ------------------------------------------------------------------
# STEP 1: Seed semester + contests + bank challenges + flags
# ------------------------------------------------------------------
Write-Step "STEP 1: Seed DB (semester, contests, challenges, flags)"
Invoke-SQLFile "$SCRIPT_DIR\seed.sql"

# Hiển thị kết quả seed (chỉ lấy data sạch, không bị ảnh hưởng bởi lỗi join)
$seedCheck = Invoke-SQL @"
SELECT DISTINCT c.slug AS contest, cc.id AS cc_id, b.name AS challenge, b.category, b.points, cc.state,
       (SELECT f.content FROM flags f WHERE f.challenge_id = b.id LIMIT 1) AS flag
FROM contests c
JOIN contests_challenges cc ON cc.contest_id = c.id
JOIN challenges b ON b.id = cc.bank_id
WHERE c.slug IN ('web-security-2024','crypto-2024')
ORDER BY c.slug, cc.id;
"@
Write-Host $seedCheck
Write-OK "seed.sql done"

# ------------------------------------------------------------------
# STEP 2: Register 3 test users qua API
# ------------------------------------------------------------------
Write-Step "STEP 2: Register test users qua API"

$testUsers = @(
    @{ username = "alice";   email = "alice@test.com";   note = "sẽ vào CẢ 2 contest" },
    @{ username = "bob";     email = "bob@test.com";     note = "chỉ vào web-security" },
    @{ username = "charlie"; email = "charlie@test.com"; note = "chỉ vào crypto" }
)

foreach ($u in $testUsers) {
    $body = @{
        username        = $u.username
        email           = $u.email
        password        = $PASSWORD
        confirmPassword = $PASSWORD
        captchaToken    = "bypass"
        userFields      = @()
    } | ConvertTo-Json

    try {
        Invoke-RestMethod -Uri "$API/auth/register-contestant" `
                          -Method POST -Body $body `
                          -ContentType "application/json" | Out-Null
        Write-OK "Registered $($u.email) ($($u.note))"
    }
    catch {
        # Lấy response body để debug
        $rawMsg = ""
        try { $rawMsg = $_.ErrorDetails.Message } catch {}
        $parsedMsg = ""
        try { $parsedMsg = ($rawMsg | ConvertFrom-Json).message } catch {}
        if (-not $parsedMsg) { $parsedMsg = $rawMsg }

        Write-Host "    [debug] raw error: '$parsedMsg'" -ForegroundColor DarkGray

        if ($parsedMsg -match "already|exist|duplicate|tồn tại|taken|registered") {
            Write-SKIP "$($u.email) đã tồn tại, bỏ qua"
        } else {
            # Không dừng script, chỉ warn — user có thể đã tồn tại trong DB từ lần trước
            Write-SKIP "$($u.email): $($_.Exception.Message) (tiếp tục)"
        }
    }
}

# ------------------------------------------------------------------
# STEP 3: Link users vào contests (contest_participants)
# ------------------------------------------------------------------
Write-Step "STEP 3: Tạo contest_participants"
Invoke-SQLFile "$SCRIPT_DIR\seed_participants.sql"

$partCheck = Invoke-SQL @"
SELECT u.username, u.email, c.slug AS contest, cp.role
FROM contest_participants cp
JOIN users u ON u.id = cp.user_id
JOIN contests c ON c.id = cp.contest_id
WHERE c.slug IN ('web-security-2024','crypto-2024');
"@
Write-Host $partCheck
Write-OK "seed_participants.sql done"

# ------------------------------------------------------------------
# STEP 4: Lấy contest IDs từ DB
# ------------------------------------------------------------------
Write-Step "STEP 4: Lấy contest IDs"

# FIX: dùng -join trước để đảm bảo là string, không phải char array
$webRaw    = (docker exec -i $CONTAINER mariadb -uroot -p"$DB_PASS" $DB_NAME --default-character-set=utf8mb4 --skip-column-names -s -e "SELECT id FROM contests WHERE slug='web-security-2024' LIMIT 1;" 2>&1) -join "`n"
$cryptoRaw = (docker exec -i $CONTAINER mariadb -uroot -p"$DB_PASS" $DB_NAME --default-character-set=utf8mb4 --skip-column-names -s -e "SELECT id FROM contests WHERE slug='crypto-2024' LIMIT 1;" 2>&1) -join "`n"

$webId    = ($webRaw    -split "`n" | Where-Object { $_.Trim() -match '^\d+$' } | Select-Object -First 1).Trim()
$cryptoId = ($cryptoRaw -split "`n" | Where-Object { $_.Trim() -match '^\d+$' } | Select-Object -First 1).Trim()

if (-not $webId)    { Write-FAIL "Không tìm thấy contest web-security-2024 trong DB!"; exit 1 }
if (-not $cryptoId) { Write-FAIL "Không tìm thấy contest crypto-2024 trong DB!"; exit 1 }

Write-OK "web-security ID = $webId"
Write-OK "crypto        ID = $cryptoId"

# ------------------------------------------------------------------
# Helpers: login + select-contest + get challenges
# ------------------------------------------------------------------
function Login($username) {
    $body = @{ username = $username; password = $PASSWORD; captchaToken = "bypass" } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$API/auth/login-contestant" `
                              -Method POST -Body $body -ContentType "application/json"
    return $resp.generatedToken
}

function Select-Contest($token, $contestId) {
    $body = @{ contestId = [int]$contestId } | ConvertTo-Json
    try {
        $resp = Invoke-RestMethod -Uri "$API/auth/select-contest" `
                                  -Method POST -Body $body -ContentType "application/json" `
                                  -Headers @{ Authorization = "Bearer $token" }
        return @{ ok = $true; token = $resp.data.token }
    } catch {
        $errMsg = ""
        try { $errMsg = $_.ErrorDetails.Message } catch {}
        return @{ ok = $false; error = $errMsg }
    }
}

function Get-Challenges($contestToken) {
    try {
        $resp = Invoke-RestMethod -Uri "$API/challenge/by-topic" `
                                  -Method GET -ContentType "application/json" `
                                  -Headers @{ Authorization = "Bearer $contestToken" }
        return @{ ok = $true; data = $resp.data }
    } catch {
        $errMsg = ""
        try { $errMsg = $_.ErrorDetails.Message } catch {}
        return @{ ok = $false; error = $errMsg }
    }
}

function Submit-Flag($contestToken, $ccId, $flag) {
    $body = @{ ChallengeId = $ccId; Submission = $flag } | ConvertTo-Json
    try {
        $resp = Invoke-RestMethod -Uri "$API/challenge/attempt" `
                                  -Method POST -Body $body -ContentType "application/json" `
                                  -Headers @{ Authorization = "Bearer $contestToken" }
        return @{ ok = $true; status = $resp.data.status; message = $resp.data.message }
    } catch {
        $errMsg = ""
        try { $errMsg = $_.ErrorDetails.Message } catch {}
        return @{ ok = $false; error = $errMsg }
    }
}

# ------------------------------------------------------------------
# STEP 5: Test toàn bộ kịch bản
# ------------------------------------------------------------------
Write-Step "STEP 5: Test kịch bản truy cập contest + challenges"

# Lấy cc_ids của từng contest từ DB (FIX: -join trước khi -split)
function Get-CCIds($contestSlug) {
    $raw = (docker exec -i $CONTAINER mariadb -uroot -p"$DB_PASS" $DB_NAME `
        --default-character-set=utf8mb4 --skip-column-names -s `
        -e "SELECT DISTINCT cc.id, cc.name FROM contests_challenges cc JOIN contests c ON c.id = cc.contest_id WHERE c.slug = '$contestSlug' ORDER BY cc.id;" 2>&1) -join "`n"
    $result = @()
    foreach ($row in ($raw -split "`n")) {
        $row = $row.Trim()
        if ($row -match '^(\d+)\s+(.+)$') {
            $result += @{ id = [int]$Matches[1]; name = $Matches[2].Trim() }
        }
    }
    return $result
}

# Lấy flag từ DB cho một cc_id
function Get-FlagForCC($ccId) {
    $raw = (docker exec -i $CONTAINER mariadb -uroot -p"$DB_PASS" $DB_NAME `
        --default-character-set=utf8mb4 --skip-column-names -s `
        -e "SELECT f.content FROM flags f JOIN challenges b ON b.id = f.challenge_id JOIN contests_challenges cc ON cc.bank_id = b.id WHERE cc.id = $ccId LIMIT 1;" 2>&1) -join "`n"
    $flag = ($raw -split "`n" | Where-Object { $_.Trim() -ne '' -and $_.Trim() -notmatch '^content$' } | Select-Object -First 1).Trim()
    return $flag
}

$webCCs    = Get-CCIds "web-security-2024"
$cryptoCCs = Get-CCIds "crypto-2024"

Write-Host "`n  Challenges trong web-security:" -ForegroundColor Gray
$webCCs    | ForEach-Object { Write-Host "    cc_id=$($_.id)  $($_.name)" -ForegroundColor Gray }
Write-Host "  Challenges trong crypto:" -ForegroundColor Gray
$cryptoCCs | ForEach-Object { Write-Host "    cc_id=$($_.id)  $($_.name)" -ForegroundColor Gray }

# ---- alice: ĐƯỢC vào cả 2, thấy challenges cả 2, nộp flag đúng ----
Write-Host "`n[alice]" -ForegroundColor White
$aliceToken = Login "alice"

$r = Select-Contest $aliceToken $webId
if ($r.ok) { Write-OK "select web-security [ALLOWED - expected]" }
else       { Write-FAIL "select web-security: $($r.error)" }
$aliceWebToken = $r.token

$r = Get-Challenges $aliceWebToken
if ($r.ok) {
    $cats = ($r.data | ForEach-Object { $_.topic }) -join ", "
    Write-OK "get challenges web → categories: $cats"
} else { Write-FAIL "get challenges web: $($r.error)" }

# Nộp flag đúng cho challenge đầu tiên của web
if ($webCCs.Count -gt 0) {
    $cc   = $webCCs[0]
    $flag = Get-FlagForCC $cc.id
    if ($flag) {
        $r = Submit-Flag $aliceWebToken $cc.id $flag
        if ($r.ok -and $r.status -eq "correct") { Write-OK "submit flag '$flag' → correct [expected]" }
        else { Write-FAIL "submit flag '$flag' → $($r.status) $($r.error)" }

        $wrong = Submit-Flag $aliceWebToken $cc.id "FCTF{wrong_flag}"
        if ($wrong.ok -and $wrong.status -eq "incorrect")      { Write-OK "submit wrong flag → incorrect [expected]" }
        elseif ($wrong.ok -and $wrong.status -eq "already_solved") { Write-OK "submit wrong flag → already_solved [expected]" }
        else { Write-FAIL "submit wrong flag: $($wrong.status) $($wrong.error)" }
    } else {
        Write-SKIP "Không lấy được flag cho cc_id=$($cc.id)"
    }
}

$r = Select-Contest $aliceToken $cryptoId
if ($r.ok) { Write-OK "select crypto [ALLOWED - expected]" }
else       { Write-FAIL "select crypto: $($r.error)" }
$aliceCryptoToken = $r.token

$r = Get-Challenges $aliceCryptoToken
if ($r.ok) {
    $cats = ($r.data | ForEach-Object { $_.topic }) -join ", "
    Write-OK "get challenges crypto → categories: $cats"
} else { Write-FAIL "get challenges crypto: $($r.error)" }

# ---- bob: CHỈ web, KHÔNG có crypto ----
Write-Host "`n[bob]" -ForegroundColor White
$bobToken = Login "bob"

$r = Select-Contest $bobToken $webId
if ($r.ok) { Write-OK "select web-security [ALLOWED - expected]" }
else       { Write-FAIL "select web-security: $($r.error)" }
$bobWebToken = $r.token

$r = Get-Challenges $bobWebToken
if ($r.ok) {
    $cats = ($r.data | ForEach-Object { $_.topic }) -join ", "
    Write-OK "get challenges web → categories: $cats"
} else { Write-FAIL "get challenges web: $($r.error)" }

$r = Select-Contest $bobToken $cryptoId
if (-not $r.ok) { Write-OK "select crypto [DENIED - expected]" }
else            { Write-FAIL "select crypto (should be denied!): got token" }

# ---- charlie: CHỈ crypto, KHÔNG có web ----
Write-Host "`n[charlie]" -ForegroundColor White
$charlieToken = Login "charlie"

$r = Select-Contest $charlieToken $webId
if (-not $r.ok) { Write-OK "select web-security [DENIED - expected]" }
else            { Write-FAIL "select web-security (should be denied!): got token" }

$r = Select-Contest $charlieToken $cryptoId
if ($r.ok) { Write-OK "select crypto [ALLOWED - expected]" }
else       { Write-FAIL "select crypto: $($r.error)" }
$charlieCryptoToken = $r.token

$r = Get-Challenges $charlieCryptoToken
if ($r.ok) {
    $cats = ($r.data | ForEach-Object { $_.topic }) -join ", "
    Write-OK "get challenges crypto → categories: $cats"
} else { Write-FAIL "get challenges crypto: $($r.error)" }

# Nộp flag đúng cho challenge đầu tiên của crypto
if ($cryptoCCs.Count -gt 0) {
    $cc   = $cryptoCCs[0]
    $flag = Get-FlagForCC $cc.id
    if ($flag) {
        $r = Submit-Flag $charlieCryptoToken $cc.id $flag
        if ($r.ok -and $r.status -eq "correct") { Write-OK "submit flag '$flag' → correct [expected]" }
        else { Write-FAIL "submit flag '$flag' → $($r.status) $($r.error)" }
    } else {
        Write-SKIP "Không lấy được flag cho cc_id=$($cc.id)"
    }
}

# ---- Cross-contest check ----
Write-Host "`n[cross-contest check]" -ForegroundColor White
if ($webCCs.Count -gt 0 -and $null -ne $aliceWebToken) {
    $r = Get-Challenges $aliceWebToken
    if ($r.ok) {
        $isWebCat = ($r.data | Where-Object { $_.topic -eq "Web" }).Count -gt 0
        if ($isWebCat) { Write-OK "alice web-token chỉ thấy Web challenges [expected]" }
        else           { Write-FAIL "alice web-token thấy categories không mong đợi" }
    } else {
        Write-FAIL "cross-contest check failed: $($r.error)"
    }
}

Write-Step "DONE"
Write-Host @"

  Test accounts  : alice / bob / charlie
  Password       : $PASSWORD
  Contest IDs    : web-security=$webId  |  crypto=$cryptoId
"@