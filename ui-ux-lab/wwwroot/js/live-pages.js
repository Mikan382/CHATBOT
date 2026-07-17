(() => {
    const adapter = window.UiLabAdapter;
    if (!adapter?.requestedLive) return;

    const banner = document.querySelector("[data-live-banner]");
    const page = document.body.dataset.livePage ?? "";
    const query = new URLSearchParams(window.location.search);

    const element = (tag, className, text) => {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = String(text);
        return node;
    };

    const formatDate = (value) => value
        ? new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value))
        : "—";

    const formatBytes = (value) => {
        const bytes = Number(value ?? 0);
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    };

    const showBanner = (message, tone = "warning", signIn = false) => {
        if (!banner) return;
        banner.hidden = false;
        banner.className = `live-data-banner live-data-banner-${tone}`;
        banner.replaceChildren(element("strong", "", message));
        if (signIn) {
            const returnUrl = `${window.location.pathname}${window.location.search}`;
            const link = element("a", "ui-button ui-button-primary", "Sign in to live application");
            link.href = `/backend/Account/Login?returnUrl=${encodeURIComponent(returnUrl)}`;
            banner.append(link);
        } else {
            banner.append(element("span", "", "Fixture content remains visible below for comparison."));
        }
    };

    const showLiveIdentity = (identity) => {
        const account = document.querySelector(".rail-account .ui-avatar");
        if (account) {
            const source = identity.displayName || identity.email || identity.role || "DB";
            account.textContent = source.split(/\s+|@/).filter(Boolean).slice(0, 2).map(part => part[0]).join("").toUpperCase();
            account.setAttribute("aria-label", `${source} · ${identity.role}`);
        }
        showBanner(`Live database · ${identity.displayName || identity.email} · ${identity.role}`, "success");
    };

    const renderSessions = (sessions) => {
        const container = document.querySelector("[data-session-list]");
        if (!container) return;
        container.replaceChildren();
        const section = element("section");
        section.append(element("h2", "", sessions.length ? "Database sessions" : "No saved sessions"));
        sessions.forEach(session => {
            const article = element("article", "session-item");
            article.dataset.sessionTitle = session.title;
            const link = element("a");
            link.href = `/Reference/Chat?mode=live&sessionId=${encodeURIComponent(session.id)}`;
            link.append(element("strong", "", session.title), element("small", "", formatDate(session.updatedAtUtc)));
            article.append(link);
            section.append(article);
        });
        container.append(section);

        const search = document.querySelector("[data-session-search]");
        search?.addEventListener("input", () => {
            const value = search.value.trim().toLowerCase();
            section.querySelectorAll("[data-session-title]").forEach(item => {
                item.hidden = !item.dataset.sessionTitle.toLowerCase().includes(value);
            });
        });
    };

    const appendMessage = (message) => {
        const list = document.querySelector("[data-live-messages]");
        if (!list) return;
        const assistant = String(message.role).toLowerCase() !== "user";
        const article = element("article", `chat-message chat-message-${assistant ? "assistant" : "user"}`);
        article.append(element("div", `message-avatar${assistant ? " message-avatar-assistant" : ""}`, assistant ? "✦" : "You"));
        const body = element("div");
        const header = element("header");
        header.append(element("strong", "", assistant ? "Assistant" : "You"), element("time", "", formatDate(message.createdAtUtc)));
        const copy = element("div", assistant ? "assistant-answer" : "");
        copy.append(element("p", "", message.content));
        body.append(header, copy);
        if (assistant && Array.isArray(message.citations) && message.citations.length) {
            const citations = element("div", "citation-list");
            citations.setAttribute("aria-label", "Answer sources");
            message.citations.forEach((citation, index) => {
                const details = element("details");
                const summary = element("summary");
                summary.append(element("span", "", String(index + 1).padStart(2, "0")), element("strong", "", citation.sourceName), element("small", "", citation.chapterTitle));
                details.append(summary, element("p", "", citation.text));
                citations.append(details);
            });
            body.append(citations);
        }
        article.append(body);
        list.append(article);
        article.scrollIntoView({ block: "nearest" });
    };

    const renderHistory = (history) => {
        const list = document.querySelector("[data-live-messages]");
        if (!list) return;
        list.replaceChildren();
        history.forEach(appendMessage);
        if (!history.length) {
            const empty = element("div", "chat-stage-note");
            empty.append(element("span", "", "Database session"), element("h3", "", "No messages yet"), element("p", "", "Send the first question to create this conversation in the current application."));
            list.append(empty);
        }
    };

    const startChat = async () => {
        const [sessionPayload, coursePayload] = await Promise.all([adapter.sessions(), adapter.courses()]);
        const sessions = sessionPayload.sessions ?? [];
        const courses = coursePayload.courses ?? [];
        renderSessions(sessions);

        const sessionId = query.get("sessionId") || window.crypto.randomUUID();
        const selectedSession = sessions.find(item => item.id === sessionId);
        const courseId = query.get("courseId") || courses[0]?.id;
        const selectedCourse = courses.find(item => item.id === courseId) || courses[0];
        const courseLabel = document.querySelector("[data-live-course]");
        const sessionTitle = document.querySelector("[data-live-session-title]");
        if (courseLabel) courseLabel.textContent = selectedCourse ? `${selectedCourse.code} · ${selectedCourse.name}` : "No course available";
        if (sessionTitle) sessionTitle.textContent = selectedSession?.title ?? "New database conversation";
        if (query.get("sessionId")) {
            const historyPayload = await adapter.history(sessionId);
            renderHistory(historyPayload.history ?? []);
        } else {
            renderHistory([]);
        }

        const composer = document.querySelector("[data-composer-demo]");
        const input = document.querySelector("[data-composer-input]");
        const sendButton = composer?.querySelector("button[type='submit']");
        if (!courseId || !composer || !(input instanceof HTMLTextAreaElement)) {
            input?.setAttribute("disabled", "");
            return;
        }

        const connection = adapter.createChatConnection();
        if (!connection) return;
        connection.on("MessageReceived", message => {
            const empty = document.querySelector("[data-live-messages] .chat-stage-note");
            empty?.remove();
            appendMessage(message);
            if (String(message.role).toLowerCase() !== "user") {
                input.removeAttribute("disabled");
                sendButton?.removeAttribute("disabled");
                input.focus();
            }
        });
        connection.on("MessageFailed", message => {
            showBanner(message || "The assistant rejected the message.", "danger");
            input.removeAttribute("disabled");
            sendButton?.removeAttribute("disabled");
        });
        await connection.start();
        await connection.invoke("JoinSession", sessionId);
        composer.addEventListener("submit", async event => {
            event.preventDefault();
            const text = input.value.trim();
            if (!text) return;
            input.value = "";
            input.setAttribute("disabled", "");
            sendButton?.setAttribute("disabled", "");
            try {
                await connection.invoke("SendMessage", sessionId, courseId, text);
            } catch {
                showBanner("The message could not be sent to the live application.", "danger");
                input.removeAttribute("disabled");
                sendButton?.removeAttribute("disabled");
            }
        });
    };

    const renderDocuments = (payload) => {
        const tbody = document.querySelector("[data-live-documents]");
        const count = document.querySelector("[data-live-document-count]");
        const managePanel = document.querySelector("[data-live-manage-panel]");
        if (!tbody) return;
        const documents = payload.documents ?? [];
        if (managePanel && !payload.permissions?.canManage) {
            managePanel.replaceChildren(
                element("p", "lab-eyebrow", "View-only access"),
                element("h2", "", "Document library"),
                element("p", "", "Your database role can inspect indexed sources but cannot upload or modify them."));
        }
        tbody.replaceChildren();
        if (count) count.textContent = `${documents.length} database document${documents.length === 1 ? "" : "s"}`;
        documents.forEach(documentItem => {
            const row = element("tr");
            const file = element("td");
            const link = element("a", "", documentItem.originalFileName);
            link.href = `/Reference/DocumentDetails?mode=live&id=${encodeURIComponent(documentItem.id)}`;
            file.append(link, element("small", "", `${documentItem.fileType} · ${formatBytes(documentItem.fileSizeBytes)}`));
            const status = element("span", "ui-badge ui-badge-success");
            status.append(element("i"), document.createTextNode("Indexed"));
            const action = element("a", "ui-button ui-button-secondary", "View");
            action.href = link.href;
            [file, element("td", "", documentItem.chapterTitle || "—"), element("td"), element("td", "", documentItem.chunksCount), element("td", "", formatDate(documentItem.uploadedAtUtc)), element("td")].forEach((cell, index) => {
                if (index === 2) cell.append(status);
                if (index === 5) cell.append(action);
                row.append(cell);
            });
            tbody.append(row);
        });
        if (!documents.length) {
            const row = element("tr");
            const cell = element("td", "", "No database documents are visible to this account.");
            cell.colSpan = 6;
            row.append(cell);
            tbody.append(row);
        }
    };

    const renderCourses = (payload) => {
        const container = document.querySelector("[data-live-courses]");
        const chapterList = document.querySelector("[data-live-chapters]");
        const title = document.querySelector("[data-live-course-title]");
        const summary = document.querySelector("[data-live-course-summary]");
        if (!container || !chapterList) return;
        const search = container.querySelector(".course-search");
        container.replaceChildren();
        if (search) container.append(search);
        const courses = payload.courses ?? [];
        const selectCourse = course => {
            container.querySelectorAll(".course-card").forEach(card => card.classList.toggle("active", card.dataset.courseId === course.id));
            if (title) title.textContent = `${course.code} · ${course.name}`;
            if (summary) summary.textContent = course.description || "No course description.";
            chapterList.replaceChildren();
            (course.chapters ?? []).forEach(chapter => {
                const item = element("li");
                item.append(element("span", "chapter-index", String(chapter.order).padStart(2, "0")));
                const copy = element("div");
                copy.append(element("strong", "", chapter.title), element("small", "", chapter.summary || chapter.clo || "No summary"));
                item.append(copy);
                chapterList.append(item);
            });
            if (!(course.chapters ?? []).length) chapterList.append(element("li", "chapter-draft", "No database chapters yet."));
        };
        courses.forEach((course, index) => {
            const card = element("button", `course-card${index === 0 ? " active" : ""}`);
            card.type = "button";
            card.dataset.courseId = course.id;
            card.append(element("span", "course-code", course.code), element("strong", "", course.name), element("small", "", `${course.chapters?.length ?? 0} database chapters`));
            card.addEventListener("click", () => selectCourse(course));
            container.append(card);
        });
        if (courses[0]) selectCourse(courses[0]);
        if (!payload.permissions?.canManage) {
            document.querySelectorAll(".course-heading-actions button,.chapter-workspace-heading>div:last-child,.chapter-list-heading button").forEach(node => node.hidden = true);
        }
    };

    const renderBenchmark = (payload) => {
        const kpis = document.querySelector("[data-live-benchmark-kpis]");
        const tbody = document.querySelector("[data-live-benchmark-runs]");
        if (!kpis || !tbody) return;
        const run = payload.recentRuns?.[0] ?? payload.latestComparisons?.[0];
        const values = run
            ? [["Hit rate", `${(run.hitRate * 100).toFixed(1)}%`], ["Mean reciprocal rank", run.meanReciprocalRank.toFixed(3)], ["Answer token F1", run.averageAnswerTokenF1.toFixed(3)], ["Average latency", `${Math.round(run.averageLatencyMilliseconds)} ms`]]
            : [["Ground-truth questions", payload.totalQuestions], ["Active questions", payload.activeQuestions], ["Gemini", payload.geminiConfigured ? "Configured" : "Missing"], ["Embeddings", payload.embeddingConfigured ? "Configured" : "Missing"]];
        kpis.replaceChildren(...values.map(([label, value]) => {
            const card = element("article");
            card.append(element("span", "", label), element("strong", "", value), element("small", "", "Live database"));
            return card;
        }));
        tbody.replaceChildren();
        (payload.recentRuns ?? []).forEach(item => {
            const row = element("tr");
            const idCell = element("td");
            idCell.append(element("strong", "", item.id.slice(0, 8)), element("br"), element("small", "", formatDate(item.completedAtUtc)));
            const status = element("span", "ui-badge ui-badge-success");
            status.append(element("i"), document.createTextNode("Completed"));
            const statusCell = element("td"); statusCell.append(status);
            const actionCell = element("td");
            const action = element("a", "ui-button ui-button-secondary", "Open in app");
            action.href = `/backend/Benchmark/Details/${encodeURIComponent(item.id)}`;
            actionCell.append(action);
            row.append(idCell, element("td", "", `${item.chunkingStrategy} · top ${item.topK}`), element("td", "", (item.hitRate * 100).toFixed(1)), element("td", "", `${Math.round(item.averageLatencyMilliseconds)} ms`), statusCell, actionCell);
            tbody.append(row);
        });
    };

    const renderUsers = (payload) => {
        const tbody = document.querySelector("[data-live-users]");
        if (!tbody) return;
        tbody.replaceChildren();
        (payload.users ?? []).forEach(user => {
            const row = element("tr", user.isLockedOut ? "account-locked" : "");
            const identity = element("td");
            const initials = user.displayName.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]).join("").toUpperCase() || "U";
            const copy = element("div"); copy.append(element("strong", "", user.displayName), element("small", "", user.email));
            identity.append(element("span", "ui-avatar", initials), copy);
            const role = element("span", `role-pill role-${user.role.toLowerCase()}`, user.role);
            const roleCell = element("td"); roleCell.append(role);
            const status = element("span", `ui-badge ${user.isLockedOut ? "ui-badge-danger" : "ui-badge-success"}`);
            status.append(element("i"), document.createTextNode(user.isLockedOut ? "Locked" : "Active"));
            const statusCell = element("td"); statusCell.append(status);
            const actionCell = element("td");
            const action = element("a", "ui-button ui-button-secondary", "Manage in app");
            action.href = "/backend/AdminUsers";
            actionCell.append(action);
            row.append(identity, roleCell, element("td", "", "Database account"), element("td", "", "—"), statusCell, actionCell);
            tbody.append(row);
        });
    };

    const renderDocument = (documentItem) => {
        const title = document.querySelector("[data-live-document-title]");
        const meta = document.querySelector("[data-live-document-meta]");
        const summary = document.querySelector("[data-live-document-summary]");
        const extracted = document.querySelector("[data-live-extracted]");
        const chunks = document.querySelector("[data-live-document-chunks]");
        if (title) title.textContent = documentItem.originalFileName;
        if (meta) meta.textContent = `${documentItem.courseCode ?? "No course"} · ${documentItem.chapterTitle ?? "No chapter"} · ${documentItem.fileType} · ${formatBytes(documentItem.fileSizeBytes)}`;
        if (summary) {
            summary.replaceChildren(element("h2", "", "Database document summary"), element("p", "", documentItem.contentText.slice(0, 600) || "No extracted text."));
            const list = element("dl");
            [["Characters", documentItem.contentText.length], ["Chunks", documentItem.chunks.length], ["Chunking", documentItem.chunkingStrategy], ["Uploaded by", documentItem.uploadedByEmail ?? "—"]].forEach(([label, value]) => {
                const item = element("div"); item.append(element("dt", "", label), element("dd", "", value)); list.append(item);
            });
            summary.append(list);
        }
        if (extracted) extracted.replaceChildren(element("h2", "", "Extracted database text"), element("p", "", documentItem.contentText || "No extracted text."));
        if (chunks) {
            chunks.replaceChildren();
            documentItem.chunks.forEach(chunk => {
                const article = element("article");
                const copy = element("div"); copy.append(element("strong", "", `Chunk ${chunk.chunkIndex}`), element("p", "", chunk.content));
                article.append(element("span", "", `#${String(chunk.chunkIndex).padStart(2, "0")}`), copy, element("small", "", `${chunk.content.length} chars`));
                chunks.append(article);
            });
        }
    };

    const run = async () => {
        try {
            const identity = await adapter.me();
            showLiveIdentity(identity);
            switch (page) {
                case "chat": await startChat(); break;
                case "documents": renderDocuments(await adapter.documents()); break;
                case "documentdetails": {
                    const id = query.get("id");
                    if (!id) throw new adapter.LiveAdapterError("Choose a database document from the live library first.", 400);
                    renderDocument(await adapter.document(id));
                    break;
                }
                case "courses": renderCourses(await adapter.courses()); break;
                case "benchmark": renderBenchmark(await adapter.benchmark()); break;
                case "admin": renderUsers(await adapter.users()); break;
            }
        } catch (error) {
            const status = error instanceof adapter.LiveAdapterError ? error.status : 0;
            adapter.mode = status === 401 ? "sign-in-required" : status === 403 ? "forbidden" : "unavailable";
            adapter.setStatus(status === 401 ? "Sign in required" : status === 403 ? "Access denied" : "Live unavailable", status === 403 ? "danger" : "warning");
            showBanner(error.message || "Live data could not be loaded.", status === 403 ? "danger" : "warning", status === 401);
        }
    };

    run();
})();
