const WEEKDAY_LABELS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

let SLOTS = [];
let TYPES = [];
let currentTypeSlots = [];
let classWeekCursor = startOfWeek(new Date());

const typeListView = document.getElementById("typeListView");
const classTypeList = document.getElementById("classTypeList");
const classesEmptyState = document.getElementById("classesEmptyState");

const bookingView = document.getElementById("bookingView");
const backToClassTypes = document.getElementById("backToClassTypes");
const bookingViewTitle = document.getElementById("bookingViewTitle");

const classWeekGrid = document.getElementById("classWeekGrid");
const classWeekLabel = document.getElementById("classWeekLabel");

const bookingModal = document.getElementById("bookingModal");
const bookingModalClose = document.getElementById("bookingModalClose");
const bookingForm = document.getElementById("bookingForm");
const bookingSubmit = document.getElementById("bookingSubmit");
const bookingSummary = document.getElementById("bookingSummary");
const classTypeIdInput = document.getElementById("bookingClassTypeId");
const startDateTimeInput = document.getElementById("bookingStartDateTime");
const partySizeInput = document.getElementById("bookingPartySize");
let bookingModalLastFocusedElement = null;

bookingModalClose.addEventListener("click", () => closeBookingModal());
bookingModal.addEventListener("click", (e) => {
  if (e.target === bookingModal) {
    closeBookingModal();
  }
});
document.addEventListener("keydown", (e) => {
  if (bookingModal.classList.contains("hidden") || e.key !== "Escape") {
    return;
  }
  closeBookingModal();
});
bookingForm.addEventListener("submit", () => {
  // The submit itself navigates the page away, so there's no need to re-enable the button on
  // success; a failed submission gets a fresh, fully re-rendered page from the server redirect.
  bookingSubmit.disabled = true;
  bookingSubmit.textContent = "Submitting…";
});

init();

async function init() {
  const response = await fetch("/classes/data");
  SLOTS = await response.json();
  TYPES = buildTypeList(SLOTS);

  renderTypeList();
  backToClassTypes.addEventListener("click", () => {
    location.hash = "";
  });
  document.getElementById("classWeekPrev").addEventListener("click", () => shiftClassWeek(-7));
  document.getElementById("classWeekNext").addEventListener("click", () => shiftClassWeek(7));
  window.addEventListener("hashchange", route);

  route();
}

function buildTypeList(slots) {
  const map = new Map();
  slots.forEach((slot) => {
    if (!map.has(slot.classTypeId)) {
      map.set(slot.classTypeId, {
        classTypeId: slot.classTypeId,
        classTypeName: slot.classTypeName,
        maxCapacity: slot.maxCapacity,
      });
    }
  });
  return [...map.values()];
}

function renderTypeList() {
  classTypeList.innerHTML = "";
  classesEmptyState.classList.toggle("hidden", TYPES.length > 0);

  TYPES.forEach((type) => {
    const tile = document.createElement("button");
    tile.type = "button";
    tile.className = "class-type-tile";

    const name = document.createElement("span");
    name.className = "class-type-name";
    name.textContent = type.classTypeName;
    tile.appendChild(name);

    const meta = document.createElement("span");
    meta.className = "class-type-meta";
    meta.textContent = `2-hour session · up to ${type.maxCapacity} people`;
    tile.appendChild(meta);

    const select = document.createElement("span");
    select.className = "class-type-select";
    select.textContent = "Select →";
    tile.appendChild(select);

    tile.addEventListener("click", () => {
      location.hash = `#type/${type.classTypeId}`;
    });

    classTypeList.appendChild(tile);
  });
}

function route() {
  const match = location.hash.match(/^#type\/([0-9a-fA-F-]{36})$/);
  if (!match) {
    bookingView.classList.add("hidden");
    typeListView.classList.remove("hidden");
    window.scrollTo(0, 0);
    return;
  }

  const classTypeId = match[1];
  const type = TYPES.find((t) => t.classTypeId === classTypeId);
  if (!type) {
    location.hash = "";
    return;
  }

  typeListView.classList.add("hidden");
  bookingView.classList.remove("hidden");
  bookingViewTitle.textContent = type.classTypeName;

  currentTypeSlots = SLOTS.filter((s) => s.classTypeId === classTypeId);
  classWeekCursor = firstAvailableWeek(currentTypeSlots);
  renderClassWeek();

  window.scrollTo(0, 0);
}

// Admin-entered class times are stored and returned as literal wall-clock digits with no real
// timezone conversion (see CLAUDE.md) -- the studio and its in-person visitors share one timezone,
// so there's nothing to convert. new Date(iso) parses those digits as UTC, and any local-time
// accessor (toLocaleTimeString, getHours, etc.) would then silently shift them by the visitor's
// browser offset. This rebuilds a Date whose *local* fields hold the original digits, so every
// existing local-time formatter downstream just works without further changes.
function slotWallClockDate(isoString) {
  const utc = new Date(isoString);
  return new Date(utc.getUTCFullYear(), utc.getUTCMonth(), utc.getUTCDate(), utc.getUTCHours(), utc.getUTCMinutes(), utc.getUTCSeconds());
}

function firstAvailableWeek(slots) {
  if (slots.length === 0) {
    return startOfWeek(new Date());
  }
  const earliest = slots.reduce((min, slot) => {
    const start = slotWallClockDate(slot.startDateTime);
    return start < min ? start : min;
  }, slotWallClockDate(slots[0].startDateTime));
  return startOfWeek(earliest);
}

function startOfWeek(date) {
  const start = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  start.setDate(start.getDate() - start.getDay());
  return start;
}

function shiftClassWeek(days) {
  classWeekCursor = new Date(classWeekCursor.getFullYear(), classWeekCursor.getMonth(), classWeekCursor.getDate() + days);
  renderClassWeek();
}

function renderClassWeek() {
  const weekEnd = new Date(classWeekCursor.getFullYear(), classWeekCursor.getMonth(), classWeekCursor.getDate() + 6);
  classWeekLabel.textContent = formatWeekRange(classWeekCursor, weekEnd);

  classWeekGrid.innerHTML = "";
  const slotsByDay = groupSlotsByDay(currentTypeSlots);
  const todayKey = dayKey(new Date());

  for (let i = 0; i < 7; i++) {
    const day = new Date(classWeekCursor.getFullYear(), classWeekCursor.getMonth(), classWeekCursor.getDate() + i);
    const key = dayKey(day);
    const daySlots = (slotsByDay.get(key) || []).slice().sort((a, b) => slotWallClockDate(a.startDateTime) - slotWallClockDate(b.startDateTime));

    const column = document.createElement("div");
    column.className = "class-week-day";
    if (key === todayKey) {
      column.classList.add("is-today");
    }

    const header = document.createElement("div");
    header.className = "class-week-day-header";

    const dayName = document.createElement("span");
    dayName.className = "class-week-day-name";
    dayName.textContent = WEEKDAY_LABELS[day.getDay()];
    header.appendChild(dayName);

    const dayDate = document.createElement("span");
    dayDate.className = "class-week-day-date";
    dayDate.textContent = day.toLocaleDateString(undefined, { month: "short", day: "numeric" });
    header.appendChild(dayDate);

    column.appendChild(header);

    const times = document.createElement("div");
    times.className = "class-week-day-times";

    if (daySlots.length === 0) {
      const empty = document.createElement("span");
      empty.className = "class-week-day-empty";
      empty.textContent = "—";
      times.appendChild(empty);
    } else {
      daySlots.forEach((slot) => {
        const start = slotWallClockDate(slot.startDateTime);
        const button = document.createElement("button");
        button.type = "button";
        button.className = "class-time-btn";
        button.textContent = formatTimeLabel(start);
        button.addEventListener("click", () => selectSlot(slot, button));
        times.appendChild(button);
      });
    }

    column.appendChild(times);
    classWeekGrid.appendChild(column);
  }
}

function formatWeekRange(start, end) {
  const startText = start.toLocaleDateString(undefined, { month: "short", day: "numeric" });
  const endText = end.toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" });
  return `${startText} – ${endText}`;
}

function groupSlotsByDay(slots) {
  const map = new Map();
  slots.forEach((slot) => {
    const key = dayKey(slotWallClockDate(slot.startDateTime));
    if (!map.has(key)) {
      map.set(key, []);
    }
    map.get(key).push(slot);
  });
  return map;
}

function dayKey(date) {
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
}

function formatDateLabel(start) {
  return start.toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric" });
}

function formatTimeLabel(start) {
  return start.toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" });
}

function formatSlot(slot) {
  const start = slotWallClockDate(slot.startDateTime);
  return `${formatDateLabel(start)} · ${formatTimeLabel(start)}`;
}

function selectSlot(slot, button) {
  document.querySelectorAll(".class-time-btn.selected").forEach((el) => el.classList.remove("selected"));
  button.classList.add("selected");

  classTypeIdInput.value = slot.classTypeId;
  startDateTimeInput.value = slot.startDateTime;
  partySizeInput.max = String(slot.maxCapacity);
  if (Number(partySizeInput.value) > slot.maxCapacity) {
    partySizeInput.value = String(slot.maxCapacity);
  }

  bookingSummary.textContent = `${slot.classTypeName} — ${formatSlot(slot)} (up to ${slot.maxCapacity} people)`;
  openBookingModal();
}

function openBookingModal() {
  if (bookingModal.classList.contains("open")) {
    return;
  }
  bookingModalLastFocusedElement = document.activeElement;
  bookingModal.classList.remove("hidden");
  void bookingModal.offsetWidth; // force a reflow so "hidden" -> visible commits before "open" triggers the transition
  bookingModal.classList.add("open");
  document.getElementById("bookingName").focus();
  setBookingModalBackgroundInert(true);
}

function closeBookingModal() {
  if (!bookingModal.classList.contains("open")) {
    bookingModal.classList.add("hidden");
    return;
  }

  bookingModal.classList.remove("open");
  const finishClose = () => {
    // Only hide if it's still meant to be closed -- guards against a stale call from this close
    // firing after a later reopen (see gallery.js's closeLightbox for the same fix).
    if (!bookingModal.classList.contains("open")) {
      bookingModal.classList.add("hidden");
    }
  };
  // transitionend is the primary signal, but it isn't guaranteed to fire (an interrupted
  // transition, a backgrounded tab, or a reduced-motion edge case can all skip it) -- without a
  // fallback, a missed event leaves the modal as an invisible, click-swallowing overlay forever.
  bookingModal.addEventListener("transitionend", finishClose, { once: true });
  setTimeout(finishClose, 250);
  setBookingModalBackgroundInert(false);

  if (bookingModalLastFocusedElement) {
    bookingModalLastFocusedElement.focus();
    bookingModalLastFocusedElement = null;
  }
}

function setBookingModalBackgroundInert(isInert) {
  const targets = [
    document.querySelector(".site-nav"),
    ...Array.from(document.querySelector(".site-main").children).filter((el) => el !== bookingModal),
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
