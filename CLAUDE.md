# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

## Repository Purpose

The Bremmer Artistry site -- ASP.NET Core (net8.0) + EF Core + Postgres. Successor to the static
[`Pottery`](../Pottery) project: same look/feel/interaction for the piece catalog, now data-driven
and expanded into a full site (Home, Gallery, Pottery Journal, Events, Classes, FAQ, About,
Contact) with an internally-authenticated admin area. See [README.md](README.md) for the directory
layout and setup.

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
- **`Piece.ShowInJournal` (default true) curates the public Journal**, the same way
  `ShowInGallery` curates the Gallery: `GetAllDetailsAsync` filters on it alongside
  `GlazeApplications.Any()`, and the admin piece list badges hidden pieces. Its EF mapping is
  `.HasDefaultValue(true).HasSentinel(true)` plus an entity initializer `= true`. Without the
  sentinel, EF treats an explicit `false` as "unset" and lets the database default overwrite it,
  so unchecking the box silently does nothing. Keep all three if you add another default-true
  bool.
- **Gallery's camera-file-name fallback is display-only, in `gallery.js`.** The bulk import
  titled 88 pieces "<Category>, a dash, then a camera file name" (`20250924_110013`);
  `buildCategories` swaps any title matching `CAMERA_FILENAME_PATTERN` for
  "<Category>, <Month Year>" and hides the tile caption (the lightbox still shows it). The data
  is unchanged, so retitling a piece in the admin removes the fallback with no code change.
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
  global CSS override. The same script updates the "1 / N" position counter under the photo
  (`[data-featured-position]`), which Razor only renders when the collection has more than one
  piece.
- **Home is server-rendered end to end and ordered for a walk-up visitor: hero + featured
  collection photo -> upcoming events -> statement band -> Gallery teaser -> footer.** `IndexModel`
  makes three handler calls (`GetUpcomingAsync`, `GetFeaturedCollectionAsync`,
  `GetGalleryCategoriesAsync`) and `Index.cshtml` renders all of it -- no client-side fetch, unlike
  Gallery/Events/Classes/Journal. The next event renders as one wide `.event-feature` card and any
  later ones as `.event-ledger-row` rows (styles in `events.css`, Home-only); the Events page keeps
  its JS-built `.event-card` grid. The statement band and the hero paragraph are the original hero
  copy split in two, not new copy -- PRODUCT.md rules out invented proof or marketing filler, so
  don't pad Home with any.
- **`IPieceHandler.GetGalleryCategoriesAsync` and `gallery.js`'s `buildCategories` are two
  implementations of the same grouping and must agree.** The handler method groups
  `GetGalleryPiecesAsync`'s output into `GalleryCategoryModel` (name, piece count, cover photo) for
  Home's teaser; the Gallery page still fetches `/gallery/data` and groups client-side because it
  needs every photo for the lightbox. Both sort by name and both take the cover from the first
  photo of the newest piece in the group -- change one and the same category shows a different
  cover on Home than on `/gallery`. Home's tiles link to `/gallery#category/<name>`, the hash route
  `gallery.js` already handles. Home shows the first four (`IndexModel.TeaserCategoryCount`) in a
  fixed single row (`.category-grid--teaser`), so a short list never wraps into a stray half-row.
- **An event card's DOM id is `event-<id>-<start as Unix milliseconds>`, and that key is built in
  two places.** `events.js`'s `occurrenceKey` builds it from the JSON date
  (`new Date(evt.startDateTime).getTime()`); `Index.cshtml` builds the same key server-side
  (`StartDateTime.ToUnixTimeMilliseconds()`) for Home's event links. Unix milliseconds were chosen
  over the raw date string precisely so the two sides don't have to match a serializer's text
  format -- keep it numeric. The id alone isn't enough: a recurring event's `Id` repeats across
  every expanded occurrence. Because cards are built after the data fetch, the browser's own
  fragment scroll finds nothing at load time; `scrollToLinkedEvent` (run after `renderCards`, and
  on `hashchange`) scrolls to the linked card and marks it `.is-linked` (copper border) so it's
  identifiable in a row of several. `HomeTests` pins the server-side half of this contract.
- **`Event` recurrence is expanded dynamically on read, never persisted as occurrence rows.** `Event`
  carries `RecurrenceFrequency`/`RecurrenceInterval`/`RecurrenceEndDate` and is expanded by
  `IRecurrenceExpander`/`RecurrenceExpander` (`Infrastructure/Services/`), which wraps Ical.Net's
  `RecurrencePattern`/`CalendarEvent.GetOccurrences(...)` -- the same library already used for `.ics`
  export -- rather than a hand-rolled recurrence implementation. `RecurrenceFrequencyMapper` maps the
  local enum to Ical.Net's `FrequencyType` in one place, shared by both the expander and
  `IcsGenerator` (a recurring event's `.ics` download emits a real `RRULE`, not a flat single date).
  Because occurrences are virtual, deleting a recurring `Event` row always removes the whole series --
  there's no per-occurrence edit/delete, and no orphaned rows to clean up either way. `EventsHandler`
  exposes three shapes over the same data: `GetAllAsync` (series-level, unexpanded -- the admin CRUD
  list, since edit/delete act on the whole series) `GetUpcomingAsync` (expands recurring events
  forward, bounded by an internal window so indefinite recurrence can't generate unbounded lists;
  non-recurring events stay unbounded, unchanged from before recurrence existed) and
  `GetOccurrencesInRangeAsync` (same expansion, but a caller-supplied range instead of an internal
  window, so the admin calendar can page arbitrarily far back or forward). `ClassAvailability` does
  **not** use this engine or these fields -- see the class-scheduling note below, its recurrence
  shape is deliberately different (weekly day-of-week template, not an anchor-date/frequency rule).
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
- **`ClassAvailability` is a weekly day-of-week template, not a date-anchored recurrence rule, and
  can offer more than one block a day.** Each row is `ClassTypeId` + `DaysOfWeek` (a `[Flags]`
  bitmask of weekdays, e.g. Mon/Wed/Fri) + `StartTime` + `LastStartTime` + `IntervalHours` -- there's
  no anchor date and no end date. The rule recurs indefinitely; `ClassesHandler.ExpandWeeklyRule`
  walks every calendar day in the caller's query range and, on a matching weekday, emits an
  occurrence at `StartTime`, then every `IntervalHours` after that up to and including
  `LastStartTime` (11am start, every 2 hours, until 6pm -> 11, 1, 3, 5). When a class only runs once
  a day, `LastStartTime` simply equals `StartTime` (`CreateAvailabilityRuleAsync` forces this when
  the admin form's `RepeatsMultipleTimesPerDay` checkbox is unchecked, ignoring whatever was
  submitted in the now-hidden interval/last-time fields) and `IntervalHours` is irrelevant since the
  inner loop runs exactly once. `BlackoutPeriod` is the *only* mechanism for excluding a specific
  date or time range (a holiday, a one-off closure) -- it isn't specific to availability rules, so it
  also blocks any `ClassBooking` request that would otherwise land in that window. This intentionally
  does not reuse `IRecurrenceExpander`/`RecurrenceFrequency` (the `Event` engine above) -- classes
  only ever need "these weekdays, these times," never Ical.Net's fuller
  daily/every-N-weeks/monthly vocabulary, and forcing that engine's date-anchored shape onto a weekly
  template made "offer this class on Mon/Wed/Fri" require three separate rules instead of one. The
  admin checkbox toggle reuses `admin-forms.js`'s `[data-recurrence-toggle]` hook (originally built
  for Event's recurrence-frequency `<select>`), generalized to also collapse/expand a target based on
  a checkbox's `checked` state instead of only a select's value.
- **The public `/classes` page is a two-step picker, not one flat list; its date/time picker is a
  one-week-at-a-time strip, not a month grid; and picking a time opens the booking form in a modal,
  not inline on the page.** Step 1 (`#classTypeList`) shows every class type that currently has at
  least one bookable slot, derived client-side from `/classes/data` (no separate endpoint) --
  clicking a tile is a hash navigation to `#type/<classTypeId>`, mirroring `pottery-journal.js`'s
  `#piece/<n>` list/detail routing exactly (a `route()` function keyed off `location.hash`,
  `hashchange` listener, `window.scrollTo(0, 0)` on each transition). Step 2 (`#bookingView`) filters
  the same fetched slot list down to that one class type and renders it as seven day columns
  (`.class-week-day`, always Sun-Sat) with that day's available start times listed directly
  underneath each header (`renderClassWeek` in `classes.js`) -- no intermediate "pick a date, then
  see times" click, both are visible at once, closer to the reference booking UX (SimplyBook.it) than
  a full month view would be. `classWeekCursor` (a week-start `Date`, not a month) initializes to the
  week of that class type's earliest available slot, so opening a type whose next opening is weeks
  out doesn't land on a blank week the visitor has to page forward through; prev/next shift by 7 days.
  An unrecognized or now-empty `classTypeId` in the hash bounces back to the list
  (`location.hash = ""`), same graceful-degradation behavior as an unknown piece number in the
  Pottery Journal. The nav chrome (`.calendar-nav`/`.calendar-month-label`, reused verbatim from
  Events' month view via `site.css`) is the only piece shared with Events' calendar -- the day-grid
  itself is Classes-only (`.class-week-grid`/`.class-week-day` in `classes.css`) since Events has no
  week view to share it with. Clicking a time slot opens `#bookingModal` (built on the same
  `.lightbox` scrim/open/close CSS mechanics `site.css` already ships for Gallery's photo viewer and
  Events' flyer viewer) instead of revealing the form inline -- `openBookingModal`/`closeBookingModal`
  in `classes.js` mirror `events.js`'s `openFlyerLightbox`/`closeFlyerLightbox` pattern exactly
  (remove-hidden, forced reflow, add-open to trigger the CSS transition; `inert` on every other
  `.site-main` child plus the nav/footer while open; restore focus to the previously-focused element
  on close), with one deliberate deviation: focus lands on the Name field instead of the close button
  on open, since the modal's whole purpose is filling out the form, not dismissing it. Phone is a
  required field on this form (`bookingPhone` has `required`, matching the server-side check in
  `ClassesHandler.CreateBookingAsync` -- never trust client-only validation).
- **The public Classes section is behind `Site:ShowClasses` (`SiteFeaturesOptions`), off by
  default.** When false, `_Layout.cshtml` omits the nav/footer links, `Contact.cshtml` swaps its
  intro sentence, `ClassesModel.OnGet`/`OnPostBookAsync` return `NotFound()`, and the
  `/classes/data` endpoint returns 404. The admin area is deliberately not gated. A new public
  surface that mentions or links to classes must check the same flag, and
  `ClassesVisibilityTests` should get a case for it. `PotteryJournalWebApplicationFactory`
  defaults the flag to true so the existing Classes tests keep exercising the page; pass
  `showClasses: false` (or `null` for "unset") to test the hidden state. The hidden-POST test
  borrows an antiforgery token from `/contact`, because without one antiforgery returns 400
  before the handler's gate ever runs.
- **FAQ is gated the same way by `Site:ShowFaq` (off by default)**: `FaqModel.OnGet` returns
  `NotFound()` and `_Layout.cshtml` drops both links; `FaqVisibilityTests` covers it. The test
  factory has three constructors: `(db)` turns both flags on, `(db, showClasses)` leaves ShowFaq
  unset, and `(db, showClasses, showFaq)` sets both (`null` means "unset" for either).
- **"Today" is the studio's local day, set by `Site:TimeZone` (`StudioTimeOptions`, IANA id,
  default `America/Chicago`).** `EventsHandler.GetUpcomingAsync` keeps an event with no end time
  until the studio's local midnight, not UTC midnight; the UTC version dropped same-day morning
  events from Home and /events after 6 PM Central. The handler reads the clock from an injected
  `TimeProvider` (`TimeProvider.System` in `Program.cs`), so `EventsHandlerTests` pins a fixed
  instant with a mocked `TimeProvider`; keep any new day-boundary test on that clock instead of
  `DateTimeOffset.UtcNow`. An unknown zone id throws when the handler is first constructed.
- **Contact re-renders instead of redirecting when a send fails.** `ContactModel` returns
  `Page()` with `ErrorMessages`, so the visitor's input survives; only success redirects (with a
  TempData status). `ContactHandler` logs the raw sender error and returns a generic
  `SEND_FAILED_MESSAGE`, so SMTP details never reach the page.
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
- **`/admin/classes/bookings/create` ("Schedule a Class") is a third booking-creation path, for
  call-based/in-person scheduling, and it's a deliberate one-step alternative to the two-step public
  flow above, not a shortcut through it.** `CreateManualBookingAsync` and the public
  `CreateBookingAsync` both funnel through a shared `ValidateAndBuildBookingAsync(model,
  enforceMinimumLeadTime)` -- capacity, blackout periods, and the slot-conflict check always run for
  both; only `AdminSettings.MinimumBookingLeadDays` is conditional, skipped for the manual path since
  an admin scheduling a call-in booking *is* the deliberate override of that guardrail, not a bypass
  of the physical constraints under it. A manual booking is inserted as `Confirmed` directly (with
  `DecisionDate` set immediately) and emails the customer a confirmation right away
  (`SendBookingConfirmationEmailAsync`, the same helper `ApproveBookingAsync` now calls) -- there's no
  Tentative interim step and no studio notification email, since the admin creating it already knows
  about it. The date/time field is a free-form `datetime-local` input, not constrained to an existing
  `ClassAvailability` slot, so an admin can schedule a one-off session outside the normal recurring
  pattern when a caller needs something custom.

## Editing Notes

- **Page gutters come from `--page-x` (`site.css`), and it is not a plain clamp.** It's fluid up
  to 3rem, then grows so content never runs wider than `--page-max` (82rem); borders and rules
  stay full-bleed. Two consequences, both of which have already caused real bugs here: (1) never
  pair `padding: 0 var(--page-x)` with a bare `max-width` on the same box -- on a wide monitor the
  gutter alone exceeds a small max-width and crushes the content to nothing. Cap the measure on
  the children instead (`.content-page > *`), or use `calc(<measure> + 2 * var(--page-x))`
  (`.gallery-intro`). (2) A component that carries its own page gutter must drop it when nested in
  a view that already has one -- `.gallery` is top-level on the Pottery Journal but nested inside
  `.gallery-category-view` on Gallery, hence the override in `gallery.css`. Check any new layout
  at 1920px wide, not just 1440px, since neither failure shows below `--page-max`.
- **Only these font weights are loaded** (`_Layout.cshtml`): Fraunces 400-600, Inter 400/500/600,
  IBM Plex Mono 400/500/600. Asking for anything else (e.g. Inter 700) silently renders as
  browser-synthesized faux bold. Add the weight to the Google Fonts URL first if one is really
  needed.
- **Text-field borders use `--control-line`, not `--shelf-line`.** Shelf Line reads at 1.7:1
  against the page, too faint to show where a field is (WCAG 1.4.11 needs 3:1). Buttons and chips
  keep Shelf Line, since their text identifies them. See DESIGN.md "Control Line".
- **A tile's date tag must sit outside `.tile-photo-frame`.** The frame clips the hover zoom; the
  `.tag` is its sibling inside `.tile-photo` so it can hang past the photo's edge. Both
  `gallery.js` and `pottery-journal.js` build tiles through a `frameFor(img)` helper; putting the
  tag inside the frame, or `overflow: hidden` back on `.tile-photo`, clips it again.
- **The phone nav is progressive enhancement.** `site-nav.js` adds `.has-toggle` to `.site-nav`,
  and only that class lets `site.css` hide the links at 640px and below. Without the script the
  links wrap as they always did, so never hide `.site-nav-menu` without the class.
- **Every public page has exactly one `h1`.** Home's is the hero headline; Gallery, Events, and
  Classes put theirs in `.section-heading` (the same component Home uses with an `h2` for section
  titles -- `site.css` scales the two differently); the Journal's is in `.journal-header`; FAQ,
  About, and Contact use `.content-page h1`. All but Home's share one "page title" scale, see
  DESIGN.md.

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
- `HomeTests` covers Home's server-rendered sections (empty state, Gallery teaser tiles, the event
  feature/ledger split, and the event link's occurrence key). It seeds through `AppDbContext`
  resolved from `_factory.Services`, since Home has no JSON endpoint to assert against.
- **Checking a UI change visually when Docker/Postgres isn't running**: the real app can't start
  without a database, but the test host can render it. A throwaway `[Explicit]` test in
  `Web.Tests` can seed the in-memory database (real file names from `./uploads` make the photos
  resolve), `GetStringAsync` each page and data endpoint, and write the responses to a temp
  folder; any static server that maps those files plus `wwwroot/` and `uploads/` then gives a
  browsable copy with the real Razor output and the real CSS/JS. Delete the test afterward -- it's
  a verification aid, not a fixture. Seeded data won't match production content, so this checks
  layout and behavior, not copy.

## Deployment

`render.yaml` is the Render Blueprint (web service + Postgres + a disk at `/app/uploads`); see
README "Deploying to Render". Three things in it are load-bearing and not obvious from the file:
(1) Render's `fromDatabase` `connectionString` is a `postgresql://` URL, which Npgsql 8 rejects
(`ArgumentException`), so `Program.cs` passes the configured string through
`ConnectionStringNormalizer.Normalize` -- keep that call if you touch the `UseNpgsql` line, or the
Render deploy dies at startup while Docker/user-secrets setups (key/value strings, passed through
unchanged) keep working and hide the problem. (2) `ASPNETCORE_FORWARDEDHEADERS_ENABLED` is what
makes the app see HTTPS behind Render's proxy; there is no `UseForwardedHeaders` in code, so this
env var is the only thing doing it. (3) Plan IDs (`0.5c-512mb`, `0.1c-256mb`) are Render's
current naming; the old `starter`/`basic-256mb` names are rejected by its schema. Validate edits
against `https://render.com/schema/render.yaml.json` (any JSON-schema validator after parsing the
YAML) or `render blueprints validate render.yaml`. Note the Blueprint spec has no variable
interpolation, so don't try to build the connection string from parts.

Otherwise the same homelab pattern as the old `Pottery` project applies (`docker/Dockerfile`,
behind Nginx Proxy Manager for TLS). See [README.md](README.md) for required environment variables --
`POTTERYJOURNAL_BOOTSTRAP_ADMIN_EMAIL`/`POTTERYJOURNAL_BOOTSTRAP_ADMIN_PASSWORD` seed the first
admin account on first startup when `AllowedAdmins` is empty.
