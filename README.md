# Vehicle Explorer

Search for a vehicle make, pick a model year, and see its vehicle types and models.
The data comes from the public [NHTSA vPIC API](https://vpic.nhtsa.dot.gov/api/).

**Live:** https://3.127.155.152.sslip.io

Built with .NET 10 (ASP.NET Core Web API) and Angular 22.
The reasons behind the main choices are in [DECISIONS.md](DECISIONS.md).

## Run with Docker

Needs Docker only.

```bash
docker compose up --build
```

Open http://localhost:8080. One container serves both the website and the API.

## Run for development

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node.js 24](https://nodejs.org).

```bash
# Terminal 1: the API on http://localhost:5080
dotnet run --project src/VehicleExplorer.Api

# Terminal 2: the website on http://localhost:4200 (it forwards /api to the API)
cd src/web
npm ci
npm start
```

Open http://localhost:4200.

## Tests

```bash
dotnet test                       # API, core and infrastructure tests
cd src/web && npm test -- --watch=false   # Angular tests
```

CI runs these on every push, together with a format check and a secret scan
([.github/workflows/ci.yml](.github/workflows/ci.yml)).

## API

| Request | Returns |
|---|---|
| `GET /api/makes?search=hon&limit=10` | Makes that match the search, best matches first (`limit` 1–50, default 10). |
| `GET /api/makes/474/model-years/2015` | The make's vehicle types with model counts, and its models for that model year. |
| `GET /health` | `{"status":"ok"}` |

Errors use the standard [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) format:
400 invalid input, 404 unknown make, 429 too many requests (100 per minute per IP),
503 NHTSA unavailable. Model years run from 1981 to next year.

## Project layout

| Folder | What it holds |
|---|---|
| `src/VehicleExplorer.Api` | Controllers, error handling, rate limiting, and the startup warm-up of the make list. |
| `src/VehicleExplorer.Core` | Make search ranking and the model year overview logic. No HTTP or framework code. |
| `src/VehicleExplorer.Infrastructure` | The NHTSA client, with retries and caching. |
| `src/web` | The Angular app. |
| `tests/VehicleExplorer.Tests` | xUnit tests for all three .NET projects. |
| `deploy`, `docs/deploy.md` | How the app runs on AWS EC2 behind Caddy (HTTPS). |
