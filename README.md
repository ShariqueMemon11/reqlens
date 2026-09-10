# ReqLens

AI requirements analyst. This repo is in **Phase 0 (scaffold)** — no upload, parsing, or LLM features yet.

## Prerequisites

- Node.js 24+
- .NET 10 SDK
- PostgreSQL locally (optional until a later phase; the connection string is configured but unused)

## Frontend

```bash
cd frontend
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173). You should see **ReqLens** and a dark/light toggle. The first visit follows your OS theme; after that the choice is stored in `localStorage`.

## Backend

```bash
cd backend
dotnet run --project ReqLens.Api
```

Health check:

```bash
curl http://localhost:5080/health
```

Expected: `{"status":"ok"}`.

CORS allows the Vite origin `http://localhost:5173`. The Postgres connection string lives in `backend/ReqLens.Api/appsettings.json` and is not used yet.

## Environment

Copy `.env.example` values as needed. Put `VITE_API_URL` in `frontend/.env` when the frontend starts calling the API. Never commit `.env`.

## Notes

- Tailwind CSS v4 is configured CSS-first. Theme tokens live in `frontend/src/styles/globals.css`. There is no `tailwind.config.ts` (documented deviation from `docs/ReqLens_Cursor_Build_Guide.md`).
- shadcn/ui is initialized (`components.json`, `src/lib/utils.ts`, default Button). Feature UI comes in later phases.
