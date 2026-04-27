# Script khởi động Backend
# Chạy script này trong PowerShell: .\start-backend.ps1

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  🚀 KHỞI ĐỘNG BACKEND" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra .NET SDK
Write-Host "📦 Kiểm tra .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "❌ .NET SDK chưa được cài đặt!" -ForegroundColor Red
    Write-Host "   Tải tại: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Di chuyển vào thư mục backend
$backendPath = "ControlCenterAndChallengeHostingServer/ContestantBE"
Write-Host "📁 Di chuyển vào thư mục: $backendPath" -ForegroundColor Yellow

if (Test-Path $backendPath) {
    Set-Location $backendPath
    Write-Host "✅ Đã vào thư mục backend" -ForegroundColor Green
} else {
    Write-Host "❌ Không tìm thấy thư mục backend!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Restore dependencies
Write-Host "📦 Restore dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Dependencies restored" -ForegroundColor Green
} else {
    Write-Host "❌ Lỗi khi restore dependencies!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  🎯 BACKEND SẼ CHẠY Ở:" -ForegroundColor Cyan
Write-Host "  http://localhost:5069" -ForegroundColor Green
Write-Host "  Swagger: http://localhost:5069/swagger" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "⚠️  Nhấn Ctrl+C để dừng backend" -ForegroundColor Yellow
Write-Host ""

# Chạy backend
Write-Host "🚀 Đang khởi động backend..." -ForegroundColor Yellow
Write-Host ""
dotnet run --launch-profile http
