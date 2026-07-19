# Bremmer Artistry

The Bremmer Artistry site: a landing page, the Pottery Journal piece catalog, an Events/calendar
section, FAQ, and About -- modeled on [bremmer-artistry.art](https://www.bremmer-artistry.art/).
Unlike the old [`Pottery`](../Pottery) static site this replaces, the catalog and events are
data-driven (Postgres) and manageable by the site owner through an internally-authenticated admin
area (email + password, no third-party sign-in). Everything else (Home, Gallery, FAQ, About copy)
is developer-maintained static content -- this is not a CMS.

## Structure

```
PotteryJournal.sln
src/
  PotteryJournal.SharedKernel/     Handler response types (HandlerResponse, DataHandlerResponse<T>,
                                    PagedHandlerResponse<T>) shared across the Infrastructure layer
  PotteryJournal.Infrastructure/   EF Core DbContext + entities/migrations, business logic handlers
                                    (Pieces, Events, AllowedAdmins), image resizing, ICS generation
  PotteryJournal.Web/              Razor Pages (public site + /admin), cookie auth against
                                    internally-stored credentials, the plain JSON data endpoints
                                    backing the public pages' scripts
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

`/admin`, gated by email/password sign-in against admin accounts stored in Postgres
(`AllowedAdmins` table, password hashed with ASP.NET Core's PBKDF2-based `PasswordHasher`) -- there
is no open signup and no third-party identity provider. From here the site owner can create/edit
pottery pieces (including photo upload, resized to a 1600px long edge and re-encoded JPEG quality
82), manage events, manage who else is allowed to sign in, and change their own password from
`/admin/change-password`.

**Event banner photos**: the Home page and Events page card lists both display the banner in a
16:9 crop (`object-fit: cover`), so upload an image already close to a 16:9 aspect ratio (e.g.
1600x900) -- a portrait or square photo will have most of its height cropped out of view. Uploads
are resized to a 1600px long edge (never upscaled) and re-encoded as JPEG quality 82, so 1600x900
is also the effective ceiling for a landscape banner.

### First-time setup

1. **Bootstrap admin**: set `BOOTSTRAP_ADMIN_EMAIL` and `BOOTSTRAP_ADMIN_PASSWORD` to the first
   admin's credentials. On first startup, if the `AllowedAdmins` table is empty, it's seeded with
   this account. After that, manage the list from `/admin/allowed-admins` -- redeploys don't reset
   it, and the bootstrap account should change its password after first sign-in.
2. Copy `.env.example` to `.env` and fill in the values above plus `POSTGRES_PASSWORD`. `.env` is
   gitignored -- never commit real secrets.
3. When adding further admins from `/admin/allowed-admins`, you set their initial password
   directly in the form (there's no invite-email flow) -- they can change it themselves afterward.
4. **SMTP** (optional but needed for class booking notifications/confirmations and the Contact Us
   form): set `SMTP_HOST`/`SMTP_PORT`/`SMTP_USER`/`SMTP_PASSWORD`/`SMTP_FROM_ADDRESS` in `.env`.
   Left blank, those email sends fail gracefully (reported as an error, not a crash) -- fine if
   you're not exercising those flows locally.

Sign-in attempts against `/admin/login` are rate-limited (10/minute per IP) since there's no
external identity provider absorbing brute-force attempts anymore.

## Running locally

```bash
cp .env.example .env   # fill in real values
docker compose up --build
```

App on `http://localhost:8080`, Postgres on the `db` service (not published to the host by
default). EF Core migrations run automatically on startup.

### Debugging without Docker (e.g. from Visual Studio)

`appsettings.json` ships with an empty `ConnectionStrings:PotteryJournal` on purpose -- the real
value is normally injected by `docker-compose.yml`'s `ConnectionStrings__PotteryJournal` env var,
which only applies inside the `app` container. Running the project directly (`dotnet run` or
Visual Studio F5) skips that entirely, so `dbContext.Database.MigrateAsync()` fails with `Host
can't be null` unless you supply a connection string yourself. Also note the `db` service's 5432
isn't published to the host by default, so even with a connection string there's nothing to
connect to.

To debug against the same Postgres the Docker dev stack uses:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d db
```

This publishes Postgres to `localhost:55432` (not 5432 -- pick a free port if that one's already
taken by something else on your machine). Then set the connection string and bootstrap admin vars
via [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) (never commit
these to `appsettings.Development.json`):

```bash
cd src/PotteryJournal.Web
dotnet user-secrets set "ConnectionStrings:PotteryJournal" "Host=localhost;Port=55432;Database=potteryjournal;Username=potteryjournal;Password=<POSTGRES_PASSWORD from .env>"
dotnet user-secrets set "POTTERYJOURNAL_BOOTSTRAP_ADMIN_EMAIL" "<BOOTSTRAP_ADMIN_EMAIL from .env>"
dotnet user-secrets set "POTTERYJOURNAL_BOOTSTRAP_ADMIN_PASSWORD" "<BOOTSTRAP_ADMIN_PASSWORD from .env>"
```

SMTP settings follow the same pattern if you need to test email locally, e.g.
`dotnet user-secrets set "Smtp:Host" "<value>"` (and `Smtp:Port`, `Smtp:User`, `Smtp:Password`,
`Smtp:FromAddress`) -- these map to `appsettings.json`'s nested `Smtp` section rather than a flat
`POTTERYJOURNAL_`-prefixed key, since it's structured config, not a single secret.

User secrets only load when `ASPNETCORE_ENVIRONMENT=Development`, which the `http`/`https` launch
profiles already set -- run/debug via one of those profiles (Visual Studio's default), not
`--no-launch-profile`.

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
