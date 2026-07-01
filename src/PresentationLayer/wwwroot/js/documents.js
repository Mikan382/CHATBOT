(function () {
  let pendingDeleteForm = null;
  const confirmModalEl = document.getElementById("documentConfirmModal");
  const confirmMessage = document.getElementById("documentConfirmMessage");
  const confirmAction = document.getElementById("documentConfirmAction");
  const confirmModal = confirmModalEl && window.bootstrap ? new bootstrap.Modal(confirmModalEl) : null;

  document.querySelectorAll(".document-delete-form").forEach((form) => {
    form.addEventListener("submit", (event) => {
      if (!confirmModal || !confirmMessage || !confirmAction) {
        if (!confirm("Delete this document and all related indexed sections?")) {
          event.preventDefault();
        }
        return;
      }

      event.preventDefault();
      const documentName = form.dataset.documentName || "this document";
      confirmMessage.textContent = `Delete ${documentName} and all related indexed sections?`;
      pendingDeleteForm = form;
      confirmModal.show();
    });
  });

  if (confirmAction) {
    confirmAction.addEventListener("click", () => {
      const form = pendingDeleteForm;
      pendingDeleteForm = null;
      confirmModal?.hide();
      form?.submit();
    });
  }

  const uploadForm = document.getElementById("uploadForm");
  if (uploadForm) {
    const uploadCourse = document.getElementById("uploadCourseId");
    const uploadChapter = document.getElementById("uploadChapterId");
    const uploadFileInput = document.getElementById("uploadFileInput");
    const uploadFileError = document.getElementById("uploadFileError");
    const maxUploadBytes = 20 * 1024 * 1024;
    const formatMb = (bytes) => `${Math.round(bytes / 1024 / 1024)} MB`;

    const validateUploadFile = () => {
      uploadFileError.classList.add("d-none");
      uploadFileError.textContent = "";
      const file = uploadFileInput.files && uploadFileInput.files[0];
      if (!file || file.size <= maxUploadBytes) {
        return true;
      }

      uploadFileError.textContent = `File size must be 20 MB or smaller. Selected file is ${formatMb(file.size)}.`;
      uploadFileError.classList.remove("d-none");
      return false;
    };

    const syncUploadChapters = () => {
      let firstVisible = null;
      uploadChapter.querySelectorAll("option[data-course-id]").forEach((option) => {
        const visible = option.dataset.courseId === uploadCourse.value;
        option.hidden = !visible;
        if (visible && !firstVisible) {
          firstVisible = option;
        }
      });
      if (firstVisible) {
        if (uploadChapter.selectedOptions[0]?.hidden || !uploadChapter.value) {
          uploadChapter.value = firstVisible.value;
        }
      } else {
        uploadChapter.value = "";
      }
    };

    uploadCourse.addEventListener("change", syncUploadChapters);
    uploadFileInput.addEventListener("change", validateUploadFile);
    syncUploadChapters();

    uploadForm.addEventListener("submit", (event) => {
      if (!validateUploadFile()) {
        event.preventDefault();
        return;
      }

      const button = document.getElementById("uploadButton");
      document.getElementById("uploadStatus").classList.remove("d-none");
      button.disabled = true;
      button.querySelector(".upload-button-text").textContent = "Uploading...";
      button.querySelector(".spinner-border").classList.remove("d-none");
    });
  }

  if (document.querySelector(".badge.bg-warning, .badge.bg-info")) {
    setTimeout(() => window.location.reload(), 5000);
  }
})();
