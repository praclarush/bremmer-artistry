# Class Scheduling, Recurring Events, Blackouts, Admin Calendar, Contact Us

Implementation plan. Not yet built -- this document records the agreed design and phased rollout so
implementation can start from it in a future session.

## Context

The site currently has one-off Events only (no recurrence), no email-sending capability at all, and
no concept of bookable classes. The request bundles five related capabilities that all hinge on two
new pieces of shared infrastructure -- **recurrence** and **email** -- so the plan builds those first,
then layers the feature-specific pieces on top:

1. Public class booking (Wheel Throw / Hand-Building, 2-hour slots, admin-verified, confirmation
   email on approval, configurable minimum lead time).
2. Admin-managed blackout days/times that block class availability.
3. A unified admin calendar (weekly/monthly toggle) showing Events + booked classes.
4. Recurring Events (daily / weekly-every-X-weeks / monthly), reusing the same recurrence engine as
   classes.
5. A Contact Us page that emails the studio.

Clarified scope (from user Q&A):
- Class booking is public self-service, but a submission is **Tentative** until an admin approves it
  from the admin area; only then does the customer get a confirmation email. The initial submission
  sends a notification to the studio's configured address, not the customer.
- Capacity is **one active booking per (class type, time slot)** -- a Wheel Throw and a Hand-Building
  class can run in the same slot, but not two Wheel Throw bookings for the same slot. Each booking is
  a **group booking**: the customer states a party size (1-n people), validated against a
  **per-class-type max capacity** the admin sets (default 6). Since only one booking is ever allowed
  per slot, capacity checking is a single comparison (`PartySize <= ClassType.MaxCapacity`) at
  submission time -- no shared-capacity pool math across multiple bookings.
- Class bookings and Contact Us both notify the **same single configured address**.
- A new **admin-editable setting** controls the minimum number of days a booking must be made in
  advance.

Explicit scope cuts (stated here so they're a deliberate decision, not an oversight):
- No seat capacity / multi-person classes beyond the party-size-vs-max-capacity check above -- one
  booking per type per slot, per the above.
- No customer accounts, login, or payment -- booking is a request form, same trust model as the
  existing Contact-style flow.
- Blackouts apply to class availability only, not to Events (the request paired blackout with
  classes specifically).
- Deleting or editing a recurring Event/ClassAvailability rule acts on the whole series -- there is no
  per-occurrence edit/delete/detach in this pass (a reasonable v2 enhancement, not required now).

## Architecture decisions

**Recurrence -- dynamic expansion via Ical.Net, not materialized rows.** `Ical.Net` is already a
referenced package (used today for `.ics` generation). Both `Event` and the new `ClassAvailability`
get plain recurrence fields (`RecurrenceFrequency`, `RecurrenceInterval`, `RecurrenceEndDate`), and a
new shared `IRecurrenceExpander` (`src/PotteryJournal.Infrastructure/Services/RecurrenceExpander.cs`)
wraps Ical.Net's `RecurrencePattern`/`CalendarEvent.GetOccurrences(...)` to expand a rule into concrete
occurrence start times within a queried date range. No occurrence rows are ever persisted -- every
read (public listing, admin calendar, ICS export) expands on the fly, bounded by the query's date
range. This means deleting the parent row removes the whole series by construction (no orphaned
occurrence rows to clean up), and ICS export for a recurring Event can emit a real `RRULE` so external
calendar apps handle the recurrence natively.

Recurring-event expansion in `EventsHandler` needs an internal, generous-but-finite window so
indefinite recurrence (`RecurrenceEndDate == null`) can't generate unbounded occurrence lists: a
forward window for `GetUpcomingAsync` (public "what's coming up" card list) and a wider
past+forward window for the calendar's "all events" view. Non-recurring events are never
range-limited by this change -- that preserves the existing unbounded-history behavior of
`/events/data/all` exactly.

**Email -- MailKit, from scratch.** Confirmed via research: no email infrastructure exists anywhere in
this repo today (no package, no service, no config). Add the `MailKit` NuGet package to
`PotteryJournal.Infrastructure`, plus `IEmailSender`/`SmtpEmailSender`
(`Infrastructure/Services/EmailSender.cs`, alongside the existing `ImageStorageService.cs`). SMTP
connection details (`Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:FromAddress`,
`Smtp:FromName`, `Smtp:UseStartTls`) are deployment secrets, so they follow the existing
`Section__Key` env-var convention in `docker-compose.yml`/README, the same way
`ConnectionStrings__PotteryJournal` does today -- not admin-UI-editable.

**Settings -- new small `AdminSettings` table, not config.** `NotificationRecipientEmail` and
`MinimumBookingLeadDays` are business-level values the studio owner should be able to change without
a redeploy, so they get a genuinely new concept for this codebase: a single-row settings table with a
`IAdminSettingsHandler`/`AdminSettingsHandler` (`GetAsync`/`UpdateAsync`, upserting the one row) and an
admin page at `/admin/settings`. This is intentionally minimal -- two fields, not a generic key-value
settings system (nothing else in the app needs one yet).

**Business logic stays in Infrastructure.** Per `CLAUDE.md`'s "zero business logic in web projects"
rule, email-sending on booking submit / approval / contact-form-submit happens inside the handlers
(`ClassesHandler.CreateBookingAsync`, `ApproveBookingAsync`, and a new `ContactHandler.SubmitAsync`),
not in the Razor Page code-behind. Pages call the handler and render the result, matching the
`EventsHandler` pattern already established.

**Events data-endpoint split.** `GetAllAsync()` (unexpanded, series-level, one row per DB `Event`
row) stays exactly as-is -- it backs `Pages/Admin/Events/Index.cshtml.cs`'s CRUD list, where edit and
delete operate on the whole series, not an occurrence. A new `GetOccurrencesAsync()` returns expanded
per-occurrence view-models (same `Event.Id`, occurrence-specific `StartDateTime`/`EndDateTime`) and
becomes what the public `/events/data/all` minimal-API endpoint calls instead of `GetAllAsync()`.
`GetUpcomingAsync()` (backing `/events/data`) is updated in place to merge non-recurring upcoming
events with expanded recurring occurrences, keeping its existing signature and caller.

Because a recurring event can now produce multiple `EventModel` entries sharing one `Id`, the public
`events.js` calendar/card rendering needs its per-event DOM ids (`event-${id}`) to incorporate the
occurrence's `startDateTime` too, or a later occurrence's card silently collides with an earlier one's
`id` and `scrollIntoView`/anchor-link lookups break.

## Data model additions

Reusing exactly the entity/config/handler shape already in the codebase (`Category`/`CategoryConfiguration`
for lookups, `Event`/`EventConfiguration`/`EventsHandler` for dated CRUD with the
`ToUniversalTime()` DateTimeOffset normalization documented in `EventsHandler.ApplySaveModel`).

| Entity | Purpose | Key fields |
|---|---|---|
| `ClassType` | Lookup, extends the existing `Category`/`Glaze`/`ClayBody` pattern, plus a capacity | `Id`, `Name` (unique), `MaxCapacity` (int, default 6) -- seeded "Wheel Throw", "Hand-Building" |
| `RecurrenceFrequency` (enum) | Shared by `Event` and `ClassAvailability` | `None = 0`, `Daily`, `Weekly`, `Monthly` |
| `Event` (existing, extended) | Adds recurrence | + `RecurrenceFrequency`, `RecurrenceInterval` (int, default 1), `RecurrenceEndDate` (nullable -- null = recurs indefinitely, display always bounded by query range) |
| `ClassAvailability` | Admin-defined recurring or one-off bookable window per class type | `Id`, `ClassTypeId` (FK), `StartDateTime` (anchor occurrence), `RecurrenceFrequency`, `RecurrenceInterval`, `RecurrenceEndDate` (nullable). Duration is a fixed 2-hour constant, not a column. |
| `ClassBooking` | An actual customer request against a computed slot | `Id`, `ClassTypeId` (FK, Restrict), `StartDateTime`, `EndDateTime`, `CustomerName`, `CustomerEmail`, `CustomerPhone` (nullable), `PartySize` (int, 1-n), `Message` (nullable), `Status` (`ClassBookingStatus`: `Tentative = 0`, `Confirmed`, `Declined`), `CreatedDate`, `DecisionDate` (nullable). Partial unique index on `(ClassTypeId, StartDateTime)` excluding `Declined` rows enforces "one active booking per type per slot" at the DB level. |
| `BlackoutPeriod` | Admin-managed date/time ranges that block class availability | `Id`, `StartDateTime`, `EndDateTime`, `Reason` (nullable), `CreatedDate` |
| `AdminSettings` | Single-row studio settings | `Id`, `NotificationRecipientEmail`, `MinimumBookingLeadDays` (int, default 2) |

No FK from `ClassBooking` to `ClassAvailability` -- occurrences are virtual, so a booking simply
records the class type and the chosen start/end time independently. Editing or deleting the
`ClassAvailability` rule that originally produced a slot never touches existing bookings.

## Implementation phases

Each phase is an independently committable unit, matching the existing "commit after each discrete
piece" workflow.

**1. Shared recurrence + Event recurrence**
- No new package needed here; `Ical.Net` is already referenced.
- `RecurrenceFrequency` enum (`Infrastructure/Data/Entities/RecurrenceFrequency.cs`).
- `IRecurrenceExpander`/`RecurrenceExpander` (`Infrastructure/Services/RecurrenceExpander.cs`):
  `List<DateTimeOffset> Expand(DateTimeOffset anchorStart, RecurrenceFrequency frequency, int interval, DateTimeOffset? recurrenceEndDate, DateTimeOffset rangeStart, DateTimeOffset rangeEnd)`.
  `frequency == None` short-circuits to "the anchor occurrence itself, if it falls in range" without
  touching Ical.Net, so callers never need to special-case non-recurring items.
- Extend `Event` + `EventConfiguration` with the three recurrence columns; migration `AddEventRecurrence`.
- `EventsHandler`:
  - `GetAllAsync()` unchanged (admin CRUD list stays series-level).
  - `GetUpcomingAsync()` updated in place: non-recurring upcoming events unchanged; recurring events
    expanded via `IRecurrenceExpander` from now to an internal forward-window constant, merged in and
    re-sorted.
  - New `GetOccurrencesAsync()`: same idea but bounded by a wider past+forward window (an internal
    constant, generous enough that browsing the public calendar a few months either way never hits
    the edge), used by the new `/events/data/all` wiring.
  - `ApplySaveModel`/`ToModel` carry the three new fields through.
- `IIcsGenerator`/`IcsGenerator`: emit an `RRULE` (via Ical.Net `RecurrencePattern`, `Until =
  RecurrenceEndDate` when set) when the event recurs, so the single `.ics` download represents the
  whole series and lets the receiving calendar app expand it.
- `Program.cs`: DI-register `IRecurrenceExpander`; change the `/events/data/all` minimal-API endpoint
  to call `GetOccurrencesAsync()` instead of `GetAllAsync()`.
- Admin `Events/Edit.cshtml(.cs)`: add recurrence fields (Frequency dropdown, Interval number input,
  optional end-date picker). A small declarative `data-recurrence-toggle` addition to
  `admin-forms.js` (matching its existing `data-add-row`/`data-remove-row` declarative style) shows/
  hides the interval/end-date inputs based on the frequency select, using Bootstrap's `d-none` (the
  admin area is Bootstrap-based, unlike the public site).
- Public `Events.cshtml`/`events.js`: `buildCard`/calendar-pill DOM ids incorporate the occurrence's
  `startDateTime`, not just `evt.id` (see the "Events data-endpoint split" note above for why).
- Tests: extend `EventsHandlerTests` for recurrence expansion at each frequency + interval; verify
  `GetUpcomingAsync`/`GetOccurrencesAsync` respect `RecurrenceEndDate` and the internal window bounds.

**2. Email infrastructure**
- Add `MailKit` to `PotteryJournal.Infrastructure.csproj`.
- `SmtpOptions` (bound from `Smtp:*` config) + `IEmailSender`/`SmtpEmailSender`
  (`Infrastructure/Services/EmailSender.cs`) with one method:
  `Task<HandlerResponse> SendAsync(string toAddress, string subject, string body)`.
- DI registration in `Program.cs` (`services.Configure<SmtpOptions>(...)`,
  `AddScoped<IEmailSender, SmtpEmailSender>()`).
- Document new env vars (`Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`,
  `Smtp__FromAddress`, `Smtp__FromName`, `Smtp__UseStartTls`) in `docker-compose.yml`, `.env.example`,
  and `README.md`, following the existing `Section__Key` convention.
- No tests hit a real SMTP server; `SmtpEmailSender` is thin enough that a unit test just verifies
  error handling on a bad connection (`HandlerResponse.IsSuccess == false` with a captured error), not
  full send verification.

**3. AdminSettings**
- `AdminSettings` entity/config, migration `AddAdminSettings`.
- `IAdminSettingsHandler`/`AdminSettingsHandler`: `GetAsync()` (creates the row with defaults on first
  read if missing -- same idempotent-seed spirit as `EnsureBootstrapAdminAsync`), `UpdateAsync(model)`.
- `Pages/Admin/Settings/Index.cshtml(.cs)` -- a two-field form (recipient email, min lead days),
  following the `Admin/ReferenceData/Index` single-page-multiple-sections style.

**4. ClassType lookup**
- `ClassType` entity/config (`Name` + `MaxCapacity`), migration `AddClassTypeLookup`.
- Extend `IReferenceDataHandler`/`ReferenceDataHandler` with `GetClassTypesAsync`/
  `AddClassTypeAsync(name, maxCapacity)`/`RemoveClassTypeAsync`, reusing the existing private
  `AddLookupAsync`/`RemoveLookupAsync` generics for the name/remove plumbing. `MaxCapacity` is an
  extra constructor argument to the entity factory passed into `AddLookupAsync`. Because `ClassType`
  carries more than a bare name, also add `UpdateClassTypeCapacityAsync(Guid id, int maxCapacity)` --
  the existing lookup pattern is Add/Remove only, so this is a deliberate, minimal extension for this
  one lookup type rather than a generic "edit" added to the shared helpers.
- Seed "Wheel Throw" and "Hand-Building" (`MaxCapacity = 6`) via a new idempotent
  `EnsureSeedDataAsync`-style call from `Program.cs`'s existing startup scope block (next to
  `EnsureBootstrapAdminAsync`), since this repo has no EF `HasData` seeding precedent to follow
  instead.
- Add a fourth card to `Admin/ReferenceData/Index.cshtml` for class types, with a small capacity
  number input + save per row (not just Add/Remove, per the above).

**5. BlackoutPeriod + ClassAvailability + ClassBooking (the core class-scheduling feature)**
- Entities/configs, migrations `AddBlackoutPeriods`, `AddClassAvailability`, `AddClassBookings`
  (including the partial unique index).
- `IClassesHandler`/`ClassesHandler` (`Infrastructure/Handlers/`) covering the whole feature slice:
  - `GetBlackoutPeriodsAsync` / `AddBlackoutPeriodAsync` / `RemoveBlackoutPeriodAsync`
  - `GetAvailabilityRulesAsync` / `CreateAvailabilityRuleAsync` / `DeleteAvailabilityRuleAsync`
  - `GetAvailableSlotsAsync(DateTimeOffset from, DateTimeOffset to)` -- expands every
    `ClassAvailability` rule via `IRecurrenceExpander`, then filters out: slots inside a
    `BlackoutPeriod`, slots starting sooner than `AdminSettings.MinimumBookingLeadDays`, and slots
    with an existing non-`Declined` `ClassBooking` for that `(ClassTypeId, StartDateTime)`.
  - `CreateBookingAsync(model)` -- re-validates lead time/blackout/availability/`PartySize <=
    ClassType.MaxCapacity` server-side (never trust the client-side filtered list), inserts as
    `Tentative`, sends the studio notification email via `IEmailSender` (including party size so the
    studio knows group size at a glance).
  - `GetBookingsAsync(ClassBookingStatus? filter)`, `ApproveBookingAsync(id)` (sets `Confirmed`,
    sends the customer confirmation email), `DeclineBookingAsync(id)` (sets `Declined`, frees the
    slot).
- Admin pages:
  - `Pages/Admin/Classes/Blackouts/Index.cshtml(.cs)` -- list + inline add/remove, same shape as
    `Admin/ReferenceData/Index`.
  - `Pages/Admin/Classes/Availability/Index.cshtml(.cs)` + `Edit.cshtml(.cs)` -- same shape as
    `Admin/Events/Index`+`Edit`, with the recurrence fields from phase 1's Event UI reused verbatim.
  - `Pages/Admin/Classes/Bookings/Index.cshtml(.cs)` -- list filterable by status, showing party size
    alongside each booking, Approve/Decline action handlers.
- Public:
  - `Pages/Classes.cshtml` + `wwwroot/js/classes.js` + `wwwroot/css/classes.css` -- fetches
    `GET /classes/data` (new minimal-API endpoint in `Program.cs`, `RateLimiterPolicies.DataEndpoints`,
    same `JsonResult<T>` helper pattern as `/events/data`; each slot's payload includes the class
    type's `MaxCapacity` so the UI can show/enforce it) to render available slots grouped by class
    type; clicking a slot reveals a real `<form method="post">` (not AJAX, to keep Razor Pages'
    built-in antiforgery -- matches the `Login.cshtml` pattern) populated via JS with the chosen
    `ClassTypeId`/`StartDateTime`, plus a party-size number input (`min="1"`, `max` set to that class
    type's `MaxCapacity`), submitting to a `Contact.cshtml.cs`-style `OnPostBookAsync` handler. PRG
    redirect back to `/classes` with a `TempData` success message explaining the tentative-until-
    approved flow.
  - New `RateLimiterPolicies.ClassBooking` policy (mirrors `LoginAttempts`'s shape, ~5/min/IP).
- Tests: `ClassesHandlerTests` covering slot expansion + blackout/lead-time filtering + the
  one-active-booking-per-type-per-slot constraint + approve/decline email triggers (mock
  `IEmailSender`).

**6. Admin unified Calendar**
- `Pages/Admin/Calendar/Index.cshtml(.cs)` -- weekly/monthly toggle (reusing the existing public
  Events month-grid CSS/JS as the starting point for month view; a new week-view layout alongside it).
- `OnGetDataAsync(DateTimeOffset start, DateTimeOffset end)` page handler (protected automatically --
  everything under `/Admin` is covered by the existing `AuthorizeFolder("/Admin")` convention in
  `Program.cs`, no extra `[Authorize]` needed) returning combined JSON: expanded `Event` occurrences
  (via `IRecurrenceExpander`, all statuses/dates -- admin sees everything, not just "upcoming") +
  `ClassBooking` rows in range (Tentative and Confirmed visually distinguished).

**7. Contact Us**
- `IContactHandler`/`ContactHandler.SubmitAsync(name, email, message)` -- builds the notification
  email, sends to `AdminSettings.NotificationRecipientEmail` via `IEmailSender`.
- `Pages/Contact.cshtml` + `Contact.cshtml.cs` (`OnPostAsync`, PRG redirect + `TempData` success
  message, same shape as `Login.cshtml.cs` minus the auth).
- New `RateLimiterPolicies.ContactForm` policy.
- Add the nav entry to `Pages/Shared/_Layout.cshtml` (`ViewData["ActiveNav"] == "Contact"`), following
  the existing FAQ/About `<li>` pattern.

**8. Documentation**
- Update `CLAUDE.md` with an Architecture Notes entry covering: the recurrence-via-dynamic-expansion
  decision, the email infrastructure and its config keys, the settings-vs-config split, and the
  booking approval/email flow -- following the file's existing style of documenting *why*, not just
  *what*.
- Update `README.md` with the new `Smtp__*` env vars and `.env.example`.

## Verification

- `dotnet test` (NUnit3, in-memory `AppDbContext`) after each phase -- new `EventsHandlerTests`
  recurrence cases, new `ClassesHandlerTests`, `ContactHandlerTests`, `AdminSettingsHandlerTests`.
- `Web.Tests`: extend `DataEndpointsTests` for `/classes/data`; add a booking-flow test using
  `PotteryJournalWebApplicationFactory` (GET the Classes page for the antiforgery token, POST a
  booking, assert `Tentative` status and that a second identical POST for the same slot is rejected).
- Manual verification via `docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d`
  (both compose files, per `CLAUDE.md`) -- full restart after any migration, per the
  hot-reload-doesn't-always-apply-schema-changes lesson from this session:
  1. Admin: seed/verify ClassType lookups, set a blackout period, create a recurring Wheel Throw
     availability rule, create a one-off Hand-Building slot.
  2. Public `/classes`: confirm blackout dates and dates inside the lead-time window are excluded from
     the available list; submit a booking; confirm the studio notification email arrives (a local SMTP
     catcher like Mailpit/MailHog is worth adding to `docker-compose.dev.yml` for this, or point
     `Smtp:Host` at a real test inbox).
  3. Admin `Classes/Bookings`: approve the booking, confirm the customer receives the confirmation
     email; confirm the slot no longer appears on `/classes`.
  4. Admin `Calendar`: confirm the booked class and an existing/recurring Event both appear, toggle
     week/month view.
  5. `/contact`: submit the form, confirm the studio email arrives.
  6. Create a recurring Event (e.g. weekly), confirm it expands correctly on the public Events
     calendar and that its `.ics` download opens correctly in a calendar app with the recurrence
     intact.
