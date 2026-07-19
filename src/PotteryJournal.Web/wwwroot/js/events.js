const WEEKDAY_LABELS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

let EVENTS = [];
let ALL_EVENTS = [];
let calendarCursor = new Date(new Date().getFullYear(), new Date().getMonth(), 1);

const eventCardsEl = document.getElementById("eventCards");
const eventsEmptyState = document.getElementById("eventsEmptyState");
const calendarContainer = document.getElementById("calendarContainer");
const calendarGrid = document.getElementById("calendarGrid");
const calendarMonthLabel = document.getElementById("calendarMonthLabel");
const listViewBtn = document.getElementById("listViewBtn");
const calendarViewBtn = document.getElementById("calendarViewBtn");

const flyerLightbox = document.getElementById("flyerLightbox");
const flyerLightboxImage = document.getElementById("flyerLightboxImage");
const flyerLightboxClose = document.getElementById("flyerLightboxClose");
let flyerLastFocusedElement = null;

flyerLightboxClose.addEventListener("click", () => closeFlyerLightbox());
flyerLightbox.addEventListener("click", (e) => {
  if (e.target === flyerLightbox) {
    closeFlyerLightbox();
  }
});
document.addEventListener("keydown", (e) => {
  if (flyerLightbox.classList.contains("hidden")) {
    return;
  }
  if (e.key === "Escape") {
    closeFlyerLightbox();
  } else if (e.key === "Tab") {
    // Only the Close button is focusable inside this lightbox, so Tab/Shift+Tab both just keep
    // focus there instead of escaping into the page behind it.
    e.preventDefault();
    flyerLightboxClose.focus();
  }
});

init();

async function init() {
  const response = await fetch("/events/data");
  EVENTS = await response.json();

  const allResponse = await fetch("/events/data/all");
  ALL_EVENTS = await allResponse.json();

  renderCards();
  renderCalendar();

  listViewBtn.addEventListener("click", () => showView("list"));
  calendarViewBtn.addEventListener("click", () => showView("calendar"));
  document.getElementById("calendarPrev").addEventListener("click", () => shiftMonth(-1));
  document.getElementById("calendarNext").addEventListener("click", () => shiftMonth(1));
}

function showView(view) {
  const showCalendar = view === "calendar";
  calendarContainer.classList.toggle("hidden", !showCalendar);
  eventCardsEl.classList.toggle("hidden", showCalendar);
  eventsEmptyState.classList.toggle("hidden", showCalendar || EVENTS.length > 0);
  calendarViewBtn.classList.toggle("active", showCalendar);
  listViewBtn.classList.toggle("active", !showCalendar);
}

function openFlyerLightbox(fileName, title) {
  flyerLightboxImage.src = `/uploads/events/${fileName}`;
  flyerLightboxImage.alt = `${title} flyer`;

  if (!flyerLightbox.classList.contains("open")) {
    flyerLastFocusedElement = document.activeElement;
    flyerLightbox.classList.remove("hidden");
    void flyerLightbox.offsetWidth; // force a reflow so "hidden" -> visible commits before "open" triggers the transition
    flyerLightbox.classList.add("open");
    flyerLightboxClose.focus();
    setFlyerBackgroundInert(true);
  }
}

function closeFlyerLightbox() {
  if (!flyerLightbox.classList.contains("open")) {
    flyerLightbox.classList.add("hidden");
    return;
  }

  flyerLightbox.classList.remove("open");
  flyerLightbox.addEventListener(
    "transitionend",
    () => {
      // Only hide if it's still meant to be closed -- guards against a stale listener from this
      // close firing after a later reopen (see gallery.js's closeLightbox for the same fix).
      if (!flyerLightbox.classList.contains("open")) {
        flyerLightbox.classList.add("hidden");
      }
    },
    { once: true }
  );
  setFlyerBackgroundInert(false);

  if (flyerLastFocusedElement) {
    flyerLastFocusedElement.focus();
    flyerLastFocusedElement = null;
  }
}

function setFlyerBackgroundInert(isInert) {
  const targets = [
    document.querySelector(".site-nav"),
    ...Array.from(document.querySelector(".site-main").children).filter((el) => el !== flyerLightbox),
    document.querySelector(".site-footer"),
  ];
  targets.forEach((el) => {
    if (!el) {
      return;
    }
    if (isInert) {
      el.setAttribute("inert", "");
    } else {
      el.removeAttribute("inert");
    }
  });
}

function shiftMonth(delta) {
  calendarCursor = new Date(calendarCursor.getFullYear(), calendarCursor.getMonth() + delta, 1);
  renderCalendar();
}

function renderCards() {
  eventCardsEl.innerHTML = "";
  eventsEmptyState.classList.toggle("hidden", EVENTS.length > 0);

  EVENTS.forEach((evt) => {
    eventCardsEl.appendChild(buildCard(evt));
  });
}

// A recurring event's Id repeats across every expanded occurrence, so the DOM id / calendar-pill
// link has to be scoped to the specific occurrence (id + startDateTime), not just the series id --
// otherwise two occurrences of the same recurring event collide on the same "event-<id>" element id.
function occurrenceKey(evt) {
  return `${evt.id}-${evt.startDateTime.replace(/[^0-9]/g, "")}`;
}

function buildCard(evt) {
  const card = document.createElement("article");
  card.className = "event-card";
  card.id = `event-${occurrenceKey(evt)}`;

  if (evt.imageFileName) {
    // When a flyer also exists, the banner becomes the click target for it (same "click the
    // photo to see more" idiom as Gallery); otherwise it's a plain, non-interactive photo box.
    const photo = document.createElement(evt.flyerImageFileName ? "button" : "div");
    photo.className = evt.flyerImageFileName ? "event-card-photo event-card-photo-btn" : "event-card-photo";
    if (evt.flyerImageFileName) {
      photo.type = "button";
      photo.setAttribute("aria-label", `View flyer for ${evt.title}`);
      photo.addEventListener("click", () => openFlyerLightbox(evt.flyerImageFileName, evt.title));
    }
    const img = document.createElement("img");
    img.src = `/uploads/events/${evt.imageFileName}`;
    img.alt = evt.title;
    img.loading = "lazy";
    photo.appendChild(img);
    card.appendChild(photo);
  }

  const body = document.createElement("div");
  body.className = "event-card-body";

  const tag = document.createElement("span");
  tag.className = "event-tag";
  tag.textContent = "[Events]";
  body.appendChild(tag);

  const date = document.createElement("span");
  date.className = "event-date";
  date.textContent = formatDateRange(evt.startDateTime, evt.endDateTime);
  body.appendChild(date);

  const title = document.createElement("h3");
  title.textContent = evt.title;
  body.appendChild(title);

  if (evt.venueName || evt.venueAddress) {
    const venue = document.createElement("p");
    venue.className = "event-venue";
    if (evt.venueName) {
      venue.appendChild(document.createTextNode(evt.venueName));
    }
    if (evt.venueAddress) {
      if (evt.venueName) {
        venue.appendChild(document.createElement("br"));
      }
      const link = document.createElement("a");
      link.href = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(evt.venueAddress)}`;
      link.target = "_blank";
      link.rel = "noopener";
      link.textContent = evt.venueAddress;
      venue.appendChild(link);
    }
    body.appendChild(venue);
  }

  const description = document.createElement("p");
  description.className = "event-description";
  description.textContent = evt.description;
  body.appendChild(description);

  if (evt.socialMediaUrl) {
    const social = document.createElement("a");
    social.className = "hero-secondary-link";
    social.href = evt.socialMediaUrl;
    social.target = "_blank";
    social.rel = "noopener";
    social.textContent = "View on Social Media →";
    body.appendChild(social);
  }

  const actions = document.createElement("div");
  actions.className = "event-actions";

  const gcal = document.createElement("a");
  gcal.href = googleCalendarUrl(evt);
  gcal.target = "_blank";
  gcal.rel = "noopener";
  gcal.textContent = "Add to Google Calendar";
  actions.appendChild(gcal);

  const ics = document.createElement("a");
  ics.href = `/events/${evt.id}/ics`;
  ics.textContent = "Download .ics";
  actions.appendChild(ics);

  if (evt.externalLinkUrl) {
    const external = document.createElement("a");
    external.href = evt.externalLinkUrl;
    external.target = "_blank";
    external.rel = "noopener";
    external.textContent = "View Event →";
    actions.appendChild(external);
  }

  if (evt.flyerImageFileName && !evt.imageFileName) {
    // No banner to repurpose as the click target, so the flyer needs its own explicit trigger.
    const flyerBtn = document.createElement("button");
    flyerBtn.type = "button";
    flyerBtn.textContent = "View Flyer";
    flyerBtn.addEventListener("click", () => openFlyerLightbox(evt.flyerImageFileName, evt.title));
    actions.appendChild(flyerBtn);
  }

  body.appendChild(actions);
  card.appendChild(body);
  return card;
}

function formatDateRange(startIso, endIso) {
  const start = new Date(startIso);
  const startText = start.toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" });
  const timeText = start.toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" });
  if (!endIso) {
    return `${startText} · ${timeText}`;
  }
  const end = new Date(endIso);
  const endTimeText = end.toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" });
  return `${startText} · ${timeText} – ${endTimeText}`;
}

function toGCalDate(iso) {
  return new Date(iso).toISOString().replace(/[-:]|\.\d{3}/g, "");
}

function googleCalendarUrl(evt) {
  const start = toGCalDate(evt.startDateTime);
  const end = toGCalDate(evt.endDateTime || evt.startDateTime);
  const params = new URLSearchParams({
    action: "TEMPLATE",
    text: evt.title,
    dates: `${start}/${end}`,
    details: evt.description || "",
    location: evt.venueAddress || evt.venueName || "",
  });
  return `https://calendar.google.com/calendar/render?${params.toString()}`;
}

function renderCalendar() {
  calendarMonthLabel.textContent = calendarCursor.toLocaleDateString(undefined, { month: "long", year: "numeric" });
  calendarGrid.innerHTML = "";

  WEEKDAY_LABELS.forEach((label) => {
    const cell = document.createElement("div");
    cell.className = "calendar-weekday";
    cell.textContent = label;
    calendarGrid.appendChild(cell);
  });

  const firstOfMonth = new Date(calendarCursor.getFullYear(), calendarCursor.getMonth(), 1);
  const gridStart = new Date(firstOfMonth);
  gridStart.setDate(gridStart.getDate() - firstOfMonth.getDay());

  const eventsByDay = groupEventsByDay();

  for (let i = 0; i < 42; i++) {
    const day = new Date(gridStart);
    day.setDate(gridStart.getDate() + i);

    const cell = document.createElement("div");
    cell.className = "calendar-day";
    if (day.getMonth() !== calendarCursor.getMonth()) {
      cell.classList.add("outside-month");
    }

    const dayNumber = document.createElement("span");
    dayNumber.className = "calendar-day-number";
    dayNumber.textContent = String(day.getDate());
    cell.appendChild(dayNumber);

    const key = dayKey(day);
    (eventsByDay.get(key) || []).forEach((evt) => {
      const hasCard = EVENTS.some((e) => occurrenceKey(e) === occurrenceKey(evt));
      const pill = document.createElement(hasCard ? "a" : "span");
      pill.className = "calendar-event";
      pill.textContent = evt.title;
      if (hasCard) {
        pill.href = `#event-${occurrenceKey(evt)}`;
        pill.addEventListener("click", (e) => {
          e.preventDefault();
          showView("list");
          document.getElementById(`event-${occurrenceKey(evt)}`)?.scrollIntoView({ behavior: "smooth", block: "center" });
        });
      } else {
        pill.classList.add("calendar-event-past");
      }
      cell.appendChild(pill);
    });

    calendarGrid.appendChild(cell);
  }
}

function groupEventsByDay() {
  const map = new Map();
  ALL_EVENTS.forEach((evt) => {
    const key = dayKey(new Date(evt.startDateTime));
    if (!map.has(key)) {
      map.set(key, []);
    }
    map.get(key).push(evt);
  });
  return map;
}

function dayKey(date) {
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
}
