// Collapses the main nav behind a "Menu" button on narrow screens. Progressive enhancement:
// without this script the links simply wrap, as they always did; the .has-toggle class is what
// lets site.css hide them, so no JavaScript means no hidden nav.
(() => {
  const nav = document.querySelector(".site-nav");
  const toggle = nav?.querySelector(".nav-toggle");
  if (!nav || !toggle) {
    return;
  }

  nav.classList.add("has-toggle");

  function setOpen(open) {
    nav.classList.toggle("is-open", open);
    toggle.setAttribute("aria-expanded", open ? "true" : "false");
  }

  toggle.addEventListener("click", () => setOpen(!nav.classList.contains("is-open")));

  nav.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && nav.classList.contains("is-open")) {
      setOpen(false);
      toggle.focus();
    }
  });
})();
