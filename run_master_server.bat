@echo off
title ProThailand LFS 0.6V Master Server CLI (Localhost:29339)
color 0B
echo ===================================================================
echo             PROTHAILAND LFS MASTER SERVER CLI
echo ===================================================================
echo Checking Python installation...
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Python is not installed or not in PATH!
    echo Please install Python 3 from https://www.python.org/
    pause
    exit /b
)

echo Starting Master Server on Localhost (Port 29339)...
python lfs_master_server.py
pause
