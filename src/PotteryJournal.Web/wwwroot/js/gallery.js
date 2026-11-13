const PIECE_IMAGE_BASE = "/uploads/pieces/";
const PHOTO_SWAP_MS = 130;
const REDUCED_MOTION = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const STAGGER_CAP = 15;
// Chrome/Edge/Safari (no Firefox yet, per caniuse) get a shared-element morph: the tile a visitor
// clicked grows directly into the lightbox photo, and paging prev/next crossfades between photos
// natively instead of the manual opacity-timeout swap below. Everything falls back to today's
// existing behavior when unsupported or under reduced motion -- see PHOTO_MORPH_NAME's static CSS
// rule in gallery.css and its `@media (prefers-reduced-motion: reduce)` kill-switch.
const VIEW_TRANSITIONS_SUPPORTED = typeof document.startViewTransition === "function";
const USE_PHOTO_MORPH = VIEW_TRANSITIONS_SUPPORTED && !REDUCED_MOTION;
const PHOTO_MORPH_NAME = "gallery-lightbox-photo";
// Bulk-imported pieces were titled "<Category> — <camera file name>" (e.g. "Bowls — 20250924_110013").
// A title like that is a camera roll, not a name, so it gets replaced on display with the
// category and month. Retitling the piece in the admin removes the fallback with no code change.
const CAMERA_FILENAME_PATTERN = /\d{8}[_-]\d{6}/;
// A horizontal drag at least this long (px), and more sideways than vertical, pages the lightbox.
const SWIPE_MIN_PX = 50;

let CATEGORIES = [];
let activeCategory = null;
let activeIndex = 0;
let lastFocusedElement = null;
let renderedCategoryName = null;
let pendingSwapTimeout = null;
// True when this page pushed the lightbox's history entry (a tile click), so closing can step back
// to the grid's entry instead of stacking another one. Paging photos replaces the entry in place:
// Back from the lightbox leaves it, rather than replaying every photo viewed.
let lightboxPushedHistory = false;

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
onHorizontalSwipe(lightbox, (direction) => stepLightbox(direction));
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
        piece.imageFileNames.map((fileName) => {
          const hasRealTitle = !CAMERA_FILENAME_PATTERN.test(piece.title);
          return {
            pieceTitle: hasRealTitle ? piece.title : `${name}, ${monthYear(piece.startedDate)}`,
            hasRealTitle,
            url: imageUrl(fileName),
            startedDate: piece.startedDate,
          };
        })
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
      lightboxPushedHistory = true;
      location.hash = `#category/${encodeURIComponent(category.name)}/${index}`;
    });

    const photoDiv = document.createElement("div");
    photoDiv.className = "tile-photo";
    const img = document.createElement("img");
    img.src = photo.url;
    img.loading = "lazy";
    img.alt = photo.pieceTitle;
    photoDiv.appendChild(frameFor(img));

    const tag = document.createElement("span");
    tag.className = "tag";
    tag.textContent = monthYear(photo.startedDate);
    photoDiv.appendChild(tag);

    btn.appendChild(photoDiv);
    // A fallback title would only repeat the grid heading and the date tag, so it stays out of
    // the visible tile (it still labels the button and image for screen readers).
    if (photo.hasRealTitle) {
      const title = document.createElement("span");
      title.className = "tile-title";
      title.textContent = photo.pieceTitle;
      btn.appendChild(title);
    }
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

  if (!isFirstOpen) {
    showLightboxPhoto(false);
    return;
  }

  if (USE_PHOTO_MORPH) {
    const sourceImg = categoryGrid.children[index]?.querySelector(".tile-photo img");
    if (sourceImg) {
      sourceImg.style.viewTransitionName = PHOTO_MORPH_NAME;
    }
    const transition = document.startViewTransition(() => {
      showLightboxPhoto(true);
      lastFocusedElement = document.activeElement;
      lightbox.classList.remove("hidden");
      lightbox.classList.add("open");
      setBackgroundInert(true);
      // Clear the tile's name before the "after" snapshot is captured -- #lightboxImage carries
      // this name permanently (see gallery.css), so leaving it on both elements at once would be
      // a duplicate view-transition-name, which throws.
      if (sourceImg) {
        sourceImg.style.viewTransitionName = "";
      }
    });
    transition.finished.finally(() => lightboxClose.focus());
    return;
  }

  showLightboxPhoto(true);
  lastFocusedElement = document.activeElement;
  lightbox.classList.remove("hidden");
  void lightbox.offsetWidth; // force a reflow so the "hidden" -> visible state is committed before "open" triggers the transition
  lightbox.classList.add("open");
  lightboxClose.focus();
  setBackgroundInert(true);
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
  lightboxCaption.textContent = `${photo.pieceTitle} · ${activeIndex + 1} of ${activeCategory.photos.length}`;

  if (pendingSwapTimeout) {
    window.clearTimeout(pendingSwapTimeout);
    pendingSwapTimeout = null;
  }

  const applyImage = () => {
    lightboxImage.alt = photo.pieceTitle;
    lightboxImage.src = photo.url;
  };

  if (skipFade || REDUCED_MOTION) {
    applyImage();
    return;
  }

  if (USE_PHOTO_MORPH) {
    // #lightboxImage carries PHOTO_MORPH_NAME permanently (gallery.css) -- the same element
    // persisting across the transition with only its src changing gets a native crossfade
    // between the old and new photo, no manual opacity timing needed.
    // Paging faster than the crossfade skips the previous transition, which rejects its `ready`
    // promise. That's expected (the newer photo wins), so it shouldn't surface as a console error.
    document.startViewTransition(applyImage).ready.catch(() => {});
    return;
  }

  // Fallback for browsers without View Transitions: briefly fade the current photo out before
  // swapping src, so stepping between photos still reads as paging rather than an instant pop.
  lightboxImage.classList.add("is-swapping");
  pendingSwapTimeout = window.setTimeout(() => {
    applyImage();
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
  // replaceState doesn't fire hashchange, so route explicitly.
  history.replaceState(null, "", `#category/${encodeURIComponent(activeCategory.name)}/${nextIndex}`);
  route();
}

function closeLightbox(opts) {
  if (!lightbox.classList.contains("open")) {
    lightbox.classList.add("hidden");
    return;
  }

  if (USE_PHOTO_MORPH && activeCategory) {
    const targetImg = categoryGrid.children[activeIndex]?.querySelector(".tile-photo img");
    const transition = document.startViewTransition(() => {
      lightbox.classList.remove("open");
      lightbox.classList.add("hidden");
      // Set the tile's name only now, inside the callback -- at this point #lightboxImage's
      // permanent name is what the "before" snapshot captured, so there's no collision yet.
      if (targetImg) {
        targetImg.style.viewTransitionName = PHOTO_MORPH_NAME;
      }
    });
    transition.finished.finally(() => {
      if (targetImg) {
        targetImg.style.viewTransitionName = "";
      }
    });
  } else {
    lightbox.classList.remove("open");
    let closeFinished = false;
    const finishClose = () => {
      if (closeFinished) {
        return;
      }
      closeFinished = true;
      // Closing strips the photo index from the hash, which triggers a second route() pass
      // that calls closeLightbox() again -- if a photo is reopened before this fires, only hide
      // if the lightbox is still meant to be closed, not whatever got reopened since.
      if (!lightbox.classList.contains("open")) {
        lightbox.classList.add("hidden");
      }
    };
    // transitionend isn't guaranteed to fire (an interrupted transition, a backgrounded tab) --
    // without a fallback, a missed event leaves the lightbox as an invisible, click-swallowing
    // overlay forever.
    lightbox.addEventListener("transitionend", finishClose, { once: true });
    window.setTimeout(finishClose, 250);
  }

  // Focus the tile of the photo last viewed, not the one first clicked: after paging through a
  // category, keyboard users land where they left off instead of back at the start.
  const lastViewedTile = activeCategory ? categoryGrid.children[activeIndex] : null;
  activeCategory = null;
  setBackgroundInert(false);

  const focusTarget = lastViewedTile || lastFocusedElement;
  if (focusTarget) {
    focusTarget.focus();
  }
  lastFocusedElement = null;

  if (!opts || !opts.skipHashClear) {
    const match = location.hash.match(/^#category\/([^/]+)\/\d+$/);
    if (match) {
      if (lightboxPushedHistory) {
        history.back();
      } else {
        // Deep-linked straight to a photo: there's no grid entry behind it to go back to.
        location.hash = `#category/${match[1]}`;
      }
    }
  }
  lightboxPushedHistory = false;
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
