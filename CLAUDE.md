# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

## Repository Purpose

The Bremmer Artistry site -- ASP.NET Core (net8.0) + EF Core + Postgres. Successor to the static
[`Pottery`](../Pottery) project: same look/feel/interaction for the piece catalog, now data-driven
and expanded into a full site (Home, Gallery, Pottery Journal, Events, FAQ, About) with a
Google-OAuth-gated admin area. See [README.md](README.md) for the directory layout and setup.

## Architecture Notes

- **No separate API project.** `PotteryJournal.Web` hosts both the Razor Pages (public site +
  admin) and the plain JSON data endpoints (`/pottery-journal/data`, `/events/data`,
  `/events/{id}/ics`) the pages' own client-side scripts fetch. These are not a versioned/public
  API -- no Swagger, no API-key auth, no `Asp.Versioning`. Don't add that ceremony back in without
  a real external consumer driving the need.
- **No ASP.NET Core Identity.** Auth is plain cookie authentication + the Google OAuth challenge,
  checked in `Program.cs`'s `OnCreatingTicket` against the `AllowedAdmins` table via
  `IAllowedAdminsHandler`. `DefaultChallengeScheme` is deliberately left unset (falls back to the
  Cookie scheme) -- setting it to Google directly bypasses the `/Admin/Login` page and challenges
  Google on every `[Authorize]` failure site-wide. This was an actual bug caught by the
  `Web.Tests` integration tests; don't reintroduce it.
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

## Testing

- `Web.Tests` uses `PotteryJournalWebApplicationFactory`, which swaps the Postgres `AppDbContext`
  for an EF Core in-memory database and injects placeholder Google OAuth config -- ASP.NET Core's
  remote auth handler validates its options on every request (to check for its OAuth callback
  path), so even anonymous-endpoint tests need non-empty `ClientId`/`ClientSecret`.
- The data endpoints use a hand-rolled `JsonResult<T>` helper in `Program.cs` instead of
  `Results.Json(...)` -- `Results.Json`'s PipeWriter fast path throws under `WebApplicationFactory`'s
  TestServer (`PipeWriter.UnflushedBytes` not implemented). Keep using `JsonResult<T>` for any new
  data endpoint rather than reintroducing `Results.Json`.

## Deployment

Same homelab pattern as the old `Pottery` project (`docker/Dockerfile`, behind Nginx Proxy Manager
for TLS). See [README.md](README.md) for required environment variables and the Google OAuth
client setup steps -- both `GOOGLE_CLIENT_ID`/`GOOGLE_CLIENT_SECRET` and `BOOTSTRAP_ADMIN_EMAIL`
are required for the app to start serving any page, not just `/admin`.
