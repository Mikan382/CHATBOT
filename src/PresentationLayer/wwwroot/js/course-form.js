(function () {
  const assignment = document.querySelector("[data-teacher-assignment]");
  if (!assignment) return;

  const search = assignment.querySelector("[data-teacher-search]");
  const list = assignment.querySelector("[data-teacher-list]");
  const selectedCount = assignment.querySelector("[data-teacher-selected-count]");
  const noResults = assignment.querySelector("[data-teacher-no-results]");
  const emptyState = assignment.querySelector("[data-teacher-empty-state]");

  const panel = assignment.querySelector("[data-teacher-create-panel]");
  const toggle = assignment.querySelector("[data-teacher-add-toggle]");
  const emailInput = assignment.querySelector("[data-teacher-new-email]");
  const nameInput = assignment.querySelector("[data-teacher-new-name]");
  const passwordInput = assignment.querySelector("[data-teacher-new-password]");
  const submit = assignment.querySelector("[data-teacher-create-submit]");
  const cancel = assignment.querySelector("[data-teacher-create-cancel]");
  const errorBox = assignment.querySelector("[data-teacher-create-error]");

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

  function showError(message) {
    errorBox.textContent = message;
    errorBox.hidden = !message;
  }

  function setPanelOpen(open) {
    panel.hidden = !open;
    toggle.textContent = open ? "Close" : "Add teacher";
    if (open) {
      showError("");
      emailInput.focus();
    }
  }

  function clearPanel() {
    emailInput.value = "";
    nameInput.value = "";
    passwordInput.value = "";
    showError("");
  }

  // Built with createElement/textContent rather than innerHTML so a display name
  // containing markup cannot inject HTML.
  function addTeacherOption(teacher) {
    const label = document.createElement("label");
    label.className = "teacher-option";
    label.setAttribute("data-teacher-option", "");

    const radio = document.createElement("input");
    radio.className = "form-check-input";
    radio.type = "radio";
    radio.name = "TeacherId";
    radio.value = teacher.id;
    radio.checked = true;

    const content = document.createElement("span");
    content.className = "teacher-option-content";

    const displayName = document.createElement("strong");
    displayName.textContent = teacher.displayName;
    const email = document.createElement("small");
    email.textContent = teacher.email;
    content.append(displayName, email);

    label.append(radio, content);
    // Keep the "Unassigned" choice first; a freshly created teacher goes right after it.
    const unassigned = assignment.querySelector("[data-teacher-unassigned]");
    if (unassigned) {
      unassigned.after(label);
    } else {
      list.prepend(label);
    }

    list.hidden = false;
    search.hidden = false;
    emptyState.hidden = true;
  }

  async function createTeacher() {
    const payload = {
      email: emailInput.value.trim(),
      fullName: nameInput.value.trim(),
      password: passwordInput.value
    };

    if (!payload.email || !payload.fullName || !payload.password) {
      showError("Email, display name, and password are required.");
      return;
    }

    submit.disabled = true;
    showError("");

    try {
      const response = await fetch("/api/courses/teachers", {
        method: "POST",
        headers: window.requestVerificationHeaders({ "Content-Type": "application/json" }),
        body: JSON.stringify(payload)
      });

      if (response.status === 401) {
        window.location.href = "/Account/Login";
        return;
      }

      const result = await response.json().catch(() => null);
      if (!response.ok || !result?.success) {
        showError(result?.error ?? "The teacher could not be created.");
        return;
      }

      addTeacherOption(result.teacher);
      updateSelection();
      search.value = "";
      filterTeachers();
      clearPanel();
      setPanelOpen(false);
    } catch {
      showError("The teacher could not be created.");
    } finally {
      submit.disabled = false;
    }
  }

  // Delegated so options added later do not need rebinding.
  list.addEventListener("change", (event) => {
    if (event.target.matches('input[type="radio"]')) {
      updateSelection();
    }
  });

  search.addEventListener("input", filterTeachers);
  toggle.addEventListener("click", () => setPanelOpen(panel.hidden));
  submit.addEventListener("click", createTeacher);
  cancel.addEventListener("click", () => {
    clearPanel();
    setPanelOpen(false);
  });

  // Enter inside the panel would otherwise submit the surrounding course form.
  for (const input of [emailInput, nameInput, passwordInput]) {
    input.addEventListener("keydown", (event) => {
      if (event.key === "Enter") {
        event.preventDefault();
        createTeacher();
      }
    });
  }

  updateSelection();
})();
