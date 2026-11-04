@echo off
rem Builds and starts web, api and postgres. Double-click or run from any folder.
setlocal
cd /d "%~dp0.."

rem 1. Docker Desktop must be running.
docker info >nul 2>&1
if errorlevel 1 (
  echo Start Docker Desktop and wait until it is running, then run this again.
  pause
  exit /b 1
)

rem 2. Create missing secrets without printing them; preserve existing settings and DB data.
powershell -NoProfile -ExecutionPolicy Bypass -File docker\initialize-env.ps1
if errorlevel 1 (
  echo Could not initialize docker\.env. Check the error above.
  pause
  exit /b 1
)

rem 3. Build and start (data in the postgres volume is kept).
docker compose -f docker/docker-compose.yml up -d --build
if errorlevel 1 (
  echo Start failed. Check: docker compose -f docker/docker-compose.yml logs
  pause
  exit /b 1
)

set "WEB_PORT=8080"
for /f "usebackq tokens=1,* delims==" %%a in ("docker\.env") do if "%%a"=="WEB_PORT" set "WEB_PORT=%%b"
echo.
echo App:    http://localhost:%WEB_PORT%
echo Health: http://localhost:%WEB_PORT%/api/health
pause
