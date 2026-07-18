(function () {
  var ROTATION_INTERVAL_MS = 5000;

  var stage = document.querySelector('[data-featured-collection]');
  if (!stage) {
    return;
  }

  var slides = stage.querySelectorAll('[data-featured-slide]');
  if (slides.length < 2) {
    return;
  }

  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    return;
  }

  var current = 0;
  setInterval(function () {
    slides[current].classList.remove('is-active');
    current = (current + 1) % slides.length;
    slides[current].classList.add('is-active');
  }, ROTATION_INTERVAL_MS);
})();
