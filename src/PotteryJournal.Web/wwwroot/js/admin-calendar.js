// Admin calendar: week/month toggle, combining Event occurrences and non-declined class bookings
// fetched from this page's own OnGetDataAsync handler.

const WEEKDAY_LABELS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

let currentView = "month";
let cursorDate = new Date();

const gridEl = document.getElementById("adminCalendarGrid");
const rangeLabelEl = document.getElementById("calendarRangeLabel");
const viewWeekBtn = document.getElementById("viewWeekBtn");
const viewMonthBtn = document.getElementById("viewMonthBtn");

viewWeekBtn.addEventListener("click", () => setView("week"));
viewMonthBtn.addEventListener("click", () => setView("month"));
document.getElementById("calendarPrevBtn").addEventListener("click", () => shift(-1));
document.getElementById("calendarNextBtn").addEventListener("click", () => shift(1));
document.getElementById("calendarTodayBtn").addEventListener("click", () => {
  cursorDate = new Date();
  render();
});

setView("month");

function setView(view) {
  currentView = view;
  viewWeekBtn.classList.toggle("active", view === "week");
  viewMonthBtn.classList.toggle("active", view === "month");
  render();
}

function shift(delta) {
  if (currentView === "week") {
    cursorDate.setDate(cursorDate.getDate() + delta * 7);
  } else {
    cursorDate.setMonth(cursorDate.getMonth() + delta);
  }
  render();
}

async function render() {
  const { rangeStart, rangeEnd, days } = computeRange();
  rangeLabelEl.textContent = formatRangeLabel(rangeStart, rangeEnd);

  const params = new URLSearchParams({
    handler: "Data",
    start: rangeStart.toISOString(),
    end: rangeEnd.toISOString(),
  });
  const response = await fetch(`/admin/calendar?${params.toString()}`);
  const items = await response.json();

  renderGrid(days, items);
}

function computeRange() {
  if (currentView === "week") {
    const start = new Date(cursorDate);
    start.setHours(0, 0, 0, 0);
    start.setDate(start.getDate() - start.getDay());
    const days = [];
    for (let i = 0; i < 7; i++) {
      const day = new Date(start);
      day.setDate(start.getDate() + i);
      days.push(day);
    }
    const rangeEnd = new Date(start);
    rangeEnd.setDate(start.getDate() + 7);
    return { rangeStart: start, rangeEnd, days };
  }

  const firstOfMonth = new Date(cursorDate.getFullYear(), cursorDate.getMonth(), 1);
  const gridStart = new Date(firstOfMonth);
  gridStart.setHours(0, 0, 0, 0);
  gridStart.setDate(gridStart.getDate() - firstOfMonth.getDay());
  const days = [];
  for (let i = 0; i < 42; i++) {
    const day = new Date(gridStart);
    day.setDate(gridStart.getDate() + i);
    days.push(day);
  }
  const rangeEnd = new Date(gridStart);
  rangeEnd.setDate(gridStart.getDate() + 42);
  return { rangeStart: gridStart, rangeEnd, days };
}

function formatRangeLabel(start, end) {
  if (currentView === "week") {
    const lastDay = new Date(end.getTime() - 1);
    return `${start.toLocaleDateString(undefined, { month: "short", day: "numeric" })} – ${lastDay.toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" })}`;
  }
  return cursorDate.toLocaleDateString(undefined, { month: "long", year: "numeric" });
}

function renderGrid(days, items) {
  gridEl.innerHTML = "";
  gridEl.classList.toggle("admin-calendar-grid-week", currentView === "week");
  gridEl.classList.toggle("admin-calendar-grid-month", currentView === "month");

  WEEKDAY_LABELS.forEach((label) => {
    const cell = document.createElement("div");
    cell.className = "admin-calendar-weekday";
    cell.textContent = label;
    gridEl.appendChild(cell);
  });

  const itemsByDay = groupByDay(items);
  const currentMonth = cursorDate.getMonth();

  days.forEach((day) => {
    const cell = document.createElement("div");
    cell.className = "admin-calendar-day";
    if (currentView === "month" && day.getMonth() !== currentMonth) {
      cell.classList.add("outside-month");
    }

    const dayNumber = document.createElement("div");
    dayNumber.className = "admin-calendar-day-number";
    dayNumber.textContent = String(day.getDate());
    cell.appendChild(dayNumber);

    const key = dayKey(day);
    (itemsByDay.get(key) || []).forEach((item) => {
      const pill = document.createElement("div");
      pill.className = `admin-calendar-item admin-calendar-item-${badgeClass(item)}`;
      const time = new Date(item.startDateTime).toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" });
      pill.textContent = `${time} ${item.title}`;
      pill.title = item.title;
      cell.appendChild(pill);
    });

    gridEl.appendChild(cell);
  });
}

function badgeClass(item) {
  if (item.type === "Event") {
    return "event";
  }
  return item.status === "Confirmed" ? "confirmed" : "tentative";
}

function groupByDay(items) {
  const map = new Map();
  items.forEach((item) => {
    const key = dayKey(new Date(item.startDateTime));
    if (!map.has(key)) {
      map.set(key, []);
    }
    map.get(key).push(item);
  });
  return map;
}

function dayKey(date) {
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
}
