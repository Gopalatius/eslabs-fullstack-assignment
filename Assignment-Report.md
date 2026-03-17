# Assignment Report

## Environment

- OS: macOS on Apple Silicon
- Node.js: `v22.22.1`
- npm: `10.9.4`
- .NET SDK: `8.0.419`
- sqlite3: `3.51.0`

Upstream sample revisions used:

- `express-template`: `045cce7`
- `vue-antd-template`: `72fc71b`

## VueJS + ExpressJS

### Objective

Run the provided backend and frontend, configure the environment if necessary, and verify a successful login to the dashboard from the frontend.

### What was done

Backend:

- installed dependencies in:
  - repository root
  - `apps`
- ran the backend with:

```bash
NODE_ENV=development npm run local
```

Frontend:

- installed dependencies in:
  - repository root
  - `apps`
- ran the sample frontend with:

```bash
npm run sample
```

### Verified configuration

- backend URL: `http://127.0.0.1:3000`
- frontend URL: `http://127.0.0.1:8080`
- provided frontend env already points to the backend:
  - `VITE_API_URL=http://127.0.0.1:3000`
  - `VITE_WITH_CREDENTIALS=include`
- sample login credentials:
  - username: `test`
  - password: `test`
  - OTP: `111111`

### Important note on the provided sample

The provided backend already includes a populated SQLite development database at `apps/app-sample/dev.sqlite3`, so the requested dashboard path did not require an additional migration/seed step for the shipped local sample data.

### Compatibility fix applied

The current upstream sample had an OTP login issue under the installed dependency set:

- the OTP path failed during user/token handling even though username/password login correctly returned the OTP challenge

A small app-level compatibility fix was applied in:

- `express-template/base/controller/auth/own.js`

The fix does two things:

- uses a direct DB lookup fallback for the OTP user lookup path
- normalizes the auth ID to a string before refresh-token storage

This was the minimum change needed to make the provided login flow reach the dashboard successfully.

### Docker validation for the provided backend

The provided backend Docker path was also validated. A small runtime compatibility adjustment was required in the upstream Dockerfile so the bundled SQLite dependency could run correctly inside the production image.

The Dockerized backend was verified for:

- healthcheck
- login challenge
- OTP token issuance

### Proof artifacts

- backend healthcheck response:
  - `submission-artifacts/proof/backend/healthcheck.json`
- backend login response:
  - `submission-artifacts/proof/backend/login.json`
- backend OTP response:
  - `submission-artifacts/proof/backend/otp.json`
- backend Docker healthcheck response:
  - `submission-artifacts/proof/backend/docker-healthcheck.json`
- backend Docker login response:
  - `submission-artifacts/proof/backend/docker-login.json`
- backend Docker OTP response:
  - `submission-artifacts/proof/backend/docker-otp.json`
- frontend sign-in screenshot:
  - `submission-artifacts/proof/frontend/signin.png`
- frontend OTP screenshot:
  - `submission-artifacts/proof/frontend/otp.png`
- frontend dashboard screenshot:
  - `submission-artifacts/proof/frontend/dashboard.png`

## C# Deliverables

### REST API

- project: `assignment-csharp/src/RestApi`
- endpoints:
  - `GET /health`
  - `GET /api/tasks`
  - `POST /api/tasks`
- characteristics:
  - minimal API
  - in-memory task store
  - Swagger in development
  - immutable record DTOs
  - configurable CORS
  - validation on task creation
  - problem-details style error handling
  - integration tests included

### WebSocket application

- project: `assignment-csharp/src/WebSocketApp`
- endpoints:
  - `GET /health`
  - `GET /` browser demo page
  - `GET /ws`
- characteristics:
  - accepts JSON messages shaped like `{"type":"echo","message":"..."}`
  - returns echoed JSON with `clientId` and `timestamp`
  - tracks active connections for logging
  - enforces a configurable max message size
  - handles invalid payloads and graceful disconnects
  - integration test included

### Containerization

- `assignment-csharp/docker-compose.yml` runs both C# apps in containers
- each app includes a multi-stage Dockerfile for Linux-oriented portability
- Docker verification completed on macOS using a local Linux container runtime
- proof artifacts:
  - `submission-artifacts/proof/csharp-rest/docker-health.json`
  - `submission-artifacts/proof/csharp-websocket/docker-health.json`
  - `submission-artifacts/proof/csharp-websocket/docker-echo.json`
  - `submission-artifacts/proof/csharp-docker-ps.txt`

### Proof artifacts

REST API:

- health response:
  - `submission-artifacts/proof/csharp-rest/health.json`
- tasks before create:
  - `submission-artifacts/proof/csharp-rest/tasks-before.json`
- created task response:
  - `submission-artifacts/proof/csharp-rest/create-task.json`
- tasks after create:
  - `submission-artifacts/proof/csharp-rest/tasks-after.json`

WebSocket:

- echo response:
  - `submission-artifacts/proof/csharp-websocket/echo.json`

## Automated Validation

Executed successfully:

- `dotnet test tests/RestApi.Tests/RestApi.Tests.csproj`
- `dotnet test tests/WebSocketApp.Tests/WebSocketApp.Tests.csproj`

The frontend proof screenshots were captured automatically with Playwright using:

- `submission-artifacts/capture-frontend-proof.mjs`
