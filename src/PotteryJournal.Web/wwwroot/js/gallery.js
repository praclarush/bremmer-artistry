const PIECE_IMAGE_BASE = "/uploads/pieces/";
const PHOTO_SWAP_MS = 130;
const REDUCED_MOTION = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const STAGGER_CAP = 15;

let CATEGORIES = [];
let activeCategory = null;
let activeIndex = 0;
let lastFocusedElement = null;
let renderedCategoryName = null;
let pendingSwapTimeout = null;

const galleryIntro = document.getElementById("galleryIntro");
const categoriesView = document.getElementById("categoriesView");
const categoriesEmpty = document.getElementById("categoriesEmpty");
const categoriesLoading = document.getElementById("categoriesLoading");
const categoriesError = document.getElementById("categoriesError");
const categoriesRetryBtn = document.getElementById("categoriesRetryBtn");
const categoryView = document.getElementById("categoryView");
const categoryTitle = document.getElementById("categoryTitle");
const categoryGrid = document.getElementById("categoryGrid");
const categoryBackBtn = document.getElementById("categoryBackBtn");

const lightbox = document.getElementById("lightbox");
const lightboxImage = document.getElementById("lightboxImage");
const lightboxCaption = document.getElementById("lightboxCaption");
const lightboxClose = document.getElementById("lightboxClose");
const lightboxPrev = document.getElementById("lightboxPrev");
const lightboxNext = document.getElementById("lightboxNext");

categoryBackBtn.addEventListener("click", () => {
  location.hash = "";
});
lightboxClose.addEventListener("click", () => closeLightbox());
lightboxPrev.addEventListener("click", () => stepLightbox(-1));
lightboxNext.addEventListener("click", () => stepLightbox(1));
lightbox.addEventListener("click", (e) => {
  if (e.target === lightbox) {
    closeLightbox();
  }
});
document.addEventListener("keydown", onKeydown);
window.addEventListener("hashchange", route);
categoriesRetryBtn.addEventListener("click", () => init());

init();

async function init() {
  categoriesLoading.classList.remove("hidden");
  categoriesError.classList.add("hidden");
  try {
    const response = await fetch("/gallery/data");
    if (!response.ok) {
      throw new Error(`Unexpected response: ${response.status}`);
    }
    const pieces = await response.json();

    CATEGORIES = buildCategories(pieces);
    renderCategoryTiles();
    route();
  } catch (err) {
    categoriesError.classList.remove("hidden");
  } finally {
    categoriesLoading.classList.add("hidden");
  }
}

function buildCategories(pieces) {
  const map = new Map();
  pieces.forEach((piece) => {
    if (!map.has(piece.category)) {
      map.set(piece.category, []);
    }
    map.get(piece.category).push(piece);
  });

  return [...map.entries()]
    .map(([name, categoryPieces]) => ({
      name,
      pieces: categoryPieces,
      photos: categoryPieces.flatMap((piece) =>
        piece.imageFileNames.map((fileName) => ({
          pieceTitle: piece.title,
          url: imageUrl(fileName),
          startedDate: piece.startedDate,
        }))
      ),
    }))
    .sort((a, b) => a.name.localeCompare(b.name));
}

function renderCategoryTiles() {
  categoriesView.innerHTML = "";
  categoriesEmpty.classList.toggle("hidden", CATEGORIES.length > 0);

  const frag = document.createDocumentFragment();
  CATEGORIES.forEach((category, index) => {
    const a = document.createElement("a");
    a.className = "category-tile";
    a.style.setProperty("--i", Math.min(index, STAGGER_CAP));
    a.href = `#category/${encodeURIComponent(category.name)}`;

    const photo = document.createElement("div");
    photo.className = "category-photo";
    if (category.photos[0]) {
      const img = document.createElement("img");
      img.src = category.photos[0].url;
      img.loading = "lazy";
      img.alt = category.name;
      photo.appendChild(img);
    }

    const name = document.createElement("span");
    name.className = "category-name";
    name.textContent = category.name;

    const count = document.createElement("span");
    count.className = "category-count";
    count.textContent = `${category.pieces.length} piece${category.pieces.length === 1 ? "" : "s"}`;

    a.appendChild(photo);
    a.appendChild(name);
    a.appendChild(count);
    frag.appendChild(a);
  });
  categoriesView.appendChild(frag);
}

function renderCategoryGrid(category) {
  categoryTitle.textContent = category.name;
  categoryGrid.innerHTML = "";

  const frag = document.createDocumentFragment();
  category.photos.forEach((photo, index) => {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "tile";
    btn.style.setProperty("--i", Math.min(index, STAGGER_CAP));
    btn.setAttribute("aria-label", `View larger photo of ${photo.pieceTitle}`);
    btn.addEventListener("click", () => {
      location.hash = `#category/${encodeURIComponent(category.name)}/${index}`;
    });

    const photoDiv = document.createElement("div");
    photoDiv.className = "tile-photo";
    const img = document.createElement("img");
    img.src = photo.url;
    img.loading = "lazy";
    img.alt = photo.pieceTitle;
    photoDiv.appendChild(img);

    const tag = document.createElement("span");
    tag.className = "tag";
    tag.textContent = monthYear(photo.startedDate);
    photoDiv.appendChild(tag);

    const title = document.createElement("span");
    title.className = "tile-title";
    title.textContent = photo.pieceTitle;

    btn.appendChild(photoDiv);
    btn.appendChild(title);
    frag.appendChild(btn);
  });
  categoryGrid.appendChild(frag);
}

function imageUrl(fileName) {
  return PIECE_IMAGE_BASE + fileName;
}

function monthYear(isoDate) {
  return new Date(`${isoDate}T00:00:00`).toLocaleDateString(undefined, { month: "short", year: "numeric" });
}

function route() {
  const match = location.hash.match(/^#category\/([^/]+)(?:\/(\d+))?$/);
  if (!match) {
    closeLightbox({ skipHashClear: true });
    categoryView.classList.add("hidden");
    categoriesView.classList.remove("hidden");
    galleryIntro.classList.remove("hidden");
    categoriesEmpty.classList.toggle("hidden", CATEGORIES.length > 0);
    renderedCategoryName = null;
    window.scrollTo(0, 0);
    return;
  }

  const categoryName = decodeURIComponent(match[1]);
  const category = CATEGORIES.find((c) => c.name === categoryName);
  if (!category) {
    location.hash = "";
    return;
  }

  categoriesView.classList.add("hidden");
  galleryIntro.classList.add("hidden");
  categoriesEmpty.classList.add("hidden");
  categoryView.classList.remove("hidden");
  if (renderedCategoryName !== category.name) {
    renderCategoryGrid(category);
    renderedCategoryName = category.name;
  }

  const photoIndex = match[2] !== undefined ? Number(match[2]) : null;
  if (photoIndex !== null && category.photos[photoIndex]) {
    openLightbox(category, photoIndex);
  } else {
    closeLightbox({ skipHashClear: true });
    window.scrollTo(0, 0);
  }
}

function openLightbox(category, index) {
  const isFirstOpen = !lightbox.classList.contains("open");
  activeCategory = category;
  activeIndex = index;
  showLightboxPhoto(isFirstOpen);

  if (isFirstOpen) {
    lastFocusedElement = document.activeElement;
    lightbox.classList.remove("hidden");
    void lightbox.offsetWidth; // force a reflow so the "hidden" -> visible state is committed before "open" triggers the transition
    lightbox.classList.add("open");
    lightboxClose.focus();
    setBackgroundInert(true);
  }
}

function setBackgroundInert(isInert) {
  const targets = [
    document.querySelector(".site-nav"),
    ...Array.from(document.querySelector(".site-main").children).filter((el) => el !== lightbox),
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

function showLightboxPhoto(skipFade) {
  const photo = activeCategory.photos[activeIndex];
  lightboxCaption.textContent = `${photo.pieceTitle} — ${activeIndex + 1} of ${activeCategory.photos.length}`;

  if (pendingSwapTimeout) {
    window.clearTimeout(pendingSwapTimeout);
    pendingSwapTimeout = null;
  }

  if (skipFade || REDUCED_MOTION) {
    lightboxImage.alt = photo.pieceTitle;
    lightboxImage.src = photo.url;
    return;
  }

  // Briefly fade the current photo out before swapping src, so stepping between photos reads
  // as paging through the work rather than an instant pop. The lightbox's own open transition
  // already handles the first-appearance reveal, so this only runs for Next/Prev steps.
  lightboxImage.classList.add("is-swapping");
  pendingSwapTimeout = window.setTimeout(() => {
    lightboxImage.alt = photo.pieceTitle;
    lightboxImage.src = photo.url;
    lightboxImage.classList.remove("is-swapping");
    pendingSwapTimeout = null;
  }, PHOTO_SWAP_MS);
}

function stepLightbox(delta) {
  if (!activeCategory) {
    return;
  }
  const total = activeCategory.photos.length;
  const nextIndex = (activeIndex + delta + total) % total;
  location.hash = `#category/${encodeURIComponent(activeCategory.name)}/${nextIndex}`;
}

function closeLightbox(opts) {
  if (!lightbox.classList.contains("open")) {
    lightbox.classList.add("hidden");
    return;
  }

  lightbox.classList.remove("open");
  lightbox.addEventListener(
    "transitionend",
    () => {
      // Closing strips the photo index from the hash, which triggers a second route() pass
      // that calls closeLightbox() again -- if a photo is reopened before this listener fires,
      // only hide if the lightbox is still meant to be closed, not whatever got reopened since.
      if (!lightbox.classList.contains("open")) {
        lightbox.classList.add("hidden");
      }
    },
    { once: true }
  );
  activeCategory = null;
  setBackgroundInert(false);

  if (lastFocusedElement) {
    lastFocusedElement.focus();
    lastFocusedElement = null;
  }

  if (!opts || !opts.skipHashClear) {
    const match = location.hash.match(/^#category\/([^/]+)\/\d+$/);
    if (match) {
      location.hash = `#category/${match[1]}`;
    }
  }
}

function onKeydown(e) {
  if (lightbox.classList.contains("hidden")) {
    return;
  }
  if (e.key === "Escape") {
    closeLightbox();
  } else if (e.key === "ArrowLeft") {
    stepLightbox(-1);
  } else if (e.key === "ArrowRight") {
    stepLightbox(1);
  } else if (e.key === "Tab") {
    trapLightboxFocus(e);
  }
}

function trapLightboxFocus(e) {
  const focusable = [lightboxClose, lightboxPrev, lightboxNext];
  const currentIndex = focusable.indexOf(document.activeElement);
  const delta = e.shiftKey ? -1 : 1;
  const nextIndex = currentIndex === -1 ? 0 : (currentIndex + delta + focusable.length) % focusable.length;
  e.preventDefault();
  focusable[nextIndex].focus();
}
