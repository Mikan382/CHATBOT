document.documentElement.dataset.uiLab = "ready";

document.querySelectorAll("[data-busy-demo]").forEach((button) => {
    button.addEventListener("click", () => {
        button.setAttribute("aria-busy", "true");
        button.setAttribute("disabled", "");
        const label = button.querySelector("span");
        if (label) label.textContent = "Running…";
        window.setTimeout(() => {
            button.removeAttribute("aria-busy");
            button.removeAttribute("disabled");
            if (label) label.textContent = "Run benchmark";
        }, 1400);
    });
});

document.querySelectorAll("[data-tab-target]").forEach((tab) => {
    tab.addEventListener("click", () => {
        const group = tab.closest("[role='tablist']");
        if (!group) return;
        group.querySelectorAll("[role='tab']").forEach((item) => item.setAttribute("aria-selected", String(item === tab)));
        document.querySelectorAll(".ui-tab-panel").forEach((panel) => {
            panel.hidden = panel.id !== tab.dataset.tabTarget;
        });
    });
});

const shell = document.querySelector("[data-reference-shell]");
const rail = document.getElementById("globalRail");
const railToggle = document.querySelector("[data-rail-toggle]");
railToggle?.addEventListener("click", () => {
    const open = rail?.classList.toggle("open") ?? false;
    railToggle.setAttribute("aria-expanded", String(open));
});

document.querySelector("[data-theme-toggle]")?.addEventListener("click", () => {
    if (!shell) return;
    const next = shell.getAttribute("data-contrast") === "soft-dark" ? "" : "soft-dark";
    if (next) shell.setAttribute("data-contrast", next); else shell.removeAttribute("data-contrast");
});

document.querySelector("[data-auth-demo]")?.addEventListener("submit", (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    if (!(form instanceof HTMLFormElement) || !form.reportValidity()) return;
    const button = form.querySelector("button[type='submit']");
    const label = button?.querySelector("span");
    button?.setAttribute("disabled", "");
    button?.setAttribute("aria-busy", "true");
    if (label) label.textContent = "Preview only";
});

const sessionRail = document.getElementById("sessionRail");
const sessionToggle = document.querySelector("[data-session-toggle]");
sessionToggle?.addEventListener("click", () => {
    const open = sessionRail?.classList.toggle("open") ?? false;
    sessionToggle.setAttribute("aria-expanded", String(open));
});

const sessionSearch = document.querySelector("[data-session-search]");
const sessionItems = [...document.querySelectorAll("[data-session-title]")];
const sessionEmpty = document.querySelector("[data-session-empty]");
sessionSearch?.addEventListener("input", () => {
    const query = sessionSearch.value.trim().toLowerCase();
    let visible = 0;
    sessionItems.forEach((item) => {
        const match = item.dataset.sessionTitle.toLowerCase().includes(query);
        item.hidden = !match;
        if (match) visible += 1;
    });
    if (sessionEmpty) sessionEmpty.hidden = visible > 0;
});

document.querySelectorAll("[data-session-rename]").forEach((button) => {
    button.addEventListener("click", () => {
        const item = button.closest("[data-session-title]");
        const title = item?.querySelector("strong");
        if (!item || !title || item.querySelector("input")) return;
        const input = document.createElement("input");
        input.className = "session-rename-input";
        input.value = item.dataset.sessionTitle;
        title.parentElement.hidden = true;
        item.prepend(input);
        input.focus(); input.select();
        const finish = () => {
            const next = input.value.trim() || item.dataset.sessionTitle;
            item.dataset.sessionTitle = next; title.textContent = next;
            title.parentElement.hidden = false; input.remove();
        };
        input.addEventListener("blur", finish, { once: true });
        input.addEventListener("keydown", (event) => { if (event.key === "Enter" || event.key === "Escape") input.blur(); });
    });
});

let pendingSessionDelete = null;
const sessionDeleteElement = document.getElementById("sessionDeleteModal");
const sessionDeleteModal = sessionDeleteElement && window.bootstrap ? bootstrap.Modal.getOrCreateInstance(sessionDeleteElement) : null;
document.querySelectorAll("[data-session-delete]").forEach((button) => {
    button.addEventListener("click", () => {
        pendingSessionDelete = button.closest("[data-session-title]");
        const copy = document.querySelector("[data-session-delete-copy]");
        if (copy && pendingSessionDelete) copy.textContent = `“${pendingSessionDelete.dataset.sessionTitle}” and its saved citations will be removed.`;
        sessionDeleteModal?.show();
    });
});
document.querySelector("[data-confirm-session-delete]")?.addEventListener("click", () => { pendingSessionDelete?.remove(); pendingSessionDelete = null; });

const composer = document.querySelector("[data-composer-demo]");
const composerInput = document.querySelector("[data-composer-input]");
const resizeComposer = () => {
    if (!(composerInput instanceof HTMLTextAreaElement)) return;
    composerInput.style.height = "auto";
    composerInput.style.height = `${Math.min(composerInput.scrollHeight, 150)}px`;
};
composerInput?.addEventListener("input", resizeComposer);
composerInput?.addEventListener("keydown", (event) => {
    if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); composer?.requestSubmit(); }
});
composer?.addEventListener("submit", (event) => {
    event.preventDefault();
    if (!(composerInput instanceof HTMLTextAreaElement) || !composerInput.value.trim()) { composerInput?.focus(); return; }
    const button = composer.querySelector("button[type='submit']");
    const label = button?.querySelector("span");
    button?.setAttribute("disabled", ""); composerInput.setAttribute("disabled", "");
    if (label) label.textContent = "Preview sent";
});
