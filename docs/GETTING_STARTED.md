# Welcome to NetForge 👋

Your new app was scaffolded from **NetForge** — an AI-ready **ASP.NET Core 10 + React 19** starter for line-of-business apps. It's built to be extended two ways: an **AI assistant can add a whole feature from a single prompt** (consistent backend vertical slices + file-system-routed frontend), and the code stays **clean enough for you to read and review at a glance**.

## Run it

Press **F5** (or **Ctrl+F5**) in Visual Studio — the ASP.NET Core API and the React client start together and your browser opens to the app. Sign in with the seeded dev admin (credentials are in the project **README.md**).

> **First-build tip:** the very first time, use **Build** (Ctrl+Shift+B) or **F5** — not **Rebuild**. On a brand-new multi-project solution Visual Studio restores packages in the background; if you *Rebuild* before that finishes you may briefly see *"assets file not found"*. Just **Build** once and it's resolved. (Standard Visual Studio behavior for any fresh multi-project solution.)

## Database

- **SQLite** (the default) — nothing to configure; the database file is created automatically on first run.
- **PostgreSQL / SQL Server** — a **`docker-compose.yml`** is included, so `docker compose up -d` gives you a local server; or point **`ConnectionStrings:Default`** in `appsettings.json` at your own. The schema is created on first run. You can switch providers anytime via **`Database:Provider`** + the connection string.

## Troubleshooting

**SPA won't start / the `*.client` project is missing?** If Visual Studio shows *"Couldn't start the SPA development server with command 'npm run dev'"* and the React `*.client` project isn't in **Solution Explorer**, it was dropped at creation. This happens when **"Place solution and project in the same directory"** is checked in the *New Project* dialog (a Visual Studio limitation with JavaScript projects). Recreate the project with that box **unchecked**, or use the **Extensions → NetForge** menu — its wizard always includes the client.

## Where to go next

| | |
|---|---|
| 📖 **Full setup + feature tour** | **[README.md](../README.md)** and **[USER_GUIDE.md](USER_GUIDE.md)** |
| 🧩 **Add a feature / widget / webhook** | **[RECIPES.md](RECIPES.md)** — copy `Features/_Template/` to start anything |
| 🤖 **Extend with an AI assistant** | **[CLAUDE.md](../CLAUDE.md)** / **[AGENTS.md](../AGENTS.md)** — the canonical "what to do" |
| 🌐 **Live demo & full docs** | https://demo.netforge.ebenmonney.com · https://docs.netforge.ebenmonney.com |

## NetForge Pro

The full NetForge platform adds **multi-tenancy, outgoing webhooks, audit trails, a widget dashboard, real-time notifications, background jobs, global ⌘K search, CSV/Excel/PDF export/import**, and a ready-made sample Sales domain. If your scaffold didn't include some of these, unlock the full edition at **https://netforge.ebenmonney.com**.

Happy building! 🚀
