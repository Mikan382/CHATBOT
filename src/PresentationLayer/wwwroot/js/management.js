(function () {
  let pendingForm = null;
  const modalEl = document.getElementById("managementConfirmModal");
  const titleEl = document.getElementById("managementConfirmTitle");
  const messageEl = document.getElementById("managementConfirmMessage");
  const actionEl = document.getElementById("managementConfirmAction");
  const modal = modalEl && window.bootstrap ? new bootstrap.Modal(modalEl) : null;

  document.querySelectorAll(".management-confirm-form").forEach((form) => {
    form.addEventListener("submit", (event) => {
      const message = form.dataset.confirmMessage || "Continue with this action?";
      const title = form.dataset.confirmTitle || "Confirm action";

      if (!modal || !titleEl || !messageEl || !actionEl) {
        if (!confirm(message)) {
          event.preventDefault();
        }
        return;
      }

      event.preventDefault();
      pendingForm = form;
      titleEl.textContent = title;
      messageEl.textContent = message;
      actionEl.textContent = form.dataset.confirmAction || "Confirm";
      modal.show();
    });
  });

  if (actionEl) {
    actionEl.addEventListener("click", () => {
      const form = pendingForm;
      pendingForm = null;
      modal?.hide();
      form?.submit();
    });
  }
})();
