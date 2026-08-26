<!-- Scaffolded-app README. The template engine renames this file to README.md and excludes the
     repo's own marketing README (screenshots + phase tracker), which is NexaECommerce-internal. -->
# NexaECommerce

An opinionated **ASP.NET Core 10 + React 19** line-of-business application, scaffolded from an AI-ready starter template — auth, RBAC, audit, settings, a widget dashboard, webhooks, and (optionally) a sample domain are already wired and ready to extend.

## Run it

**Prerequisites:** .NET SDK **10.0**, Node.js **20+**, and a trusted dev HTTPS cert (`dotnet dev-certs https --trust`, first run only). The **SQLite** default needs no database setup — the file is created on first run.

```bash
dotnet run --project NexaECommerce.Server   # SpaProxy auto-starts the React client too
```

> **Using PostgreSQL or SQL Server?** Start the database first — a **`docker-compose.yml`** is included, so `docker compose up -d` gives you a local server — or point **`ConnectionStrings:Default`** in `appsettings.json` at your own. Switch providers anytime via **`Database:Provider`**.

Then open **https://localhost:3000** and sign in with the seeded dev admin:

```
admin@nexaecommerce.local  /  Admin123!$
```

On first run it creates the database, applies migrations, and seeds an admin (plus demo data, if you kept the Sales domain).

> **SPA won't start (`npm run dev` error) / no `*.client` in Solution Explorer?** If you created this in Visual Studio, leave **"Place solution and project in the same directory"** *unchecked* — checking it makes VS drop the React client project (a VS limitation with JavaScript `.esproj` projects). Recreate with it unchecked, or use the **Extensions → NexaECommerce** wizard, which always includes the client.

| Surface | URL |
|---|---|
| **App (use this)** | https://localhost:3000 |
| API | https://localhost:7000 |
| Interactive API docs (Scalar) | https://localhost:7000/scalar *(dev)* |
| Background jobs (Hangfire) | https://localhost:7000/hangfire *(dev)* |

## What's inside

**Platform** — RFC 7807 errors · FluentValidation · auditing · settings · caching · email · background jobs (Hangfire) · BLOB storage · an `IEndpointFilter` pipeline (validation / audit / performance / transaction).
**Auth & access** — ASP.NET Identity (cookie default, Bearer behind a flag) · email confirm + password reset · 2FA · OAuth (whichever providers you kept) · per-device sessions · roles + fine-grained, wildcard-capable permissions.
**Multi-tenancy** — always-on infrastructure, **off by default** (`Tenancy:Mode=MultiTenant` to enable).
**UX building blocks** — `<DataGrid>` + `useDataGrid` · a form layer · file upload + image processing · CSV/Excel/PDF export + import · standardized loading/empty/error states.
**Product features** — global ⌘K search · audit log + per-entity timeline · entity comments with `@mention` · a drag/resize widget dashboard · webhooks (HMAC-signed) · real-time notifications (SignalR) · i18n (5 languages + RTL) · theming + dark mode · health checks · rate limiting · PWA · onboarding tour · in-app changelog.

## Architecture in five bullets

1. **Backend vertical slices** under `Features/{Domain}/` — six files each; auto-registered by reflection. **Never edit `Program.cs` to add a feature.**
2. **Frontend file-system routes** under `src/pages/` — the path tree *is* the URL tree. `_`-prefixed = ignored by the router.
3. **Errors** = RFC 7807 ProblemDetails, always. Throw a `DomainException` subclass — never raw `Exception`.
4. **Lists** = `PagedRequest`/`PagedResult<T>` with operator-suffix query syntax (`?price=gte:10&sort=name:asc`).
5. **Cross-cutting concerns** = the `IEndpointFilter` pipeline, not per-handler wiring.

Copy `Features/_Template/` (backend) or `src/pages/_template/` (frontend) to start anything — the scaffolding *is* the canonical shape.

## Extend it (with or without an AI assistant)

This project is built to be **AI-extensible** — a consistent vertical-slice backend and file-system-routed frontend mean an assistant can add a whole feature from one prompt. The guidance lives in:

| Doc | What it's for |
|---|---|
| [docs/USER_GUIDE.md](docs/USER_GUIDE.md) | Run/set up + a tour of every feature. **Start here.** |
| [CLAUDE.md](CLAUDE.md) / [AGENTS.md](AGENTS.md) | AI-agent guidance (the canonical "what to do"). |
| [docs/CONVENTIONS.md](docs/CONVENTIONS.md) | One-screen conventions cheat-sheet. |
| [docs/RECIPES.md](docs/RECIPES.md) | Long-form, copy-pasteable how-tos (add a feature, widget, webhook, …). |

## Stack

- **Backend:** .NET 10 · Minimal APIs · EF Core 10 · ASP.NET Identity · FluentValidation · Serilog · Hangfire · SignalR · Scalar · QuestPDF · MailKit · Magick.NET
- **Frontend:** React 19 · Vite · TypeScript · Tailwind v4 · shadcn/ui · React Router 7 (file-system routes) · TanStack Query · Zustand
- **DB:** SQLite, PostgreSQL, or SQL Server — this project is wired for the one you chose; switch any time via `Database:Provider` + the connection string.

## License

Add your license here.
