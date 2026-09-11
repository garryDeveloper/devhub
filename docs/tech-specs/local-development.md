# Local development

Everything needed to run DevHub on your machine, with no AWS account.

---

## 1. Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` |
| Node.js | 20 LTS+ | `node -v` |
| Docker Desktop | latest | `docker ps` |
| EF Core tools | 10.x | `dotnet tool install --global dotnet-ef` |
| Expo Go (phone) or an emulator | — | for mobile |

---

## 2. Ports

| Service | Port |
|---|---|
| API (HTTP) | 5080 |
| PostgreSQL | 5432 |
| pgAdmin / Adminer | 5050 |
| MinIO (S3) | 9000 / console 9001 |
| Web (Vite) | 5173 |
| Metro (mobile) | 8081 |

Keep these fixed — CORS origins, `.env` files and docs all reference them.

---

## 3. Infrastructure

`infrastructure/docker-compose.yml`:

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: devhub
      POSTGRES_PASSWORD: devhub
      POSTGRES_DB: devhub
    ports: ["5432:5432"]
    volumes: ["pgdata:/var/lib/postgresql/data"]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U devhub"]
      interval: 5s
      retries: 10

  adminer:
    image: adminer
    ports: ["5050:8080"]
    depends_on: [postgres]

  minio:                       # local S3, added in DEVHUB-089
    image: minio/minio
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: devhub
      MINIO_ROOT_PASSWORD: devhub123
    ports: ["9000:9000", "9001:9001"]
    volumes: ["miniodata:/data"]

volumes: { pgdata: {}, miniodata: {} }
```

```bash
docker compose -f infrastructure/docker-compose.yml up -d
docker compose -f infrastructure/docker-compose.yml logs -f postgres
docker compose -f infrastructure/docker-compose.yml down          # keeps data
docker compose -f infrastructure/docker-compose.yml down -v       # wipes data
```

---

## 4. Backend

```bash
cd api
dotnet restore
dotnet ef database update -p src/DevHub.Infrastructure -s src/DevHub.Api
dotnet run --project src/DevHub.Api
# → http://localhost:5080/swagger
```

Secrets never live in `appsettings.json`:

```bash
cd api/src/DevHub.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=devhub;Username=devhub;Password=devhub"
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
dotnet user-secrets set "Webhooks:DefaultSecret" "$(openssl rand -hex 32)"
dotnet user-secrets set "Storage:AccessKey" "devhub"
dotnet user-secrets set "Storage:SecretKey" "devhub123"
```

Configuration layering: `appsettings.json` (safe defaults) → `appsettings.Development.json` →
user-secrets → environment variables. In AWS the last two become SSM parameters injected as
environment variables (DEVHUB-097).

Common commands:

```bash
dotnet build DevHub.sln
dotnet test                                    # all tests (needs Docker for integration)
dotnet test tests/DevHub.Domain.UnitTests      # fast loop
dotnet watch --project src/DevHub.Api run      # hot reload

dotnet ef migrations add DEVHUB042_AddX -p src/DevHub.Infrastructure -s src/DevHub.Api
dotnet ef migrations remove -p src/DevHub.Infrastructure -s src/DevHub.Api   # only if not applied
dotnet ef migrations script -p src/DevHub.Infrastructure -s src/DevHub.Api   # review the SQL
```

---

## 5. Web

```bash
cd web
npm install
cp .env.example .env.local        # VITE_API_URL=http://localhost:5080
npm run dev                       # http://localhost:5173
npm run lint
npm run test
npm run build && npm run preview
```

---

## 6. Mobile

```bash
cd mobile
npm install
cp .env.example .env.local        # EXPO_PUBLIC_API_URL=http://<LAN-IP>:5080
npx expo start
# i = iOS simulator, a = Android emulator, or scan the QR with Expo Go
```

On a physical device `localhost` is the phone, not your computer. Use the machine's LAN IP
(`ipconfig getifaddr en0` on macOS, `hostname -I` on Linux) and make sure the API listens on
`0.0.0.0`, not only `127.0.0.1`.

---

## 7. Seed data

A `--seed` flag on the API (Development only) creates a demo dataset: one user
(`dev@devhub.local` / `Password123!`), one workspace, one project `DEV` with three environments,
~20 issues across statuses, a few labels, two releases and a handful of deployments and CI runs.

```bash
dotnet run --project src/DevHub.Api -- --seed
```

Seeding runs through the domain (not raw SQL) so it can never create invalid state, and it is
idempotent — running it twice does not duplicate anything.

---

## 8. Testing webhooks locally

```bash
# infrastructure/webhooks/send-deployment.sh
SECRET=$(cd api/src/DevHub.Api && dotnet user-secrets list | grep Webhooks | cut -d= -f2 | xargs)
BODY='{"externalId":"local-1","environment":"Production","version":"1.0.0","status":"Succeeded","branch":"main","occurredAt":"'"$(date -u +%FT%TZ)"'"}'
TS=$(date +%s)
SIG=$(printf '%s.%s' "$TS" "$BODY" | openssl dgst -sha256 -hmac "$SECRET" -hex | awk '{print $2}')
curl -sS -X POST http://localhost:5080/api/webhooks/deployments \
  -H 'Content-Type: application/json' \
  -H "X-DevHub-Project: $PROJECT_ID" \
  -H "X-DevHub-Timestamp: $TS" \
  -H "X-DevHub-Signature: sha256=$SIG" \
  --data-raw "$BODY"
```

For real GitHub deliveries, expose the API with `ngrok http 5080` and point the webhook at the
generated URL.

---

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `password authentication failed` | container recreated without the volume, or a stale password in user-secrets |
| `relation "issues" does not exist` | migrations not applied — `dotnet ef database update` |
| CORS error in the browser | origin not in the API allow-list, or the API restarted without the Development profile |
| 401 on every request after ~15 min | refresh flow broken; check the single-flight refresh in the API client |
| Integration tests hang | Docker not running, or the Testcontainers image is still pulling |
| Mobile "Network request failed" | using `localhost` on a device instead of the LAN IP |
| S3 upload fails in the browser only | MinIO/bucket CORS configuration (see `storage-s3-spec.md` §9) |
| Port already in use | `lsof -i :5080` and kill, or change the port in `launchSettings.json` |

---

## 10. Daily loop

```bash
docker compose -f infrastructure/docker-compose.yml up -d      # once per day
cd api  && dotnet watch --project src/DevHub.Api run           # terminal 1
cd web  && npm run dev                                         # terminal 2
cd mobile && npx expo start                                    # terminal 3 (when needed)
```

Before opening a PR:

```bash
cd api && dotnet build && dotnet test
cd web && npm run lint && npm run test && npm run build
cd mobile && npm run typecheck && npm run lint
```
