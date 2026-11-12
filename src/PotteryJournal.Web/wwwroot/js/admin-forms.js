// Generic support for repeatable form rows (piece notes, glaze applications) in the admin area.
// "Remove" hides a row and clears its inputs rather than removing it from the DOM, so the
// remaining rows keep their original, sequential model-binding indices.
document.addEventListener("click", (event) => {
  const addButton = event.target.closest("[data-add-row]");
  if (addButton) {
    event.preventDefault();
    addRow(addButton.getAttribute("data-add-row"));
    return;
  }

  const removeButton = event.target.closest("[data-remove-row]");
  if (removeButton) {
    event.preventDefault();
    removeRow(removeButton);
  }
});

function addRow(containerId) {
  const container = document.getElementById(containerId);
  const template = document.getElementById(`${containerId}Template`);
  if (!container || !template) return;

  const index = container.querySelectorAll(".admin-repeatable-row").length;
  const html = template.innerHTML.replaceAll("__index__", String(index));

  const wrapper = document.createElement("div");
  wrapper.innerHTML = html.trim();
  container.appendChild(wrapper.firstElementChild);
}

function removeRow(removeButton) {
  const row = removeButton.closest(".admin-repeatable-row");
  if (!row) return;

  row.classList.add("row-removed");
  row.querySelectorAll("input, textarea").forEach((el) => {
    el.value = "";
  });
  row.querySelectorAll("select").forEach((el) => {
    el.value = "";
  });
}

// Shows/hides a target element based on a control's state -- a <select> (e.g. a recurrence
// frequency dropdown) collapses the target when set to "None"; a checkbox collapses the target
// when unchecked. Declarative, so any admin form can opt in with data-recurrence-toggle="<id>".
document.querySelectorAll("[data-recurrence-toggle]").forEach((control) => {
  const target = document.getElementById(control.getAttribute("data-recurrence-toggle"));
  if (!target) return;

  const isCollapsed = () => (control.type === "checkbox" ? !control.checked : control.value === "None");
  const sync = () => target.classList.toggle("d-none", isCollapsed());
  control.addEventListener("change", sync);
  sync();
});
