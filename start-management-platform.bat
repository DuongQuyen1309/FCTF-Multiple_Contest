@echo off
REM Script khởi động FCTF-ManagementPlatform
REM Chạy script này bằng cách double-click

echo ============================================
echo   KHOI DONG MANAGEMENT PLATFORM (ADMIN)
echo ============================================
echo.

REM Di chuyển vào thư mục ManagementPlatform
cd FCTF-ManagementPlatform

echo Dang khoi dong Management Platform...
echo.
echo Management Platform se chay o:
echo   http://localhost:8000
echo   http://localhost:8000/login
echo.
echo Nhan Ctrl+C de dung
echo.

REM Chạy với --disable-gevent để tránh lỗi
python serve.py --disable-gevent

pause
