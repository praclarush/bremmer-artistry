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
