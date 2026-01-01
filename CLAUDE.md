# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Assistente Executivo is a full-stack AI assistant platform for executives with contact management, document automation, audio notes, and credit-based billing. Multi-language support (pt-BR, pt-PT, en-US, es-ES, it-IT, fr-FR).

## Tech Stack

- **Backend**: .NET 10.0, ASP.NET Core, EF Core with PostgreSQL, MediatR (CQRS), Serilog
- **Frontend**: Next.js 16.1, React 19, TypeScript, Tailwind CSS 4, next-intl
- **Auth**: Keycloak (OAuth 2.0/OIDC), JWT, BFF session pattern
- **AI**: OpenAI (gpt-4o-mini, whisper-1, tts-1)
- **Infrastructure**: Docker, Google Cloud Run, Redis (Upstash), Mailjet

## Build & Test Commands

### Backend (.NET)
```bash
dotnet build                              # Build solution
dotnet test                               # Run all tests
dotnet test --filter "FullyQualifiedName~TestName"  # Run single test
dotnet run --project backend/src/AssistenteExecutivo.Api
```

### Frontend (Next.js)
```bash
cd web
npm install
npm run dev       # Development server (port 3000)
npm run build     # Production build
npm run lint      # ESLint
```

### MCP Server
```bash
cd mcp-server
npm install
npm run build     # TypeScript compilation
npm run dev       # Development with tsx watch
```

### FastAPI (OCR/Audio)
```bash
cd fastapi
pip install -r requirements.txt
python server.py  # Run server (port 8000)
```

### Docker
```bash
docker compose -f docker/docker-compose.keycloak.yml up --build  # Keycloak + OCR
docker compose -f docker/docker-compose.web.yml --profile dev up  # Web dev
```

## Architecture

### Backend Layers (Clean Architecture)
```
Domain → Application → Infrastructure → Api
```

- **Domain** (`AssistenteExecutivo.Domain`): Entities, Value Objects, Domain Events, Interfaces
- **Application** (`AssistenteExecutivo.Application`): Commands, Queries, Handlers (MediatR), DTOs
- **Infrastructure** (`AssistenteExecutivo.Infrastructure`): EF Core repositories, OpenAI providers, external services
- **Api** (`AssistenteExecutivo.Api`): Controllers, Middleware, Auth handlers

### Key Patterns
- **CQRS**: Commands for mutations, Queries for reads, dispatched via MediatR
- **Repository Pattern**: Interface in Application, implementation in Infrastructure
- **Domain Events**: Entities publish events for cross-domain concerns
- **BFF Authentication**: Session cookies managed by backend, not exposed to frontend

### Frontend Structure
```
web/src/
├── app/           # Next.js App Router pages
├── components/    # Reusable React components
├── lib/
│   ├── api/       # API client wrappers (typed)
│   └── types/     # TypeScript interfaces
└── i18n/          # Internationalization (next-intl)
```

## Key Directories

| Path | Description |
|------|-------------|
| `backend/src/AssistenteExecutivo.Api/Controllers/` | REST API endpoints |
| `backend/src/AssistenteExecutivo.Application/Commands/` | Command definitions |
| `backend/src/AssistenteExecutivo.Application/Handlers/` | Command/Query handlers |
| `backend/src/AssistenteExecutivo.Domain/Entities/` | Domain entities |
| `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/` | AI provider implementations |
| `web/src/app/` | Next.js pages (App Router) |
| `web/src/lib/api/` | Frontend API clients |
| `mcp-server/src/tools/` | MCP tool definitions for Claude integration |

## Domain Entities

- **Contact**: Business contacts with relationships, emails, phones
- **Note**: Text and audio notes attached to contacts
- **DraftDocument**: Document drafts with status workflow (pending → approved → sent)
- **Template/Letterhead**: Document templates and letterheads
- **Reminder**: Scheduled reminders with status tracking
- **CreditWallet**: Usage credits and transactions
- **Plan**: Subscription tiers with limits

## Configuration

Backend configuration in `backend/src/AssistenteExecutivo.Api/appsettings.json`:
- `ConnectionStrings:DefaultConnection` - PostgreSQL
- `ConnectionStrings:Redis` - Redis (Upstash)
- `OpenAI` - API keys and model settings
- `Keycloak` - OAuth configuration
- `Email:Mailjet` - Email service

Frontend uses `NEXT_PUBLIC_API_BASE_URL` environment variable.

## Database

PostgreSQL with EF Core. Migration scripts in `backend/scripts/`.

## Language

- Code and comments: English
- UI text and user-facing content: Portuguese (Brazil) as primary, with translations
