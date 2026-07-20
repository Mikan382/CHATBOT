(function () {
  const assignment = document.querySelector("[data-teacher-assignment]");
  if (!assignment) return;

  const search = assignment.querySelector("[data-teacher-search]");
  const list = assignment.querySelector("[data-teacher-list]");
  const selectedCount = assignment.querySelector("[data-teacher-selected-count]");
  const noResults = assignment.querySelector("[data-teacher-no-results]");

  // Re-queried on each use so options added after load are included.
  function teacherOptions() {
    return Array.from(assignment.querySelectorAll("[data-teacher-option]"));
  }

  function updateSelection() {
    let selectedName = "";
    for (const option of teacherOptions()) {
      const radio = option.querySelector('input[type="radio"]');
      option.classList.toggle("selected", radio.checked);
      if (radio.checked && radio.value) {
        selectedName = option.querySelector("strong")?.textContent?.trim() ?? "";
      }
    }

    selectedCount.textContent = selectedName || "Unassigned";
  }

  function filterTeachers() {
    const query = search.value.trim().toLocaleLowerCase();
    let visibleCount = 0;

    for (const option of teacherOptions()) {
      // The "Unassigned" choice always stays available so a teacher can be cleared.
      if (option.hasAttribute("data-teacher-unassigned")) {
        option.hidden = false;
        continue;
      }
      const visible = !query || option.textContent.toLocaleLowerCase().includes(query);
      option.hidden = !visible;
      if (visible) visibleCount += 1;
    }

    if (noResults) {
      noResults.hidden = visibleCount > 0;
    }
  }

  // Delegated so options added later do not need rebinding.
  list.addEventListener("change", (event) => {
    if (event.target.matches('input[type="radio"]')) {
      updateSelection();
    }
  });

  search.addEventListener("input", filterTeachers);

  updateSelection();
})();
