# Repository Guidelines

## Project Structure & Module Organization
- `backend/` holds the .NET solution; core layers live in `backend/src/` and tests in `backend/tests/`.
- `web/` is the Next.js frontend (App Router) with source in `web/src/`.
- `mcp-server/` contains the TypeScript MCP server integration.
- `fastapi/` hosts the Python OCR/audio service (`server.py`).
- `app/` contains the Android client.
- `n8n-workflows/` stores workflow exports; `docker/` contains local compose files.
- Supporting docs live in `documents/`, plus `plans/` and `samples/` for specs and examples.

## Build, Test, and Development Commands
- `dotnet build` builds the full .NET solution.
- `dotnet test` runs backend xUnit tests in `backend/tests/`.
- `dotnet run --project backend/src/AssistenteExecutivo.Api` starts the API locally.
- `cd web && npm install && npm run dev` starts the Next.js dev server (port 3000).
- `cd web && npm run build` builds the frontend; `npm run lint` runs ESLint.
- `cd mcp-server && npm install && npm run dev` runs the MCP server with watch; `npm run build` compiles it.
- `cd fastapi && pip install -r requirements.txt && python server.py` starts the OCR/audio API (port 8000).
- `docker compose -f docker/docker-compose.keycloak.yml up --build` launches Keycloak + OCR; `docker compose -f docker/docker-compose.web.yml --profile dev up` runs web dev services.

## Coding Style & Naming Conventions
- Follow conventions already used in each module; avoid reformatting unrelated code.
- .NET: PascalCase for types/namespaces, camelCase for locals/fields.
- TypeScript/React: PascalCase for components, camelCase for variables/functions.
- UI text is primarily Portuguese (pt-BR); code identifiers and comments remain in English.
- Use existing linters/formatters (`dotnet format` conventions, ESLint in `web/`).

## Testing Guidelines
- Backend tests are xUnit; keep new tests under `backend/tests/` alongside related projects.
- Run single tests with `dotnet test --filter "FullyQualifiedName~TestName"`.
- No dedicated frontend test suite is configured; note any manual verification in PRs.

## Commit & Pull Request Guidelines
- Commit history uses short, imperative messages (e.g., “Fix”); keep them concise and scoped.
- PRs should include: summary, testing performed, and screenshots for UI changes.
- Link related issues and call out any migrations or new configuration keys.

## Configuration & Secrets
- Backend config lives in `backend/src/AssistenteExecutivo.Api/appsettings.json`; keep secrets in env vars.
- Frontend uses `NEXT_PUBLIC_API_BASE_URL`; document new env vars in PRs.
- Workflow exports live in `n8n-workflows/`; update this folder when changing automation flows.
