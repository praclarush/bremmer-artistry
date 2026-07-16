# Bremmer Artistry

The Bremmer Artistry site: a landing page, the Pottery Journal piece catalog, an Events/calendar
section, FAQ, and About -- modeled on [bremmer-artistry.art](https://www.bremmer-artistry.art/).
Unlike the old [`Pottery`](../Pottery) static site this replaces, the catalog and events are
data-driven (Postgres) and manageable by the site owner through a Google-OAuth-gated admin area.
Everything else (Home, Gallery, FAQ, About copy) is developer-maintained static content -- this is
not a CMS.

## Structure

```
PotteryJournal.sln
src/
  PotteryJournal.SharedKernel/     Handler response types (HandlerResponse, DataHandlerResponse<T>,
                                    PagedHandlerResponse<T>) shared across the Infrastructure layer
  PotteryJournal.Infrastructure/   EF Core DbContext + entities/migrations, business logic handlers
                                    (Pieces, Events, AllowedAdmins), image resizing, ICS generation
  PotteryJournal.Web/              Razor Pages (public site + /admin), Google OAuth + cookie auth,
                                    the plain JSON data endpoints backing the public pages' scripts
tests/
  PotteryJournal.Infrastructure.Tests/   NUnit3 + Moq, EF Core InMemory provider
  PotteryJournal.Web.Tests/              WebApplicationFactory integration tests
docker/
  Dockerfile                       Multi-stage build for PotteryJournal.Web
docker-compose.yml                 App + Postgres + named volumes (pgdata, uploads)
```

## Public site

Home (`/`), Gallery (`/gallery`), Pottery Journal (`/pottery-journal`), Events (`/events`), FAQ
(`/faq`), About (`/about`). The Pottery Journal page carries over the old static site's grid,
filter chips, search, and hash-routed (`#piece/<n>`) worksheet detail view -- same look, feel, and
interaction, now backed by Postgres instead of a checked-in `pieces.json`.

## Admin area

`/admin`, gated by Google OAuth against an allow-list stored in Postgres (`AllowedAdmins` table) --
there is no open signup. From here the site owner can create/edit pottery pieces (including photo
upload, resized to a 1600px long edge and re-encoded JPEG quality 82), manage events, and manage
who else is allowed to sign in.

### First-time setup

1. **Google OAuth client**: in Google Cloud Console, create an OAuth 2.0 Client ID (Web
   application). Set the authorized redirect URI to `https://<your-domain>/signin-google`. Put the
   client ID/secret in `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET`.
2. **Bootstrap admin**: set `BOOTSTRAP_ADMIN_EMAIL` to the first Google account that should have
   access. On first startup, if the `AllowedAdmins` table is empty, it's seeded with this email.
   After that, manage the list from `/admin/allowed-admins` -- redeploys don't reset it.
3. Copy `.env.example` to `.env` and fill in the values above plus `POSTGRES_PASSWORD`. `.env` is
   gitignored -- never commit real secrets.

**Google's remote authentication handler validates its options (including that ClientId/ClientSecret
are non-empty) on every single request**, not just authenticated ones -- it needs to check whether
each request is its OAuth callback. This means the app will 500 on every page, not just `/admin`,
if `GOOGLE_CLIENT_ID`/`GOOGLE_CLIENT_SECRET` aren't set. There's no way to make Google auth "later,
once it matters" -- it's load-bearing from the first deploy.

## Running locally

```bash
cp .env.example .env   # fill in real values
docker compose up --build
```

App on `http://localhost:8080`, Postgres on the `db` service (not published to the host by
default). EF Core migrations run automatically on startup.

Without Docker: `dotnet run --project src/PotteryJournal.Web`, with a `PotteryJournal` connection
string in `appsettings.Development.json` or user-secrets pointing at a local Postgres instance, and
the same `Authentication:Google:ClientId`/`ClientSecret` config keys.

The dev machine used to build this only has the .NET 10 SDK/runtime installed, not .NET 8 -- the
projects target `net8.0` (per house style) but set `<RollForward>LatestMajor</RollForward>` so
`dotnet run`/`dotnet test` work locally via the installed 10.0 runtime. Docker always uses the real
`aspnet:8.0` runtime image regardless.

## Tests

```bash
dotnet test
```

## Deployment

Same homelab pattern as the old `Pottery` project: built via `docker/Dockerfile`, deployed behind
Nginx Proxy Manager (TLS termination happens there, not in this app). Uploaded photos and Postgres
data persist in the `uploads` and `pgdata` named volumes.

**Known gap**: ASP.NET Core's cookie auth Data Protection keys aren't currently persisted outside
the container (a startup warning flags this). Every container restart invalidates existing admin
sessions, forcing a re-login -- not a security issue, just an occasional inconvenience. Worth
persisting keys to a volume (`PersistKeysToFileSystem`) in a later pass.

## Not in this phase

See the "Explicitly deferred" section of the implementation plan this was built from: Gallery
cart/checkout, migrating the old 287-piece catalog, category taxonomy management UI, recurring
events, self-lockout protection when an admin edits their own allow-list entry.
