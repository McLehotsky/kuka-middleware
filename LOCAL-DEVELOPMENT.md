# Local development

How to run the KUKA FM Middleware on your own PC. The app runs **only in Docker Compose**: `web` (nginx with the built Vue app, proxies `/api`), `api` (ASP.NET Core) and `postgres` (PostgreSQL with a persistent volume). Builds, unit tests, lint and new migrations run on the host with the .NET SDK and Node.

All commands are PowerShell, run from the **repository root** unless the step says otherwise.

| What | URL |
|---|---|
| Web UI | http://localhost:8080 |
| API (through nginx) | http://localhost:8080/api/... |
| API health check | http://localhost:8080/api/health |

Only the web container is published to the host. The API and PostgreSQL are reachable only inside the compose network.

---

## 1. Prerequisites (once per PC)

| Tool | Version | Install | Used for |
|---|---|---|---|
| Docker Desktop | with Compose v2 | `winget install --id Docker.DockerDesktop -e` | running the app |
| .NET SDK | 10.0.402 (pinned in `backend/global.json`) | `winget install --id Microsoft.DotNet.SDK.10 -e` | backend build, tests, migrations |
| Node.js | 24 LTS | `winget install --id OpenJS.NodeJS.LTS -e` | frontend type check, lint, tests, build |

Docker Desktop must be running (the engine status says "running") before any `docker` command. Check the tools in a new terminal:

```powershell
docker version
```

```powershell
dotnet --list-sdks
```

```powershell
node -v
```

---

## 2. Configuration (once per PC)

Copy the template. `docker/.env` is git-ignored and never committed:

```powershell
Copy-Item docker/.env.example docker/.env
```

Open `docker/.env` and set `POSTGRES_PASSWORD` to a strong password of your own (no `;` in it). The other defaults are fine. The database password is only used inside Docker.

Don't paste the output of `docker compose config` anywhere: it shows the resolved password. Use `config --quiet` to only validate the file.

---

## 3. Run the app

Build the images and start all three containers in the background:

```powershell
docker compose -f docker/docker-compose.yml up -d --build
```

Then open http://localhost:8080. On the first start the API applies all EF Core migrations, including the seeded `web-ui` client; there is nothing to create by hand.

**After changing code**, run the same command again. It rebuilds only the images whose files changed and recreates those containers. The database is kept.

Check that everything runs (`postgres` shows `healthy`):

```powershell
docker compose -f docker/docker-compose.yml ps
```

```powershell
Invoke-RestMethod http://localhost:8080/api/health
```

Stop the containers and **keep** the data:

```powershell
docker compose -f docker/docker-compose.yml down
```

---

## 4. Logs and database

API logs (add `-f` to follow them):

```powershell
docker compose -f docker/docker-compose.yml logs api
```

Open psql inside the database container (user and database from `docker/.env`, defaults `kuka` / `kuka_middleware`):

```powershell
docker compose -f docker/docker-compose.yml exec postgres psql -U kuka -d kuka_middleware
```

**Start from an empty database.** This deletes all data in the Docker volume. The next `up` recreates the schema:

```powershell
docker compose -f docker/docker-compose.yml down -v
```

---

## 5. Tests and checks (on the host)

Install the tools and packages after cloning, and after `package.json` or tool changes:

```powershell
dotnet tool restore --tool-manifest backend/.config/dotnet-tools.json
```

```powershell
npm --prefix frontend ci
```

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

**Compose file**

```powershell
docker compose -f docker/docker-compose.yml config --quiet
```

---

## 6. Useful extras

**Create a mission from the command line** (the same as the New mission form):

```powershell
Invoke-RestMethod -Method Post http://localhost:8080/api/missions -ContentType 'application/json' -Body '{"missionType":"RACK_MOVE","priority":2,"steps":[{"nodeCode":"A-01-PICK","action":"Load"},{"nodeCode":"B-07-DROP","action":"Unload"}]}'
```

**List missions:**

```powershell
Invoke-RestMethod 'http://localhost:8080/api/missions?state=active'
```

---

## 7. Troubleshooting

| Problem | Cause and fix |
|---|---|
| `error during connect` / `Docker Desktop is unable to start` | The Docker engine is not running. Start Docker Desktop and wait until it says "running"; a PC restart helps after a fresh install. |
| `set POSTGRES_PASSWORD in docker/.env` | `docker/.env` is missing or incomplete. Do step 2. |
| `port is already allocated` on 8080 | Another program uses port 8080. Set another `WEB_PORT` in `docker/.env` and run `up` again. |
| Web UI shows "Paused · retrying" or errors on every screen | The API container is down or restarting. Check `ps` and `logs api`. |
| `password authentication failed for user "kuka"` in the API log | `POSTGRES_PASSWORD` was changed after the volume was created; PostgreSQL keeps the first password. Put the old one back, or reset the data with `down -v`. |
| `dotnet ef` not found | Run the `dotnet tool restore` command from step 5. |
