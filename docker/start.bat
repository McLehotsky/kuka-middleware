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

rem 2. First run: create docker\.env from the example with a random hex DB password (never printed).
if exist docker\.env (
  echo docker\.env exists - leaving it unchanged.
) else (
  powershell -NoProfile -Command "$b = New-Object byte[] 24; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); $p = -join ($b | ForEach-Object { $_.ToString('x2') }); $c = [IO.File]::ReadAllText('docker\.env.example'); if ($c -notmatch '(?m)^POSTGRES_PASSWORD=change-me\r?$') { exit 1 }; [IO.File]::WriteAllText('docker\.env', ($c -replace '(?m)^POSTGRES_PASSWORD=change-me(?=\r?$)', ('POSTGRES_PASSWORD=' + $p)))"
  if errorlevel 1 (
    echo Could not create docker\.env from docker\.env.example.
    pause
    exit /b 1
  )
  echo Created docker\.env with a random database password.
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
