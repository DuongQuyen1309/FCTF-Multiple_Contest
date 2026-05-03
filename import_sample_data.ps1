# PowerShell script to import sample contest data
# Run this from the project root directory

Write-Host "=== Importing Sample Contest Data ===" -ForegroundColor Cyan

# Database connection details from .env
$server = "localhost"
$port = "3306"
$database = "ctfd"
$user = "fctf_user"
$password = "fctf_password"

# Check if mysql command is available
$mysqlPath = Get-Command mysql -ErrorAction SilentlyContinue

if (-not $mysqlPath) {
    Write-Host "ERROR: mysql command not found!" -ForegroundColor Red
    Write-Host "Please install MySQL/MariaDB client or add it to PATH" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternative: You can run the SQL script manually:" -ForegroundColor Yellow
    Write-Host "1. Open your MySQL client (HeidiSQL, MySQL Workbench, etc.)" -ForegroundColor White
    Write-Host "2. Connect to: $server`:$port" -ForegroundColor White
    Write-Host "3. Select database: $database" -ForegroundColor White
    Write-Host "4. Run the SQL file: sample_contest_data.sql" -ForegroundColor White
    exit 1
}

Write-Host "Connecting to database: $database@$server`:$port" -ForegroundColor Yellow

# Import the SQL file
$sqlFile = "sample_contest_data.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "ERROR: SQL file not found: $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Importing data from: $sqlFile" -ForegroundColor Yellow

# Run mysql command
$env:MYSQL_PWD = $password
mysql -h $server -P $port -u $user $database < $sqlFile

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "SUCCESS! Sample data imported successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now:" -ForegroundColor Cyan
    Write-Host "1. Login with: student1 / test123" -ForegroundColor White
    Write-Host "2. View contests at: http://localhost:5173/contests" -ForegroundColor White
    Write-Host "3. Select a contest to start testing" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "ERROR: Failed to import data!" -ForegroundColor Red
    Write-Host "Please check the error messages above" -ForegroundColor Yellow
}

# Clear password from environment
$env:MYSQL_PWD = ""
