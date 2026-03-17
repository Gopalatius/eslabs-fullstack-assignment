# C# Assignment Deliverables

This solution contains two small ASP.NET Core applications designed to be easy to run on macOS and straightforward to deploy on a Unix-like environment. Both services can be run directly with `dotnet` or through Docker.

## Projects

- `src/RestApi`
  - Minimal REST API
  - Endpoints:
    - `GET /health`
    - `GET /api/tasks`
    - `POST /api/tasks`
  - Includes Swagger/OpenAPI in development, immutable record DTOs, task validation, configurable CORS, and problem-details style error handling
- `src/WebSocketApp`
  - Minimal WebSocket echo service
  - Endpoints:
    - `GET /health`
    - `GET /` serves a small browser-based WebSocket test page
    - `GET /ws` for WebSocket upgrade requests
  - Includes configurable allowed origins, keepalive, max message size enforcement, connection logging, and graceful disconnect handling
- `tests/RestApi.Tests`
  - REST API integration tests
- `tests/WebSocketApp.Tests`
  - WebSocket integration tests

## Prerequisites

- .NET SDK 8

Verify the installation with:

```bash
dotnet --version
```

## Run Locally

REST API:

```bash
ASPNETCORE_URLS=http://127.0.0.1:5090 dotnet run --no-launch-profile --project src/RestApi/RestApi.csproj
```

WebSocket app:

```bash
ASPNETCORE_URLS=http://127.0.0.1:5091 dotnet run --no-launch-profile --project src/WebSocketApp/WebSocketApp.csproj
```

## Verify

REST API:

```bash
curl http://127.0.0.1:5090/health
curl http://127.0.0.1:5090/api/tasks
curl -H 'content-type: application/json' \
  -d '{"title":"Prepare AI callbot assignment walkthrough"}' \
  http://127.0.0.1:5090/api/tasks
```

WebSocket app:

- open `http://127.0.0.1:5091` in a browser and use the built-in demo page, or
- use a CLI client:

```bash
node - <<'NODE'
const ws = new WebSocket('ws://127.0.0.1:5091/ws');
ws.addEventListener('open', () => {
  ws.send(JSON.stringify({ type: 'echo', message: 'hello from client' }));
});
ws.addEventListener('message', (event) => {
  console.log(String(event.data));
  ws.close();
});
NODE
```

## Test

```bash
dotnet test tests/RestApi.Tests/RestApi.Tests.csproj
dotnet test tests/WebSocketApp.Tests/WebSocketApp.Tests.csproj
```

## Run With Docker

Build and start both services:

```bash
docker compose up --build
```

Then verify:

```bash
curl http://127.0.0.1:5090/health
open http://127.0.0.1:5090/swagger
open http://127.0.0.1:5091
```

## Publish

REST API:

```bash
dotnet publish src/RestApi/RestApi.csproj -c Release -o out/RestApi
```

WebSocket app:

```bash
dotnet publish src/WebSocketApp/WebSocketApp.csproj -c Release -o out/WebSocketApp
```

## Linux Deployment Notes

Example `systemd` service for the REST API:

```ini
[Unit]
Description=Assignment REST API
After=network.target

[Service]
WorkingDirectory=/opt/assignment/restapi
ExecStart=/usr/bin/dotnet /opt/assignment/restapi/RestApi.dll
Environment=ASPNETCORE_URLS=http://127.0.0.1:5090
Restart=always
RestartSec=5
SyslogIdentifier=assignment-restapi
User=www-data

[Install]
WantedBy=multi-user.target
```

Example `systemd` service for the WebSocket app:

```ini
[Unit]
Description=Assignment WebSocket App
After=network.target

[Service]
WorkingDirectory=/opt/assignment/websocket
ExecStart=/usr/bin/dotnet /opt/assignment/websocket/WebSocketApp.dll
Environment=ASPNETCORE_URLS=http://127.0.0.1:5091
Restart=always
RestartSec=5
SyslogIdentifier=assignment-websocket
User=www-data

[Install]
WantedBy=multi-user.target
```

Example Nginx reverse proxy:

```nginx
server {
    listen 80;
    server_name example.local;

    location /api/ {
        proxy_pass http://127.0.0.1:5090/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /ws/ {
        proxy_pass http://127.0.0.1:5091/ws;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}
```

## Submission Notes

- Submit source code, tests, README, and curated proof artifacts.
- Exclude generated folders such as `bin/`, `obj/`, and local test result directories.
- For the review, demo the REST API first, then use the WebSocket browser page to show a successful echo round-trip.
