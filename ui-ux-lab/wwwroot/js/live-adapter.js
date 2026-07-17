(() => {
    const query = new URLSearchParams(window.location.search);
    const wantsLive = query.get("mode") === "live";
    const statusElement = document.querySelector("[data-adapter-state]");
    const fixtures = {
        sessions: [
            { id: "fixture-advanced-mvc", title: "Advanced MVC patterns" },
            { id: "fixture-dependency-injection", title: "Dependency injection review" }
        ]
    };

    const setStatus = (label, tone) => {
        if (!statusElement) return;
        statusElement.className = `ui-badge ui-badge-${tone}`;
        statusElement.innerHTML = `<i></i>${label}`;
    };

    const adapter = {
        mode: wantsLive ? "live" : "fixture",
        async listSessions() {
            if (!wantsLive) return fixtures.sessions;
            const abortController = new AbortController();
            const timeoutId = window.setTimeout(() => abortController.abort(), 3500);
            try {
                const response = await fetch("/backend/api/chat/sessions", {
                    credentials: "include",
                    signal: abortController.signal,
                    headers: { Accept: "application/json" }
                });
                if (!response.ok) throw new Error(`REST probe returned ${response.status}`);
                const payload = await response.json();
                setStatus("Live application", "success");
                return payload.sessions ?? [];
            } catch (error) {
                adapter.mode = "fixture-fallback";
                adapter.lastError = error instanceof Error ? error.message : String(error);
                setStatus("Fixture fallback", "warning");
                return fixtures.sessions;
            } finally {
                window.clearTimeout(timeoutId);
            }
        },
        createChatConnection() {
            if (!wantsLive || !window.signalR) return null;
            return new window.signalR.HubConnectionBuilder()
                .withUrl("/backend/chatHub")
                .withAutomaticReconnect()
                .build();
        }
    };

    window.UiLabAdapter = adapter;
    if (wantsLive) {
        setStatus("Checking live app…", "warning");
        adapter.listSessions();
    } else {
        setStatus("Fixture preview", "warning");
    }
})();
