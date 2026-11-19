const DASH = "—";
const PIECE_IMAGE_BASE = "/uploads/pieces/";
// 40+ glazes as one chip wall pushed the first pot below the fold. The most-used glazes stay
// visible (chips are sorted by count); the rest sit behind a "More glazes" toggle.
const GLAZE_CHIP_LIMIT = 8;
// A horizontal drag at least this long (px), and more sideways than vertical, pages the photos.
const SWIPE_MIN_PX = 50;

let PIECES = [];
let activeClay = null;
let activeGlaze = null;
const categoryFilter = new URLSearchParams(location.search).get("category");

const filterBar = document.querySelector(".filter-bar");
const galleryView = document.getElementById("galleryView");
const detailView = document.getElementById("detailView");
const worksheet = document.getElementById("worksheet");
const backBtn = document.getElementById("backBtn");
const searchInput = document.getElementById("searchInput");
const pieceCountEl = document.getElementById("pieceCount");
const emptyState = document.getElementById("emptyState");
const clayChips = document.getElementById("clayChips");
const glazeChips = document.getElementById("glazeChips");

const journalLoading = document.getElementById("journalLoading");
const journalError = document.getElementById("journalError");
const headerTools = document.getElementById("headerTools");
const filterToggle = document.getElementById("filterToggle");
const emptyStateText = document.getElementById("emptyStateText");
const clearFiltersBtn = document.getElementById("clearFiltersBtn");
const chipPainters = [];

searchInput.addEventListener("input", refreshGallery);
filterToggle.addEventListener("click", () => {
  const open = filterBar.classList.toggle("is-open");
  filterToggle.setAttribute("aria-expanded", String(open));
});
clearFiltersBtn.addEventListener("click", clearFilters);
backBtn.addEventListener("click", () => {
  location.hash = "";
});
window.addEventListener("hashchange", route);
document.getElementById("journalRetryBtn").addEventListener("click", () => init());

init();

// Same loading/error/retry treatment as gallery.js: without it, a failed fetch rendered as an
// empty shelf ("0 pieces"), indistinguishable from a journal with nothing in it.
async function init() {
  journalLoading.classList.remove("hidden");
  journalError.classList.add("hidden");
  try {
    const response = await fetch("/pottery-journal/data" + (categoryFilter ? `?category=${encodeURIComponent(categoryFilter)}` : ""));
    if (!response.ok) {
      throw new Error(`Unexpected response: ${response.status}`);
    }
    PIECES = await response.json();
  } catch (err) {
    journalError.classList.remove("hidden");
    return;
  } finally {
    journalLoading.classList.add("hidden");
  }

  chipPainters.length = 0;
  buildChips(clayChips, countBy(PIECES, (p) => [p.clay]), () => activeClay, (v) => (activeClay = v));
  buildChips(
    glazeChips,
    countBy(PIECES, (p) => p.glazeApplications.map((g) => g.glazeName)),
    () => activeGlaze,
    (v) => (activeGlaze = v),
    { limit: GLAZE_CHIP_LIMIT, noun: "glazes" }
  );

  refreshGallery();
  route();
}

function countBy(pieces, getValues) {
  const counts = new Map();
  pieces.forEach((p) => {
    getValues(p).forEach((value) => {
      counts.set(value, (counts.get(value) || 0) + 1);
    });
  });
  return [...counts.entries()]
    .sort((a, b) => b[1] - a[1])
    .map(([value, count]) => ({ value, count }));
}

// overflow: optional { limit, noun }. Chips past the limit are hidden behind a "More <noun>"
// toggle -- except the active one, which always stays visible so the current filter is never
// tucked out of sight.
function buildChips(container, options, getActive, setActive, overflow) {
  container.innerHTML = "";
  container.classList.remove("is-expanded");
  const items = [{ value: null, label: "All" }].concat(
    options.map(({ value, count }) => ({
      value,
      label: `${value === DASH ? "Unspecified" : value} (${count})`,
    }))
  );
  const chips = [];

  items.forEach(({ value, label }, i) => {
    const chip = document.createElement("button");
    chip.type = "button";
    chip.className = "chip";
    if (overflow && i > overflow.limit) {
      chip.classList.add("chip--overflow");
    }
    chip.textContent = label;
    chip.addEventListener("click", () => {
      setActive(value);
      refreshGallery();
      paint();
    });
    container.appendChild(chip);
    chips.push(chip);
  });

  const hiddenCount = overflow ? Math.max(0, items.length - 1 - overflow.limit) : 0;
  if (hiddenCount > 0) {
    const more = document.createElement("button");
    more.type = "button";
    more.className = "chip chip-more";
    more.setAttribute("aria-expanded", "false");
    more.textContent = `More ${overflow.noun} (${hiddenCount})`;
    more.addEventListener("click", () => {
      const expanded = container.classList.toggle("is-expanded");
      more.setAttribute("aria-expanded", String(expanded));
      more.textContent = expanded ? `Fewer ${overflow.noun}` : `More ${overflow.noun} (${hiddenCount})`;
    });
    container.appendChild(more);
  }

  chipPainters.push(paint);
  paint();

  function paint() {
    const active = getActive();
    chips.forEach((chip, i) => {
      const isActive = items[i].value === active;
      chip.classList.toggle("active", isActive);
      chip.setAttribute("aria-pressed", String(isActive));
    });
  }
}

function getFilteredPieces() {
  const q = searchInput.value.trim().toLowerCase();
  return PIECES.filter((p) => {
    if (activeClay !== null && p.clay !== activeClay) return false;
    if (activeGlaze !== null && !p.glazeApplications.some((g) => g.glazeName === activeGlaze)) return false;
    if (q && !searchText(p).includes(q)) return false;
    return true;
  });
}

// Search covers everything a visitor can see a chip or label for -- searching "waterfall" used to
// return nothing while the Waterfall glaze chip showed 71 pieces.
function searchText(piece) {
  return [piece.title, piece.clay, piece.category ?? "", ...piece.glazeApplications.map((g) => g.glazeName)]
    .join(" ")
    .toLowerCase();
}

function clearFilters() {
  activeClay = null;
  activeGlaze = null;
  searchInput.value = "";
  chipPainters.forEach((paint) => paint());
  refreshGallery();
}

function refreshGallery() {
  const filtered = getFilteredPieces();
  pieceCountEl.textContent = filtered.length;
  const activeFilterCount = (activeClay !== null ? 1 : 0) + (activeGlaze !== null ? 1 : 0);
  filterToggle.textContent = activeFilterCount > 0 ? `Filters (${activeFilterCount})` : "Filters";
  renderGallery(filtered);
}

function describeActiveFilters() {
  const parts = [];
  const q = searchInput.value.trim();
  if (q) parts.push(`“${q}”`);
  if (activeClay !== null) parts.push(activeClay === DASH ? "unspecified clay" : activeClay);
  if (activeGlaze !== null) parts.push(activeGlaze);
  return parts;
}

function renderGallery(pieces) {
  galleryView.innerHTML = "";
  emptyState.classList.toggle("hidden", pieces.length > 0);
  const activeFilters = describeActiveFilters();
  emptyStateText.textContent = activeFilters.length > 0
    ? `No pieces match ${activeFilters.join(" + ")}.`
    : "No pieces on the shelf yet.";
  clearFiltersBtn.classList.toggle("hidden", activeFilters.length === 0);

  const frag = document.createDocumentFragment();
  // /pottery-journal/data already arrives newest first (PieceHandler.GetAllDetailsAsync).
  pieces.forEach((piece) => {
    frag.appendChild(buildTile(piece));
  });
  galleryView.appendChild(frag);
}

function buildTile(piece) {
  const btn = document.createElement("button");
  btn.className = "tile";
  btn.setAttribute("aria-label", `Open worksheet for ${piece.title}`);
  btn.addEventListener("click", () => {
    location.hash = `#piece/${piece.pieceNumber}`;
  });

  const photo = document.createElement("div");
  photo.className = "tile-photo";
  const img = document.createElement("img");
  img.src = imageUrl(piece.images[0]);
  img.loading = "lazy";
  img.alt = piece.title;
  photo.appendChild(frameFor(img));

  const tag = document.createElement("span");
  tag.className = "tag";
  tag.textContent = `#${pad(piece.pieceNumber)} · ${monthYear(piece.startedDate)}`;
  photo.appendChild(tag);

  const title = document.createElement("span");
  title.className = "tile-title";
  title.textContent = piece.title;

  btn.appendChild(photo);
  btn.appendChild(title);
  // The server sends "—" for an unrecorded clay body; on a tile that's just a stray dash.
  if (piece.clay && piece.clay !== DASH) {
    const sub = document.createElement("span");
    sub.className = "tile-sub";
    sub.textContent = piece.clay;
    btn.appendChild(sub);
  }
  return btn;
}

function imageUrl(image) {
  if (!image) return "";
  return PIECE_IMAGE_BASE + (image.fileName || image);
}

function pad(n) {
  return String(n).padStart(3, "0");
}

function parseDate(isoDate) {
  return new Date(`${isoDate}T00:00:00`);
}

function monthYear(isoDate) {
  return parseDate(isoDate).toLocaleDateString(undefined, { month: "short", year: "numeric" });
}

function longDate(isoDate) {
  return parseDate(isoDate).toLocaleDateString(undefined, { month: "long", day: "numeric", year: "numeric" });
}

function route() {
  const match = location.hash.match(/^#piece\/(\d+)$/);
  if (!match) {
    detailView.classList.add("hidden");
    headerTools.classList.remove("hidden");
    filterBar.classList.remove("hidden");
    galleryView.classList.remove("hidden");
    emptyState.classList.toggle("hidden", galleryView.children.length > 0);
    window.scrollTo(0, 0);
    return;
  }
  const piece = PIECES.find((p) => p.pieceNumber === Number(match[1]));
  if (!piece) {
    location.hash = "";
    return;
  }
  // Search and count only act on the grid, so they leave with it instead of filtering a hidden list.
  headerTools.classList.add("hidden");
  filterBar.classList.add("hidden");
  galleryView.classList.add("hidden");
  emptyState.classList.add("hidden");
  detailView.classList.remove("hidden");
  renderWorksheet(piece);
  window.scrollTo(0, 0);
}

function renderWorksheet(piece) {
  worksheet.innerHTML = "";

  const fields = document.createElement("table");
  fields.className = "ws-fields";
  const rows = [
    ["PROJECT #", `#${pad(piece.pieceNumber)}`, "CLAY", piece.clay],
    ["PROJECT NAME", piece.title, "CATEGORY", piece.category || DASH],
    ["STARTED", longDate(piece.startedDate), "FINISHED", piece.finishedDate ? longDate(piece.finishedDate) : DASH],
    ["SIZE", piece.sizeText, "WEIGHT", piece.weightText],
    ["GLAZING", piece.glazeSummary, "ATTACHMENTS", piece.attachmentsText || DASH],
  ];
  rows.forEach(([l1, v1, l2, v2]) => {
    const tr = document.createElement("tr");
    tr.appendChild(cell("label", l1));
    tr.appendChild(cell("value", v1));
    tr.appendChild(cell("label", l2));
    tr.appendChild(cell("value", v2));
    fields.appendChild(tr);
  });
  worksheet.appendChild(fields);

  worksheet.appendChild(sectionTitle("SKETCH / PHOTO"));
  worksheet.appendChild(buildPhotoBox(piece.images, piece.title));

  worksheet.appendChild(sectionTitle("ADDITIONAL NOTES"));
  worksheet.appendChild(buildNotesBox(piece));

  worksheet.appendChild(sectionTitle("GLAZE APPLICATIONS"));
  worksheet.appendChild(buildGlazeBox(piece.glazeApplications));
}

function cell(cls, text) {
  const td = document.createElement("td");
  td.className = cls;
  td.textContent = text;
  return td;
}

function sectionTitle(text) {
  const div = document.createElement("div");
  div.className = "ws-section-title";
  div.textContent = text;
  return div;
}

function buildPhotoBox(images, alt) {
  const box = document.createElement("div");
  box.className = "ws-photo-box";
  box.innerHTML = `
    <span class="corner tl">/</span>
    <span class="corner tr">\\</span>
    <span class="corner bl">\\</span>
    <span class="corner br">/</span>
  `;

  let current = 0;
  const frame = document.createElement("div");
  frame.className = "ws-photo-frame";
  const img = document.createElement("img");
  img.src = images.length ? imageUrl(images[0]) : "";
  img.alt = alt;
  frame.appendChild(img);
  box.appendChild(frame);

  if (images.length > 1) {
    const prev = document.createElement("button");
    prev.className = "ws-photo-arrow prev";
    prev.setAttribute("aria-label", "Previous photo");
    prev.textContent = "‹";
    prev.addEventListener("click", () => show((current - 1 + images.length) % images.length));

    const next = document.createElement("button");
    next.className = "ws-photo-arrow next";
    next.setAttribute("aria-label", "Next photo");
    next.textContent = "›";
    next.addEventListener("click", () => show((current + 1) % images.length));

    const nav = document.createElement("div");
    nav.className = "ws-photo-nav";
    const dots = images.map((_, i) => {
      const dot = document.createElement("button");
      dot.setAttribute("aria-label", `Photo ${i + 1}`);
      dot.addEventListener("click", () => show(i));
      nav.appendChild(dot);
      return dot;
    });

    box.appendChild(prev);
    box.appendChild(next);
    box.appendChild(nav);
    onHorizontalSwipe(box, (direction) => show((current + direction + images.length) % images.length));

    function show(i) {
      current = i;
      img.src = imageUrl(images[current]);
      dots.forEach((d, di) => d.setAttribute("aria-current", di === current ? "true" : "false"));
    }
    show(0);
  }

  return box;
}

function buildNotesBox(piece) {
  const box = document.createElement("div");
  box.className = "ws-notes-box";
  if (!piece.notes.length) {
    box.classList.add("empty");
    box.textContent = "No notes recorded for this piece.";
    return box;
  }
  piece.notes.forEach((note) => {
    const p = document.createElement("p");
    p.style.margin = "0";
    if (note.title) {
      const strong = document.createElement("span");
      strong.className = "note-title";
      strong.textContent = note.title + ": ";
      p.appendChild(strong);
    }
    p.appendChild(document.createTextNode(note.noteText));
    box.appendChild(p);
  });
  return box;
}

function buildGlazeBox(glazeApplications) {
  const box = document.createElement("div");
  box.className = "ws-glaze-box";
  if (!glazeApplications.length) {
    box.classList.add("empty");
    box.textContent = "No glaze recorded.";
    return box;
  }
  glazeApplications.forEach((g) => {
    const row = document.createElement("div");
    row.className = "glaze-row";

    const loc = document.createElement("span");
    loc.className = "glaze-loc";
    loc.textContent = g.location;

    const name = document.createElement("span");
    name.className = "glaze-name";
    name.textContent = g.glazeName;

    const coats = document.createElement("span");
    coats.className = "glaze-coats";
    coats.textContent = `${g.coats}× coat${g.coats === 1 ? "" : "s"}`;

    row.appendChild(loc);
    row.appendChild(name);
    row.appendChild(coats);
    box.appendChild(row);
  });
  return box;
}

// Calls onSwipe(1) for a swipe left (next) and onSwipe(-1) for a swipe right (previous).
function onHorizontalSwipe(element, onSwipe) {
  let startX = 0;
  let startY = 0;
  element.addEventListener("touchstart", (e) => {
    startX = e.changedTouches[0].clientX;
    startY = e.changedTouches[0].clientY;
  }, { passive: true });
  element.addEventListener("touchend", (e) => {
    const dx = e.changedTouches[0].clientX - startX;
    const dy = e.changedTouches[0].clientY - startY;
    if (Math.abs(dx) >= SWIPE_MIN_PX && Math.abs(dx) > Math.abs(dy)) {
      onSwipe(dx < 0 ? 1 : -1);
    }
  }, { passive: true });
}

// Wraps a tile photo in the clipping frame: the frame crops the hover zoom, while the date tag
// (a sibling, outside the frame) is free to hang past the photo's edge.
function frameFor(img) {
  const frame = document.createElement("div");
  frame.className = "tile-photo-frame";
  frame.appendChild(img);
  return frame;
}
