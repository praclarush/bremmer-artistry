# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

## Repository Purpose

The Bremmer Artistry site -- ASP.NET Core (net8.0) + EF Core + Postgres. Successor to the static
[`Pottery`](../Pottery) project: same look/feel/interaction for the piece catalog, now data-driven
and expanded into a full site (Home, Gallery, Pottery Journal, Events, FAQ, About) with an
internally-authenticated admin area. See [README.md](README.md) for the directory layout and setup.

## Design Context

`PRODUCT.md` and `DESIGN.md` at the repo root capture the strategic and visual design system
(register: brand-led public site with a utility admin area; North Star "The Kiln-Lit Ledger" --
dark navy/copper palette, Fraunces/Inter/IBM Plex Mono, the piece "worksheet" as the signature
component). Read them before any `/impeccable` design work or UI changes to the public site.

## Architecture Notes

- **No separate API project.** `PotteryJournal.Web` hosts both the Razor Pages (public site +
  admin) and the plain JSON data endpoints (`/pottery-journal/data`, `/events/data`,
  `/events/{id}/ics`) the pages' own client-side scripts fetch. These are not a versioned/public
  API -- no Swagger, no API-key auth, no `Asp.Versioning`. Don't add that ceremony back in without
  a real external consumer driving the need.
- **No ASP.NET Core Identity, and no third-party sign-in.** Auth was Google OAuth originally, then
  switched to internally-managed credentials (see project history if the "why" ever matters). It's
  plain cookie authentication, signed in directly by `Login.cshtml.cs` after
  `IAllowedAdminsHandler.ValidateCredentialsAsync` checks an email/password against the
  `AllowedAdmins` table. Password hashing uses `Microsoft.AspNetCore.Identity.PasswordHasher<T>`
  from the `Microsoft.Extensions.Identity.Core` NuGet package -- that's just the PBKDF2 hashing
  utility, not full ASP.NET Core Identity; there's no `UserManager`, no Identity EF Core store, no
  Identity-shaped tables. Don't pull in full Identity to "do this properly" -- the utility class is
  the properly-scoped choice for a single admin-accounts table with no roles/claims/2FA.
- `/admin/login` is rate-limited (`RateLimiterPolicies.LoginAttempts`, 10/min/IP via
  `[EnableRateLimiting]` on `LoginModel`) since there's no external identity provider absorbing
  brute-force attempts anymore. Any new unauthenticated POST endpoint that takes user-supplied
  credentials should get the same treatment.
- **SharedKernel's `HandlerResponse`/`DataHandlerResponse<T>`/`PagedHandlerResponse<T>`** were
  built fresh in this repo (`PotteryJournal.SharedKernel/Core/`) -- there was no existing shared
  NuGet package to reference. All `PotteryJournal.Infrastructure` handler methods return one of
  these three types; see `SharedKernel/Core/*.cs` for the pattern.
- **SQL standards translation**: the SQL Server-oriented naming/structure rules were translated to
  Postgres equivalents in the initial migration -- `uuid`/`gen_random_uuid()` instead of
  `uniqueidentifier`/`newid()`, `COMMENT ON` instead of `MS_Description` extended properties, a
  dedicated `potteryjournal` schema instead of avoiding `dbo`. PascalCase table/column names are
  kept (EF Core's Npgsql provider quotes identifiers automatically).
- **Piece photo and event banner uploads** are resized (1600px long edge, never upscaled) and
  re-encoded (JPEG quality 82) by `ImageStorageService` before being saved to the `uploads` volume,
  matching the convention documented in the old `Pottery` project's CLAUDE.md.
- **Gallery is deliberately independent of the Pottery Journal.** They share the same `Piece` rows
  (no separate content type -- avoids duplicate data entry) but the relationship is opt-in, not
  automatic: a piece only appears on `/gallery` when `Piece.ShowInGallery` is true *and* it has
  either a `Piece.CategoryId` or a `Piece.CollectionId` set (see the Collection note below --
  Collection is a fallback grouping when Category is blank, not a duplicate of it). Every piece is
  loggable for record-keeping (glaze combos, clay, notes) without ever surfacing publicly. This
  replaced an earlier design where any piece with a
  non-empty `Category` auto-generated a Gallery tile, and clicking that tile just navigated to
  `/pottery-journal?category=X` -- i.e. Gallery used to be a filtered lens into the Journal, not its
  own thing. Don't reintroduce that coupling. `Gallery.cshtml`/`gallery.js` now fetch their own
  `/gallery/data` endpoint (`IPieceHandler.GetGalleryPiecesAsync`, filtered on `ShowInGallery`) and
  render category tiles -> a drill-down photo grid -> a lightbox entirely client-side, independent
  of `pottery-journal.js`'s own data and routing.
- **Clay, Glaze, and Category are managed lookup tables (`ClayBodies`/`Glazes`/`Categories`), not
  free-text fields.** `Piece` holds `ClayBodyId`/`CategoryId` FKs (nullable, `SetNull` on delete);
  `GlazeApplication` holds a required `GlazeId` FK (`Restrict` on delete -- a glaze in use can't be
  removed). Admins manage the three lists from `/admin/reference-data`
  (`IReferenceDataHandler`/`ReferenceDataHandler`), and `Admin/Pieces/Edit.cshtml` renders them as
  `<select>` dropdowns instead of free-text inputs. A piece with no glaze applications never
  surfaces outside the admin portal -- `PieceHandler.GetAllDetailsAsync` (used by the public
  `/pottery-journal/data` endpoint) filters on `GlazeApplications.Any()`; admin-only reads
  (`GetByIdAsync`, `GetSummariesPagedAsync`) are unfiltered.
- **Collection is a separate concept from Category, not a synonym for it -- but it's a Gallery
  fallback, not fully independent of it.** Category groups pieces by form/type on the Gallery;
  Collection (`Collections` table, `Piece.CollectionId`) groups pieces by a named body of work
  (e.g. "Lightning-Cracked Collection") for the homepage's rotating showcase, independent of
  whatever category those pieces also carry. `PieceHandler.GetGalleryPiecesAsync` uses
  `Category?.Name ?? Collection!.Name` as each piece's Gallery tile label, so a piece with a
  Collection but no Category still gets a Gallery tile (named after the Collection) instead of
  being silently excluded -- a piece that has both keeps grouping under its Category, unaffected.
  At most one collection has
  `IsFeaturedOnHomepage = true` at a time -- `ReferenceDataHandler.SetCollectionFeaturedAsync`
  un-features any previous one before featuring the new one, so callers never need to clear the old
  flag themselves. `IndexModel` fetches the featured set via
  `IPieceHandler.GetFeaturedCollectionAsync()` (null when nothing is featured or the featured
  collection has no photographed pieces); `wwwroot/js/featured-collection.js` auto-advances the
  crossfade and explicitly no-ops under `prefers-reduced-motion` rather than relying solely on the
  global CSS override.
- **Recurrence (`Event` and `ClassAvailability`) is expanded dynamically on read, never persisted as
  occurrence rows.** Both entities carry the same three fields (`RecurrenceFrequency`,
  `RecurrenceInterval`, `RecurrenceEndDate`) and are expanded by the shared
  `IRecurrenceExpander`/`RecurrenceExpander` (`Infrastructure/Services/`), which wraps Ical.Net's
  `RecurrencePattern`/`CalendarEvent.GetOccurrences(...)` -- the same library already used for `.ics`
  export -- rather than a hand-rolled recurrence implementation. `RecurrenceFrequencyMapper` maps the
  local enum to Ical.Net's `FrequencyType` in one place, shared by both the expander and
  `IcsGenerator` (a recurring event's `.ics` download emits a real `RRULE`, not a flat single date).
  Because occurrences are virtual, deleting a recurring `Event` or `ClassAvailability` row always
  removes the whole series -- there's no per-occurrence edit/delete, and no orphaned rows to clean up
  either way. `EventsHandler` exposes three shapes over the same data: `GetAllAsync` (series-level,
  unexpanded -- the admin CRUD list, since edit/delete act on the whole series) `GetUpcomingAsync`
  (expands recurring events forward, bounded by an internal window so indefinite recurrence can't
  generate unbounded lists; non-recurring events stay unbounded, unchanged from before recurrence
  existed) and `GetOccurrencesInRangeAsync` (same expansion, but a caller-supplied range instead of
  an internal window, so the admin calendar can page arbitrarily far back or forward).
- **Email is MailKit-backed, added from scratch for class booking and Contact Us -- nothing else
  sends email.** `IEmailSender`/`SmtpEmailSender` (`Infrastructure/Services/`) wrap MailKit;
  `SmtpOptions` binds SMTP host/port/credentials from the `Smtp:*` config section, wired the same way
  `ConnectionStrings:PotteryJournal` already is (`Smtp__*` env vars in `docker-compose.yml`, sourced
  from `.env`; `Smtp:*` user-secrets for local debugging -- see README). A failed send is caught and
  returned as a `HandlerResponse` error, never thrown -- email is an external I/O boundary that
  shouldn't crash whatever triggered it.
- **Settings vs. config: `AdminSettings` (a DB table) holds business-level values the studio owner
  edits from `/admin/settings`; `Smtp:*` (env-var config) holds deployment secrets a developer edits
  by redeploying.** `AdminSettings` has exactly two fields today --
  `NotificationRecipientEmail` (where class-booking and Contact-Us notifications go) and
  `MinimumBookingLeadDays` (how many days ahead a class must be booked) -- deliberately not a
  generic key-value settings system, since nothing else needs one yet.
  `AdminSettingsHandler.GetAsync()` creates the single row with defaults on first read if the table
  is empty, mirroring `EnsureBootstrapAdminAsync`'s idempotent-seed pattern.
- **Class booking is a two-step approval flow, and a booking is independent of the
  `ClassAvailability` rule that produced its slot.** A public submission
  (`ClassesHandler.CreateBookingAsync`) re-validates lead time, blackout periods, and party-size-vs-
  `ClassType.MaxCapacity` server-side (never trusts the client's already-filtered slot list), inserts
  as `ClassBookingStatus.Tentative`, and emails `AdminSettings.NotificationRecipientEmail` -- the
  customer gets nothing yet. An admin approving it from `/admin/classes/bookings`
  (`ApproveBookingAsync`) is what emails the customer a confirmation and flips it to `Confirmed`;
  declining frees the slot. A partial unique index on `(ClassTypeId, StartDateTime)` excluding
  `Declined` rows enforces "one active booking per class type per slot" at the DB level (a Wheel
  Throw and a Hand-Building booking can share a slot; two Wheel Throws can't) -- since `ClassBooking`
  has no FK to `ClassAvailability`, editing or deleting the availability rule that originally
  produced a slot never touches bookings already made against it.

## Editing Notes

- **Pottery Journal page** (`Pages/PotteryJournal.cshtml` + `wwwroot/js/pottery-journal.js`) is a
  near-verbatim port of the old static site's `index.html`/`app.js`: same DOM-building style (no
  `innerHTML` for data-driven content, hash-based routing via `#piece/<n>`), just fetching
  `/pottery-journal/data` instead of a static `pieces.json`. Keep that pattern if you touch it.
- **Admin repeatable rows** (piece notes, glaze applications on `Pages/Admin/Pieces/Edit.cshtml`)
  use a hide-and-clear-inputs pattern for "remove" (`wwwroot/js/admin-forms.js`), not DOM removal --
  removing a row would break the sequential `Piece.Notes[i]` index that ASP.NET Core's model
  binder requires. Blank rows are filtered server-side in `EditModel.OnPostSaveAsync`.
- **Piece and event photo deletion**: deleting a piece or event through the admin UI does not
  automatically delete its image files from disk unless the `IndexModel`/`EditModel` handler
  explicitly calls `IImageStorageService.Delete(...)` first -- check both `OnPostDeleteAsync`
  methods if you add new delete paths, or you'll orphan files on the uploads volume.
  `PieceHandler.DeleteAsync`/`EventsHandler.DeleteAsync` only remove the database rows.
- **Events have two independent photos**: `ImageFileName` (banner, shown inline on the card) and
  `FlyerImageFileName` (shown in a lightbox, not inline) -- separate uploads, separate
  `SetImageAsync`/`SetFlyerImageAsync` handler methods, both needing cleanup in
  `Admin/Events/Index.cshtml.cs`'s `OnPostDeleteAsync`. On the public card
  (`wwwroot/js/events.js`), the banner photo becomes the flyer's click target when both exist
  (`.event-card-photo-btn`); when only a flyer exists, a "View Flyer" button in `.event-actions`
  is the fallback trigger instead, since there's no photo to repurpose. The flyer lightbox
  (`#flyerLightbox`) reuses the same `.lightbox`/`.lightbox-figure`/`.lightbox-close` base as
  Gallery's photo viewer (shared in `site.css`; Gallery's paging arrows stay Gallery-only in
  `gallery.css`) but has no prev/next -- one flyer per event, no paging needed.
- **Local dev runtime mismatch**: this dev machine has only the .NET 10 SDK/runtime installed, not
  .NET 8. Projects target `net8.0` (per house style) with `<RollForward>LatestMajor</RollForward>`
  so `dotnet run`/`dotnet test` still work locally. Don't "fix" this by retargeting to `net10.0` --
  Docker uses the real `aspnet:8.0` image regardless of what's on this box.
- **`Login.cshtml`/`AccessDenied.cshtml` use `_AuthLayout.cshtml`, not `_AdminLayout.cshtml`.** They
  render for signed-out visitors, so they can't use the layout with the authenticated nav (sign-out
  form, Change Password link, admin email). This was a real bug caught during manual verification --
  the login page rendered *two* antiforgery-protected forms (its own + the nav's sign-out form),
  which is confusing UI and made scripting the login flow error-prone. Any other pre-auth page needs
  `_AuthLayout.cshtml` too.
- **Always pass both compose files together**: `docker compose -f docker-compose.yml -f
  docker-compose.dev.yml <command>`, never plain `docker compose <command>` in this repo. The dev
  overlay is what publishes Postgres to `localhost:55432` and bind-mounts `./uploads` -- running any
  command (`up`, `build`, etc.) with just the base file recalculates the `db`/`app` services from
  the base file alone and silently drops both, breaking a Visual-Studio-debugging session that's
  using them (`Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:55432`, or images 404ing).
  This has recurred multiple times from rebuilding the app image for verification with the base file
  only -- the container needing rebuilt is almost never a reason to drop the `-f` flags.
- **Every `.cshtml` with a code-behind `.cshtml.cs` must declare `@model`.** Omitting it (as
  `Classes.cshtml` did while being built) doesn't break the page visibly -- it still compiles, `OnGet`
  still renders correctly, and the form's antiforgery token still shows up -- but Razor Pages fails
  to associate the compiled view with its `PageModel` class for POST **handler dispatch**: every POST
  silently falls through to re-rendering the GET view (200, no handler code runs at all, no exception,
  no log line) instead of invoking `OnPost*Async`. This is essentially undetectable by inspection or
  by manually clicking through the page in a browser during dev (it looks like the form "did nothing"
  or "didn't save," easy to blame on the handler logic instead) -- it only surfaced here via an
  integration test asserting the POST's redirect status. Confirmed no other page with a code-behind
  in this repo is missing `@model` except `Gallery.cshtml` (GET-only, so harmless there, left alone).

## Testing

- `Web.Tests` uses `PotteryJournalWebApplicationFactory`, which swaps the Postgres `AppDbContext`
  for an EF Core in-memory database. No credential placeholders are needed anymore -- that was a
  Google-OAuth-era requirement (its remote auth handler validated options on every request) that
  no longer applies now that auth is internally managed.
- The data endpoints use a hand-rolled `JsonResult<T>` helper in `Program.cs` instead of
  `Results.Json(...)` -- `Results.Json`'s PipeWriter fast path throws under `WebApplicationFactory`'s
  TestServer (`PipeWriter.UnflushedBytes` not implemented). Keep using `JsonResult<T>` for any new
  data endpoint rather than reintroducing `Results.Json`.

## Deployment

Same homelab pattern as the old `Pottery` project (`docker/Dockerfile`, behind Nginx Proxy Manager
for TLS). See [README.md](README.md) for required environment variables --
`POTTERYJOURNAL_BOOTSTRAP_ADMIN_EMAIL`/`POTTERYJOURNAL_BOOTSTRAP_ADMIN_PASSWORD` seed the first
admin account on first startup when `AllowedAdmins` is empty.
