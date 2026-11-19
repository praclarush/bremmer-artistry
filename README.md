# Bremmer Artistry

The Bremmer Artistry site: a landing page, a Gallery, the Pottery Journal piece catalog, an
Events/calendar section, class booking, FAQ, About, and Contact -- modeled on
[bremmer-artistry.art](https://www.bremmer-artistry.art/). Unlike the old [`Pottery`](../Pottery)
static site this replaces, the catalog, gallery, events, and classes are data-driven (Postgres)
and manageable by the site owner through an internally-authenticated admin area (email + password,
no third-party sign-in). Page copy (the Home headline and statement, FAQ, About) is
developer-maintained static content -- this is not a CMS.

## Structure

```
PotteryJournal.slnx
PRODUCT.md                         Who the site is for and what it has to do
DESIGN.md                          The visual system: palette, type scale, components, rules
src/
  PotteryJournal.SharedKernel/     Handler response types (HandlerResponse, DataHandlerResponse<T>,
                                    PagedHandlerResponse<T>) shared across the Infrastructure layer
  PotteryJournal.Infrastructure/   EF Core DbContext + entities/migrations, business logic handlers
                                    (Pieces, Events, Classes, Contact, ReferenceData, AdminSettings,
                                    AllowedAdmins), image resizing, ICS generation, event
                                    recurrence, SMTP email
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

Home (`/`), Gallery (`/gallery`), Pottery Journal (`/pottery-journal`), Events (`/events`), Classes
(`/classes`), FAQ (`/faq`), About (`/about`), Contact (`/contact`).

- **Home** is rendered entirely on the server, in the order a visitor deciding whether to come see
  the work needs it: the headline beside the featured collection's rotating photo, upcoming events
  (the next one as a wide feature, later ones as dated rows), a one-sentence statement about the
  work, a row of Gallery category tiles, then the footer. Each section drops out cleanly when its
  data is missing -- no featured collection, no events, or no Gallery pieces. With no upcoming
  events, the hero's main button points to the Gallery instead, and the events section offers
  Instagram and Contact links rather than an empty heading.
- **Gallery** shows category tiles, a per-category photo grid, and a lightbox. A piece appears
  only when it is marked "show in gallery" and has a category or a collection. A piece titled
  with a camera file name (e.g. "Bowls — 20250924_110013", from the bulk import) displays as its
  category and month instead; retitle it in the admin to replace the fallback.
- **Pottery Journal** carries over the old static site's grid, filter chips, search, and
  hash-routed (`#piece/<n>`) worksheet detail view -- same look, feel, and interaction, now backed
  by Postgres instead of a checked-in `pieces.json`. A piece shows here only when it has at least
  one glaze application and "Show in Pottery Journal" is checked (on by default), so test and
  practice pieces can be logged without going public. Newest pieces come first.
- **Events** offers a card list and a month calendar. Home's event links open this page scrolled
  to the specific event, with that card outlined.
- **Classes** is a two-step picker (class type, then a week of open times) that submits a
  tentative booking request for the studio to approve. It is **hidden by default**: with
  `Site:ShowClasses` false or unset, the nav and footer links disappear, Contact's intro drops the
  mention of classes, and `/classes`, its booking POST, and `/classes/data` all return 404. Turn
  it on with `Site__ShowClasses=true` as an environment variable (compose `environment:`, or an
  `envVars` entry in `render.yaml`) or `dotnet user-secrets set "Site:ShowClasses" "true"` for
  local debugging. The admin side (`/admin/classes`, bookings, availability) stays available
  either way, so classes can be set up before they go public.
- **FAQ** is hidden the same way behind `Site:ShowFaq` (off by default; `/faq` returns 404 and the
  links disappear) until it has real questions in it.
- **Contact** re-displays the form with everything the visitor typed when a send fails, plus a
  plain-language error and a mailto fallback. SMTP details go to the log, never to the page.

The footer on every page links the main sections plus email and Instagram. On phones the main
nav collapses behind a Menu button (`wwwroot/js/site-nav.js`); without JavaScript the links simply
wrap.

### Design

`PRODUCT.md` and `DESIGN.md` at the repo root are the source of truth for audience, tone, palette,
type scale, and component rules -- read them before changing public-facing UI. Content is held to
an 82rem maximum width on large screens; the gutter token that does this (`--page-x` in
`site.css`) has a couple of non-obvious rules, covered in `DESIGN.md` under "Page width".

## Admin area

`/admin`, gated by email/password sign-in against admin accounts stored in Postgres
(`AllowedAdmins` table, password hashed with ASP.NET Core's PBKDF2-based `PasswordHasher`) -- there
is no open signup and no third-party identity provider. From here the site owner can create/edit
pottery pieces (including photo upload, resized to a 1600px long edge and re-encoded JPEG quality
82), manage events, manage class availability, blackout dates and bookings, maintain the clay,
glaze, category, collection and class-type lists, edit notification settings, manage who else is
allowed to sign in, and change their own password from `/admin/change-password`.

**Event banner photos**: the Events page cards display the banner in a 16:9 crop and the Home
page's "next event" feature in a slightly taller 16:10 crop (both `object-fit: cover`), so upload
an image already close to 16:9 (e.g. 1600x900) with nothing important at the left or right edge --
a portrait or square photo will have most of its height cropped out of view. Uploads are resized
to a 1600px long edge (never upscaled) and re-encoded as JPEG quality 82, so 1600x900 is also the
effective ceiling for a landscape banner. A flyer belongs in the separate flyer upload, which
opens uncropped in a lightbox.

**Home page content**: the photo beside the headline comes from whichever collection is marked
"featured on homepage" under `/admin/reference-data` (one photo per piece in it, rotating); with
none featured, the headline stands alone. The Gallery row shows the first four Gallery categories
by name.

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

Both test projects run against EF Core's in-memory provider, so no Postgres or Docker is needed.

## Deployment

Same homelab pattern as the old `Pottery` project: built via `docker/Dockerfile`, deployed behind
Nginx Proxy Manager (TLS termination happens there, not in this app). Uploaded photos and Postgres
data persist in the `uploads` and `pgdata` named volumes.

### Deploying to Render

`render.yaml` is a [Render Blueprint](https://render.com/docs/infrastructure-as-code) that creates
everything the site needs: the Docker web service (built from `docker/Dockerfile`), a Postgres
database, and a persistent disk mounted at `/app/uploads` for photos.

1. Push the repo to GitHub, then in the Render dashboard choose **New > Blueprint** and select it.
2. Render prompts once for the secret values: the bootstrap admin email and password (same meaning
   as in [First-time setup](#first-time-setup)) and the `Smtp__*` settings. SMTP can be left blank
   and filled in later from the service's Environment tab; emails fail gracefully until then.
3. Apply. Render builds the image, runs migrations on first start, and seeds the admin account.
   Sign in at `/admin/login` and change the password.

Things to know:

- **Region** (`ohio`) is set on both the service and the database and can't be changed after
  creation -- edit it in `render.yaml` before the first apply if another region suits your visitors
  better. They must share a region to talk over Render's private network.
- **Plans** are the smallest available (`0.5c-512mb` web, `0.1c-256mb` Postgres). Photo uploads
  are resized in memory, so move the web service up a plan if large uploads time out or the
  service restarts under memory pressure. The disk is 2 GB and can be grown but never shrunk.
- **One instance only**: a service with a disk can't be scaled out, and each deploy has a short
  gap while the disk moves to the new instance.
- **Secrets** are set with `sync: false`, so they live only in Render, never in the repo, and
  later edits to `render.yaml` don't overwrite them.
- **The database URL**: Render supplies a `postgresql://` URL, which Npgsql can't read directly.
  `ConnectionStringNormalizer` converts it at startup; key/value strings (Docker, user-secrets)
  pass through untouched.
- **Backups**: check your Postgres plan's backup retention on Render, and take your own copy of
  the uploads disk -- it is the only place the photos live.

The deploy sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED` because Render terminates TLS in front of
the app; without it, auth cookies lose their `Secure` flag and the per-IP login rate limit sees
Render's proxy address for every visitor.

**Known gap**: ASP.NET Core's cookie auth Data Protection keys aren't currently persisted outside
the container (a startup warning flags this). Every container restart invalidates existing admin
sessions, forcing a re-login -- not a security issue, just an occasional inconvenience. Worth
persisting keys to a volume (`PersistKeysToFileSystem`) in a later pass.

## Not in this phase

See the "Explicitly deferred" section of the implementation plan this was built from: Gallery
cart/checkout, migrating the old 287-piece catalog, self-lockout protection when an admin edits
their own allow-list entry. (Category management and recurring events were on that list and have
since been built.)
