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
