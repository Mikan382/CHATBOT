(() => {
    const query = new URLSearchParams(window.location.search);
    const requestedLive = query.get("mode") === "live";
    const statusElement = document.querySelector("[data-adapter-state]");

    class LiveAdapterError extends Error {
        constructor(message, status = 0) {
            super(message);
            this.name = "LiveAdapterError";
            this.status = status;
        }
    }

    const setStatus = (label, tone) => {
        if (!statusElement) return;
        statusElement.className = `ui-badge ui-badge-${tone}`;
        statusElement.replaceChildren();
        statusElement.append(document.createElement("i"), document.createTextNode(label));
    };

    const requestJson = async (path, options = {}) => {
        if (!requestedLive) throw new LiveAdapterError("Live mode was not requested.");
        const abortController = new AbortController();
        const timeoutId = window.setTimeout(() => abortController.abort(), 5000);
        try {
            const response = await fetch(`/backend${path}`, {
                ...options,
                credentials: "include",
                signal: abortController.signal,
                headers: { Accept: "application/json", ...(options.headers ?? {}) }
            });
            if (response.status === 401) throw new LiveAdapterError("Sign in is required.", 401);
            if (response.status === 403) throw new LiveAdapterError("This account does not have access.", 403);
            if (!response.ok) throw new LiveAdapterError(`Application API returned ${response.status}.`, response.status);
            setStatus("Live database", "success");
            adapter.mode = "live";
            return response.status === 204 ? null : response.json();
        } catch (error) {
            if (error instanceof LiveAdapterError) throw error;
            adapter.mode = "unavailable";
            throw new LiveAdapterError(
                error instanceof DOMException && error.name === "AbortError"
                    ? "The application did not respond within five seconds."
                    : "The application is offline."
            );
        } finally {
            window.clearTimeout(timeoutId);
        }
    };

    const adapter = {
        requestedLive,
        mode: requestedLive ? "checking" : "fixture",
        LiveAdapterError,
        setStatus,
        requestJson,
        me: () => requestJson("/api/ui-lab/me"),
        courses: () => requestJson("/api/ui-lab/courses"),
        documents: () => requestJson("/api/ui-lab/documents"),
        document: (id) => requestJson(`/api/ui-lab/documents/${encodeURIComponent(id)}`),
        benchmark: () => requestJson("/api/ui-lab/benchmark"),
        users: () => requestJson("/api/ui-lab/users"),
        sessions: () => requestJson("/api/chat/sessions"),
        history: (sessionId) => requestJson(`/api/chat/${encodeURIComponent(sessionId)}`),
        createChatConnection() {
            if (!requestedLive || !window.signalR) return null;
            return new window.signalR.HubConnectionBuilder()
                .withUrl("/backend/chatHub")
                .withAutomaticReconnect()
                .build();
        }
    };

    window.UiLabAdapter = adapter;
    setStatus(requestedLive ? "Checking live data…" : "Fixture preview", "warning");
})();
