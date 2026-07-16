const DASH = "—";
const PIECE_IMAGE_BASE = "/uploads/pieces/";

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

init();

async function init() {
  const response = await fetch("/pottery-journal/data" + (categoryFilter ? `?category=${encodeURIComponent(categoryFilter)}` : ""));
  PIECES = await response.json();

  buildChips(clayChips, countBy(PIECES, (p) => [p.clay]), () => activeClay, (v) => (activeClay = v));
  buildChips(
    glazeChips,
    countBy(PIECES, (p) => p.glazeApplications.map((g) => g.glazeName)),
    () => activeGlaze,
    (v) => (activeGlaze = v)
  );

  refreshGallery();

  searchInput.addEventListener("input", refreshGallery);
  backBtn.addEventListener("click", () => {
    location.hash = "";
  });
  window.addEventListener("hashchange", route);

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

function buildChips(container, options, getActive, setActive) {
  container.innerHTML = "";
  const items = [{ value: null, label: "All" }].concat(
    options.map(({ value, count }) => ({
      value,
      label: `${value === DASH ? "Unspecified" : value} (${count})`,
    }))
  );

  items.forEach(({ value, label }) => {
    const chip = document.createElement("button");
    chip.type = "button";
    chip.className = "chip";
    chip.textContent = label;
    chip.addEventListener("click", () => {
      setActive(value);
      refreshGallery();
      paint();
    });
    container.appendChild(chip);
  });

  paint();

  function paint() {
    const active = getActive();
    [...container.children].forEach((chip, i) => {
      chip.classList.toggle("active", items[i].value === active);
    });
  }
}

function getFilteredPieces() {
  const q = searchInput.value.trim().toLowerCase();
  return PIECES.filter((p) => {
    if (activeClay !== null && p.clay !== activeClay) return false;
    if (activeGlaze !== null && !p.glazeApplications.some((g) => g.glazeName === activeGlaze)) return false;
    if (q && !(p.title.toLowerCase().includes(q) || p.clay.toLowerCase().includes(q))) return false;
    return true;
  });
}

function refreshGallery() {
  const filtered = getFilteredPieces();
  pieceCountEl.textContent = filtered.length;
  renderGallery(filtered);
}

function renderGallery(pieces) {
  galleryView.innerHTML = "";
  emptyState.classList.toggle("hidden", pieces.length > 0);

  const frag = document.createDocumentFragment();
  [...pieces].reverse().forEach((piece) => {
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
  photo.appendChild(img);

  const tag = document.createElement("span");
  tag.className = "tag";
  tag.textContent = `#${pad(piece.pieceNumber)} · ${monthYear(piece.startedDate)}`;
  photo.appendChild(tag);

  const title = document.createElement("span");
  title.className = "tile-title";
  title.textContent = piece.title;

  const sub = document.createElement("span");
  sub.className = "tile-sub";
  sub.textContent = piece.clay;

  btn.appendChild(photo);
  btn.appendChild(title);
  btn.appendChild(sub);
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
