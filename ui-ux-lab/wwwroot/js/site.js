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
