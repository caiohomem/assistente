# Web (Next.js) no Docker

## Dev (localhost)

```bash
cd web
npm install
npm run dev
```

## Dev (Docker)

1) Garanta que o backend esteja acessível (ex: `http://localhost:5239`).
   - Importante: se o backend estiver rodando no **localhost** com bind em `127.0.0.1`, o container não consegue acessar.
   - Para permitir acesso via Docker, rode o backend em `0.0.0.0:5239` (veja profile `http-docker` em `backend/src/AssistenteExecutivo.Api/Properties/launchSettings.json`).
2) Suba o Next em modo dev no Docker:

```bash
docker compose -f docker/docker-compose.web.yml --profile dev up --build
```

- App: `http://localhost:3000`
- Debug Node inspector: `localhost:9229` (se estiver ocupado, o Next sobe em `9230`, `9231`, ...)

### Variáveis (dev)

Por padrão o container usa `NEXT_PUBLIC_API_BASE_URL=http://host.docker.internal:5239`.
Se quiser sobrescrever:

```bash
set NEXT_PUBLIC_API_BASE_URL=http://host.docker.internal:5239
docker compose -f docker/docker-compose.web.yml --profile dev up --build
```

## Prod (Docker local / Google Cloud)

Build + run local:

```bash
set NEXT_PUBLIC_API_BASE_URL=http://localhost:5239
docker compose -f docker/docker-compose.web.yml --profile prod up --build
```

- App: `http://localhost:8080`

### Cloud Run (Google Cloud)

- Use a imagem gerada pelo `Dockerfile` em `web/`.
- O container escuta na porta `8080` (Cloud Run injeta `PORT`, já suportado).

### Observação importante (NEXT_PUBLIC_*)

`NEXT_PUBLIC_API_BASE_URL` é embutida no build do frontend. Para trocar esse valor em produção,
o esperado é rebuildar a imagem com o build-arg/ENV correto.

## Debug no VS Code (attach)

1) Suba `web-dev` (profile `dev`) como acima.
2) Veja o “Debugger port” no log (`docker compose ... logs -f web-dev`).
3) No VS Code, use `Web (Docker): Attach Next.js (9229)` ou `(... 9230)` conforme o log.
