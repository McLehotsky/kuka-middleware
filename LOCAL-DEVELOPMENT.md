# Local development

How to run the KUKA FM Middleware on your own PC: the API on `dotnet run` and the web UI on the Vite dev server, against a local PostgreSQL. Docker is not needed for this. The Docker setup is at the end.

All commands are PowerShell, run from the **repository root** unless the step says otherwise.

| What | URL |
|---|---|
| Web UI (Vite dev server) | http://localhost:5173 |
| API (`dotnet run`) | http://localhost:5063 |
| API health check | http://localhost:5063/api/health |

The Vite dev server forwards every `/api/...` request to the API on port 5063, so the browser only needs http://localhost:5173.

---

## 1. Prerequisites (once per PC)

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.0.402 (pinned in `backend/global.json`) | `winget install --id Microsoft.DotNet.SDK.10 -e` |
| Node.js | 24 LTS | `winget install --id OpenJS.NodeJS.LTS -e` |
| PostgreSQL | 18 | `winget install --id PostgreSQL.PostgreSQL.18 -e` (you choose the `postgres` superuser password during install) |

Check them in a new terminal:

```powershell
dotnet --list-sdks
node -v
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' --version
```

The PostgreSQL installer runs it as a Windows service, `postgresql-x64-18`, on port 5432. If the service is stopped, start it again:

```powershell
Start-Service postgresql-x64-18
```

---

## 2. Database and connection string (once per PC)

### 2.1 Create the app role and database

Pick your own password for the `kuka` role (no `;` in it). Run psql as the `postgres` superuser; it asks for the superuser password:

```powershell
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -h localhost -U postgres -c "CREATE ROLE kuka LOGIN PASSWORD '<your-password>';"
```

```powershell
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -h localhost -U postgres -c "CREATE DATABASE kuka_middleware OWNER kuka;"
```

If the role and database already exist and you only need a new password, run:

```powershell
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -h localhost -U postgres -c "ALTER ROLE kuka PASSWORD '<your-password>';"
```

### 2.2 Store the connection string in user-secrets

The connection string never goes into the repo. In Development the API reads it from .NET user-secrets, which are stored in your Windows profile and not in the repo:

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=kuka_middleware;Username=kuka;Password=<your-password>" --project backend/src/KukaMiddleware.Api
```

To check that it is set, list the keys. This prints the secret value too, so don't share the output:

```powershell
dotnet user-secrets list --project backend/src/KukaMiddleware.Api
```

The tables don't need to be created by hand. The API applies all EF Core migrations on startup, including the seeded `web-ui` client.

---

## 3. Install dependencies (after cloning, and after `package.json` or tool changes)

```powershell
dotnet tool restore --tool-manifest backend/.config/dotnet-tools.json
```

```powershell
npm --prefix frontend ci
```

---

## 4. Run the app

You need **two terminals**, one for the API and one for the web UI.

**Terminal 1: API** (http://localhost:5063)

```powershell
dotnet run --project backend/src/KukaMiddleware.Api
```

Wait for `Now listening on: http://localhost:5063`. On the first start the log shows the migrations being applied.

**Terminal 2: web UI** (http://localhost:5173)

```powershell
npm --prefix frontend run dev
```

Then open http://localhost:5173. Stop either server with `Ctrl+C` in its terminal.

Quick check that the API is up:

```powershell
Invoke-RestMethod http://localhost:5063/api/health
```

---

## 5. Tests and checks

These run the same checks CI runs.

**Backend** (run in `backend/`)

```powershell
dotnet build
```

```powershell
dotnet test
```

```powershell
dotnet ef migrations has-pending-model-changes --project src/KukaMiddleware.Api
```

**Frontend** (run in `frontend/`)

```powershell
npm run typecheck
```

```powershell
npm run lint
```

```powershell
npm run test
```

```powershell
npm run build
```

---

## 6. Useful extras

**Create a mission from the command line** (the same as the New mission form):

```powershell
Invoke-RestMethod -Method Post http://localhost:5063/api/missions -ContentType 'application/json' -Body '{"missionType":"RACK_MOVE","priority":2,"steps":[{"nodeCode":"A-01-PICK","action":"Load"},{"nodeCode":"B-07-DROP","action":"Unload"}]}'
```

**List missions:**

```powershell
Invoke-RestMethod 'http://localhost:5063/api/missions?state=active'
```

**Start from an empty database.** This drops all local data. Stop the API first, then run both commands and start the API again; the migrations recreate the schema:

```powershell
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -h localhost -U postgres -c "DROP DATABASE kuka_middleware;"
```

```powershell
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -h localhost -U postgres -c "CREATE DATABASE kuka_middleware OWNER kuka;"
```

---

## 7. Troubleshooting

| Problem | Cause and fix |
|---|---|
| `The ConnectionString property has not been initialized` when the API starts | No connection string in user-secrets for **your** Windows profile. Do step 2.2. Secrets set from another environment (for example a sandboxed Claude session) are not visible in your terminal. |
| `password authentication failed for user "kuka"` | The password in user-secrets differs from the role's password. Set it again with `ALTER ROLE` (2.1) and `user-secrets set` (2.2). |
| `Connection refused` on port 5432 | PostgreSQL is not running: `Start-Service postgresql-x64-18`. |
| Web UI shows "Paused · retrying", or Vite logs `http proxy error ... ECONNREFUSED` | The API is not running on 5063. Start terminal 1. |
| `address already in use` on 5063 or 5173 | An old server is still running. Close its terminal, or find the process with `Get-NetTCPConnection -LocalPort 5063` and stop it. |
| `dotnet ef` not found | Run the `dotnet tool restore` command from step 3. |

---

## 8. With Docker (later)

Docker Desktop is not installed on the main dev PC yet; the full stack is verified in CI. Once Docker is available:

1. Copy the template and fill in your own values: `Copy-Item docker/.env.example docker/.env`.
2. Start web, api and postgres. The app runs on http://localhost:8080:

```powershell
docker compose -f docker/docker-compose.yml up -d --build
```

To stop, keeping the data:

```powershell
docker compose -f docker/docker-compose.yml down
```
