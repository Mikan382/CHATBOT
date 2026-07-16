(function () {
  const assignment = document.querySelector("[data-teacher-assignment]");
  if (!assignment) return;

  const search = assignment.querySelector("[data-teacher-search]");
  const options = Array.from(assignment.querySelectorAll("[data-teacher-option]"));
  const selectedCount = assignment.querySelector("[data-teacher-selected-count]");
  const noResults = assignment.querySelector("[data-teacher-no-results]");

  function updateSelection() {
    const selected = options.filter((option) => {
      const checkbox = option.querySelector('input[type="checkbox"]');
      const checked = checkbox.checked;
      option.classList.toggle("selected", checked);
      return checked;
    }).length;

    selectedCount.textContent = `${selected} selected`;
  }

  function filterTeachers() {
    const query = search.value.trim().toLocaleLowerCase();
    let visibleCount = 0;

    for (const option of options) {
      const visible = !query || option.textContent.toLocaleLowerCase().includes(query);
      option.hidden = !visible;
      if (visible) visibleCount += 1;
    }

    if (noResults) {
      noResults.hidden = visibleCount > 0;
    }
  }

  for (const option of options) {
    option.querySelector('input[type="checkbox"]').addEventListener("change", updateSelection);
  }

  if (search) {
    search.addEventListener("input", filterTeachers);
  }

  updateSelection();
})();
