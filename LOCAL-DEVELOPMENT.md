# Local development

How to run the KUKA FM Middleware on your own PC. The app runs **only in Docker Compose**: `web` (nginx with the built Vue app, proxies `/api`), `api` (ASP.NET Core) and `postgres` (PostgreSQL with a persistent volume). Builds, unit tests, lint and new migrations run on the host with the .NET SDK and Node.

All commands are PowerShell, run from the **repository root** unless the step says otherwise.

| What | URL |
|---|---|
| Web UI | http://localhost:8080 |
| API (through nginx) | http://localhost:8080/api/... |
| API health check | http://localhost:8080/api/health |

Only the web container is published to the host, bound to `127.0.0.1`. The API and PostgreSQL are reachable only inside the compose network. URLs use the default port; substitute `WEB_PORT` from `docker/.env` if changed (for example, 8081).

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

## Quick start (Windows)

With Docker Desktop running, double-click `docker/start.bat` (or run it from a terminal). It creates `docker/.env` with a random database password and independent API keys if missing, builds the images, starts the stack and prints the app URL. Existing passwords, configured keys, ports and database data are preserved. Run it again after code changes. Steps 2 and 3 below do the same by hand.

---

## 2. Configuration (once per PC)

Initialize local configuration and random secrets. `docker/.env` is git-ignored and never committed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File docker/initialize-env.ps1
```

The initializer also upgrades an existing `.env` with missing API keys without changing the database password. API keys must be distinct 64-character lowercase hex values. The API fails startup if keys are missing, placeholders, invalid or equal. The database password is only used inside Docker.

Don't paste the output of `docker compose config` anywhere: it shows resolved secrets. Use `config --quiet` to only validate the file. Do not paste `docker inspect` environment output or `nginx -T`: runtime configuration contains keys.

---

## 3. Run the app

Build the images and start all three containers in the background:

```powershell
docker compose -f docker/docker-compose.yml up -d --build
```

Then open http://localhost:8080. On startup the API applies EF Core migrations and configures the `web-ui` and `external-system` clients from secret configuration. Only key hashes enter the database; IDs and disabled states are preserved across restarts.

### Local UI and API authentication

The local UI is trusted: nginx adds its `web-ui` key when the request has no `X-API-Key` header. Access to this UI permits mission operations as `web-ui`; there is no individual operator login. The key is supplied at container startup, not built into browser JavaScript or image layers. Nginx rejects foreign Host/Origin and cross-site browser requests.

An external client supplies its own `X-API-Key` header; nginx forwards explicit keys unchanged. Invalid keys return 401. Direct API requests inside the compose network require a key except for `/api/health`. The same health URL through nginx is public too.

Authorized create/cancel attempts, including 400/404/409 failures, are written to `audit_log`. Unknown/missing/inactive keys are rejected and logged to stdout without an audit row. JSON technical logs contain no key/header/body values; ordinary read requests do not create audit rows.

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
| `ApiKeys:WebUi` / `ApiKeys:ExternalSystem` startup error | Run `docker/start.bat` to initialize missing keys. If keys were edited manually, each must be a distinct 64-character lowercase hex value. |
| 401 with an explicit API key | The key is unknown or the client is inactive. Check configuration and `api_client.is_active`; never paste the key into logs or chat. |
| 403 from nginx | Use the local UI hostname (`localhost` or `127.0.0.1`) and matching Origin; cross-site browser requests are rejected. |
| `port is already allocated` on 8080 | Another program uses port 8080. Set another `WEB_PORT` in `docker/.env` and run `up` again. |
| Web UI shows "Paused · retrying" or errors on every screen | The API container is down or restarting. Check `ps` and `logs api`. |
| `password authentication failed for user "kuka"` in the API log | `POSTGRES_PASSWORD` was changed after the volume was created; PostgreSQL keeps the first password. Put the old one back, or reset the data with `down -v`. |
| `dotnet ef` not found | Run the `dotnet tool restore` command from step 5. |
