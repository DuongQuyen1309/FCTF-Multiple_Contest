@echo off
REM Script khởi động Backend cho Windows
REM Chạy script này bằng cách double-click hoặc: start-backend.bat

echo ============================================
echo   KHOI DONG BACKEND
echo ============================================
echo.

REM Kiểm tra .NET SDK
echo Kiem tra .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK chua duoc cai dat!
    echo Tai tai: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [OK] .NET SDK da duoc cai dat
echo.

REM Di chuyển vào thư mục backend
echo Di chuyen vao thu muc backend...
cd ControlCenterAndChallengeHostingServer\ContestantBE
if %errorlevel% neq 0 (
    echo [ERROR] Khong tim thay thu muc backend!
    pause
    exit /b 1
)

echo [OK] Da vao thu muc backend
echo.

REM Restore dependencies
echo Restore dependencies...
dotnet restore
if %errorlevel% neq 0 (
    echo [ERROR] Loi khi restore dependencies!
    pause
    exit /b 1
)

echo [OK] Dependencies restored
echo.

echo ============================================
echo   BACKEND SE CHAY O:
echo   http://localhost:5069
echo   Swagger: http://localhost:5069/swagger
echo ============================================
echo.
echo Nhan Ctrl+C de dung backend
echo.

REM Chạy backend
echo Dang khoi dong backend...
echo.
dotnet run --launch-profile http

pause
