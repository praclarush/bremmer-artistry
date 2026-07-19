let SLOTS = [];

const slotsContainer = document.getElementById("classSlots");
const emptyState = document.getElementById("classesEmptyState");
const bookingForm = document.getElementById("bookingForm");
const bookingSummary = document.getElementById("bookingSummary");
const classTypeIdInput = document.getElementById("bookingClassTypeId");
const startDateTimeInput = document.getElementById("bookingStartDateTime");
const partySizeInput = document.getElementById("bookingPartySize");

init();

async function init() {
  const response = await fetch("/classes/data");
  SLOTS = await response.json();
  renderSlots();
}

function renderSlots() {
  slotsContainer.innerHTML = "";
  emptyState.classList.toggle("hidden", SLOTS.length > 0);

  const groups = groupByClassType(SLOTS);
  groups.forEach((slots, classTypeName) => {
    const group = document.createElement("div");
    group.className = "class-slot-group";

    const heading = document.createElement("h3");
    heading.textContent = classTypeName;
    group.appendChild(heading);

    const grid = document.createElement("div");
    grid.className = "class-slot-grid";
    slots.forEach((slot) => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "class-slot-btn";
      button.textContent = formatSlot(slot);
      button.addEventListener("click", () => selectSlot(slot, button));
      grid.appendChild(button);
    });
    group.appendChild(grid);

    slotsContainer.appendChild(group);
  });
}

function groupByClassType(slots) {
  const map = new Map();
  slots.forEach((slot) => {
    if (!map.has(slot.classTypeName)) {
      map.set(slot.classTypeName, []);
    }
    map.get(slot.classTypeName).push(slot);
  });
  return map;
}

function formatSlot(slot) {
  const start = new Date(slot.startDateTime);
  const dateText = start.toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric" });
  const timeText = start.toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" });
  return `${dateText} · ${timeText}`;
}

function selectSlot(slot, button) {
  document.querySelectorAll(".class-slot-btn.selected").forEach((el) => el.classList.remove("selected"));
  button.classList.add("selected");

  classTypeIdInput.value = slot.classTypeId;
  startDateTimeInput.value = slot.startDateTime;
  partySizeInput.max = String(slot.maxCapacity);
  if (Number(partySizeInput.value) > slot.maxCapacity) {
    partySizeInput.value = String(slot.maxCapacity);
  }

  bookingSummary.textContent = `${slot.classTypeName} — ${formatSlot(slot)} (up to ${slot.maxCapacity} people)`;
  bookingForm.classList.remove("hidden");
  bookingForm.scrollIntoView({ behavior: "smooth", block: "start" });
}
