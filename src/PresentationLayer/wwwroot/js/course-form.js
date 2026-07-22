(function () {
  const headInput = document.querySelector("[data-head-input]");
  const headSummary = document.querySelector("[data-head-summary]");
  const teacherSummary = document.querySelector("[data-teacher-summary]");
  if (!headInput || !headSummary || !teacherSummary) return;

  const modal = document.getElementById("courseTeacherModal");
  const search = modal.querySelector("[data-teacher-search]");
  const noResults = modal.querySelector("[data-teacher-no-results]");
  const hasTeacherAccounts = modal.querySelectorAll("[data-teacher-checkbox]").length > 0;

  function checkedTeachers() {
    return Array.from(modal.querySelectorAll("[data-teacher-checkbox]:checked"));
  }

  function checkboxFor(teacherId) {
    return modal.querySelector(`[data-teacher-checkbox][value="${teacherId}"]`);
  }

  // A head teacher who is no longer on the course is not a head teacher.
  function headCheckbox() {
    if (!headInput.value) return null;
    const checkbox = checkboxFor(headInput.value);
    return checkbox && checkbox.checked ? checkbox : null;
  }

  function emptyRow(message) {
    const div = document.createElement("div");
    div.className = "assignment-empty";
    div.textContent = message;
    return div;
  }

  function actionButton(label, className, action, teacherId) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `btn btn-sm ${className}`;
    button.textContent = label;
    button.dataset.action = action;
    button.dataset.teacherId = teacherId;
    return button;
  }

  function personRow(checkbox, actions) {
    const row = document.createElement("div");
    row.className = "assignment-row";

    const text = document.createElement("span");
    text.className = "assignment-row-text";
    const name = document.createElement("strong");
    name.textContent = checkbox.dataset.teacherName;
    const email = document.createElement("small");
    email.textContent = checkbox.dataset.teacherEmail;
    text.append(name, email);
    row.append(text, ...actions);

    return row;
  }

  function headBadge() {
    const badge = document.createElement("span");
    badge.className = "badge text-bg-info";
    badge.textContent = "Head";
    return badge;
  }

  function render() {
    const head = headCheckbox();
    // Drop a stale id so the form never posts a head who left the course.
    if (!head) headInput.value = "";

    if (!hasTeacherAccounts) {
      const message = "No active Teacher accounts exist yet. Create one under Users first.";
      headSummary.replaceChildren(emptyRow(message));
      teacherSummary.replaceChildren(emptyRow(message));
      return;
    }

    headSummary.replaceChildren(head
      ? personRow(head, [actionButton("Clear", "btn-outline-secondary", "clear-head", head.value)])
      : emptyRow("No head teacher. Add teachers below, then mark one as head."));

    const teachers = checkedTeachers();
    teacherSummary.replaceChildren();
    if (teachers.length === 0) {
      teacherSummary.appendChild(emptyRow("No teachers assigned to this course."));
      return;
    }

    for (const teacher of teachers) {
      const isHead = head && head.value === teacher.value;
      teacherSummary.appendChild(personRow(teacher, [
        isHead ? headBadge() : actionButton("Set as head", "btn-outline-primary", "set-head", teacher.value),
        actionButton("Remove", "btn-outline-danger", "remove", teacher.value)
      ]));
    }
  }

  modal.addEventListener("change", (event) => {
    if (event.target.matches("[data-teacher-checkbox]")) render();
  });

  function handleAction(event) {
    const button = event.target.closest("[data-action]");
    if (!button) return;

    const teacherId = button.dataset.teacherId;
    if (button.dataset.action === "set-head") {
      headInput.value = teacherId;
    } else if (button.dataset.action === "clear-head") {
      headInput.value = "";
    } else {
      // Removing someone from the course also drops the head role they may hold.
      const checkbox = checkboxFor(teacherId);
      if (checkbox) checkbox.checked = false;
      if (headInput.value === teacherId) headInput.value = "";
    }

    render();
  }

  headSummary.addEventListener("click", handleAction);
  teacherSummary.addEventListener("click", handleAction);

  search.addEventListener("input", () => {
    const query = search.value.trim().toLocaleLowerCase();
    let visibleCount = 0;

    for (const option of modal.querySelectorAll("[data-teacher-option]")) {
      const visible = !query || option.textContent.toLocaleLowerCase().includes(query);
      option.hidden = !visible;
      if (visible) visibleCount += 1;
    }

    noResults.hidden = visibleCount > 0;
  });

  const strategySelect = document.getElementById("DefaultChunkingStrategy");
  const fixedChunkOptions = document.querySelectorAll("[data-fixed-chunk-option]");

  function updateChunkOptionsVisibility() {
    if (!strategySelect || !fixedChunkOptions.length) return;
    const isFixed = strategySelect.value === "fixed";
    fixedChunkOptions.forEach((el) => {
      const inputs = el.querySelectorAll("input");
      inputs.forEach((input) => {
        input.disabled = !isFixed;
      });
      el.style.opacity = isFixed ? "1" : "0.5";
    });
  }

  if (strategySelect) {
    strategySelect.addEventListener("change", updateChunkOptionsVisibility);
    updateChunkOptionsVisibility();
  }

  render();
})();
