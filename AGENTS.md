# Repository Guidelines

## Project Structure & Module Organization
- `backend/` houses the .NET solution and projects, with core layers under `backend/src/` and tests in `backend/tests/`.
- `web/` is the Next.js frontend (App Router) with source in `web/src/`.
- `mcp-server/` contains the TypeScript MCP server integration.
- `fastapi/` hosts a Python OCR/audio service (`server.py`).
- `app/` contains the Android client.
- `docker/` includes compose files for Keycloak and web dev environments.
- Supporting assets and workflows live in `documents/`, `n8n-workflows/`, `plans/`, and `samples/`.

## Build, Test, and Development Commands
- `dotnet build` builds the full .NET solution.
- `dotnet test` runs all backend xUnit tests in `backend/tests/`.
- `dotnet run --project backend/src/AssistenteExecutivo.Api` starts the API.
- `cd web && npm install && npm run dev` starts the Next.js dev server.
- `cd web && npm run build` creates a production build; `npm run lint` runs ESLint.
- `cd mcp-server && npm install && npm run dev` runs the MCP server with watch; `npm run build` compiles TypeScript.
- `cd fastapi && pip install -r requirements.txt && python server.py` runs the OCR/audio API.
- `docker compose -f docker/docker-compose.keycloak.yml up --build` starts Keycloak + OCR; `docker compose -f docker/docker-compose.web.yml --profile dev up` runs web dev services.

## Coding Style & Naming Conventions
- Follow the existing conventions in each module; keep formatting consistent with nearby files.
- .NET: PascalCase for types and namespaces, camelCase for locals/fields.
- TypeScript/React: PascalCase for components, camelCase for variables and functions.
- UI text is primarily Portuguese (pt-BR); code comments and identifiers are English.
- Use ESLint for `web/` and standard `dotnet` formatting for backend changes.

## Testing Guidelines
- Backend tests use xUnit and live under `backend/tests/` in `*.Tests` projects.
- Run tests with `dotnet test`; target a specific test via `dotnet test --filter "FullyQualifiedName~TestName"`.
- No dedicated frontend test script is defined yet; add one if you introduce web tests.

## Commit & Pull Request Guidelines
- Commit history is short and imperative (e.g., “Fix”, “Agreement”); keep messages brief and focused.
- PRs should include: summary of changes, testing performed, and screenshots for UI updates.
- Link related issues or tickets when applicable, and call out any config or migration steps.

## Configuration & Secrets
- Backend config lives in `backend/src/AssistenteExecutivo.Api/appsettings.json`; keep secrets in env vars.
- Frontend uses `NEXT_PUBLIC_API_BASE_URL`; document new env vars in your PR.
