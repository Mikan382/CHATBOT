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
