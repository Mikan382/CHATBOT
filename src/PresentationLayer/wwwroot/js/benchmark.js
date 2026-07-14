document.addEventListener("DOMContentLoaded", () => {
  const runForm = document.querySelector("[data-benchmark-run-form]");
  const runButton = document.querySelector("[data-benchmark-run-button]");
  const runningStatus = document.querySelector("[data-benchmark-running]");

  runForm?.addEventListener("submit", () => {
    if (runButton) {
      runButton.disabled = true;
    }
    runningStatus?.classList.remove("d-none");
  });

  const courseSelect = document.querySelector("[data-benchmark-course-select]");
  const documentSelect = document.querySelector("[data-benchmark-document-select]");
  const noDocuments = document.querySelector("[data-benchmark-no-documents]");
  if (!courseSelect || !documentSelect) {
    return;
  }

  const filterDocuments = () => {
    const courseId = courseSelect.value;
    let availableCount = 0;
    let selectedAvailable = false;

    Array.from(documentSelect.options).forEach((option) => {
      if (!option.value) {
        return;
      }

      const available = option.dataset.courseId === courseId;
      option.hidden = !available;
      option.disabled = !available;
      if (available) {
        availableCount += 1;
        selectedAvailable ||= option.selected;
      }
    });

    if (!selectedAvailable) {
      documentSelect.value = "";
    }
    noDocuments?.classList.toggle("d-none", availableCount > 0);
  };

  courseSelect.addEventListener("change", filterDocuments);
  filterDocuments();
});
