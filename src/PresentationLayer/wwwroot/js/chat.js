(function () {
  const sessionId = document.getElementById("sessionId").textContent.trim();
  const messages = document.getElementById("messages");
  const form = document.getElementById("chatForm");
  const input = document.getElementById("messageInput");
  const sendButton = document.getElementById("sendButton");
  const clearButton = document.getElementById("clearButton");
  const courseSelect = document.getElementById("courseId");
  const sessionList = document.getElementById("sessionList");
  const sessionSearch = document.getElementById("sessionSearch");
  const chatEmptyHint = document.getElementById("chatEmptyHint");

  function updateEmptyHint() {
    if (!chatEmptyHint) return;
    chatEmptyHint.hidden = messages.querySelector(".message:not(.typing)") !== null;
  }

  function modelType() {
    return document.querySelector("input[name='modelType']:checked").value;
  }

  function parseUtc(dateStr) {
    if (!dateStr) return new Date(0);
    // Treat as UTC when server omits timezone suffix
    if (!dateStr.endsWith("Z") && !/[+-]\d{2}:\d{2}$/.test(dateStr)) {
      return new Date(dateStr + "Z");
    }
    return new Date(dateStr);
  }

  function formatTime(dateStr) {
    return parseUtc(dateStr).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  }

  function formatRelativeTime(dateStr) {
    const diffMs = Date.now() - parseUtc(dateStr).getTime();
    const mins = Math.floor(diffMs / 60000);
    if (mins < 1) return "Just now";
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }

  function buildMeta(message) {
    const time = formatTime(message.createdAtUtc);
    if (message.role === "user") return `You · ${time}`;
    let meta = `Assistant · ${time}`;
    if (message.processingSeconds != null) {
      meta += ` · ${message.processingSeconds.toFixed(1)}s`;
    }
    return meta;
  }

  // --- Markdown renderer (safe, no external lib) ---
  function escapeHtml(s) {
    return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
  }
  function renderMarkdown(text) {
    let html = escapeHtml(text);
    html = html.replace(/`([^`\n]+)`/g, "<code>$1</code>");
    html = html.replace(/\*\*([^*\n]+)\*\*/g, "<strong>$1</strong>");
    html = html.replace(/\*([^*\n]+)\*/g, "<em>$1</em>");
    html = html.replace(/^\s*[-*]\s+(.+)$/gm, "<li>$1</li>");
    html = html.replace(/(<li>[\s\S]*?<\/li>)/g, "<ul>$1</ul>");
    html = html.replace(/\n/g, "<br>");
    return html;
  }

  // --- Typing indicator ---
  let typingEl = null;
  let longWaitTimer = null;
  let longWaitEl = null;

  function clearLongWait() {
    if (longWaitTimer) {
      clearTimeout(longWaitTimer);
      longWaitTimer = null;
    }
    if (longWaitEl) {
      longWaitEl.remove();
      longWaitEl = null;
    }
  }

  function showTyping() {
    if (typingEl) return;
    typingEl = document.createElement("div");
    typingEl.className = "message assistant typing";
    typingEl.innerHTML = '<div class="typing-dots"><span></span><span></span><span></span></div>';
    messages.appendChild(typingEl);
    messages.scrollTop = messages.scrollHeight;

    longWaitTimer = setTimeout(() => {
      if (!typingEl || longWaitEl) return;
      longWaitEl = document.createElement("div");
      longWaitEl.className = "typing-status";
      longWaitEl.textContent = "Searching documents and generating answer…";
      typingEl.appendChild(longWaitEl);
    }, 3000);
  }

  function hideTyping() {
    clearLongWait();
    if (typingEl) {
      typingEl.remove();
      typingEl = null;
    }
  }

  // --- Optimistic user message ---
  let optimisticEl = null;

  function clearOptimistic() {
    if (optimisticEl) {
      optimisticEl.remove();
      optimisticEl = null;
    }
  }

  function renderOptimisticUserMessage(text) {
    clearOptimistic();
    optimisticEl = document.createElement("div");
    optimisticEl.className = "message user optimistic";
    optimisticEl.dataset.optimistic = "true";

    const meta = document.createElement("div");
    meta.className = "message-meta";
    meta.textContent = `You · ${formatTime(new Date().toISOString())}`;

    const body = document.createElement("div");
    body.textContent = text;

    optimisticEl.appendChild(meta);
    optimisticEl.appendChild(body);
    messages.appendChild(optimisticEl);
    messages.scrollTop = messages.scrollHeight;
    updateEmptyHint();
  }

  // --- Render message ---
  function renderMessage(message, options = {}) {
    const wrapper = document.createElement("div");
    wrapper.className = `message ${message.role === "user" ? "user" : "assistant"}`;
    if (message.id) wrapper.dataset.messageId = message.id;

    const meta = document.createElement("div");
    meta.className = "message-meta";
    meta.textContent = options.metaOverride ?? buildMeta(message);
    wrapper.appendChild(meta);

    const body = document.createElement("div");
    body.innerHTML = renderMarkdown(message.content);
    wrapper.appendChild(body);

    if (message.error) {
      const error = document.createElement("div");
      error.className = "text-danger small mt-2";
      error.textContent = message.error;
      wrapper.appendChild(error);
    }

    if (message.citations && message.citations.length > 0) {
      for (const citation of message.citations) {
        const cite = document.createElement("span");
        cite.className = "citation";
        cite.textContent = `${citation.sourceName} / chunk #${citation.chunkIndex}: ${citation.text.substring(0, 220)}...`;
        wrapper.appendChild(cite);
      }
    }

    messages.appendChild(wrapper);
    messages.scrollTop = messages.scrollHeight;
    updateEmptyHint();
    return wrapper;
  }

  async function loadHistory() {
    const response = await fetch(`/api/chat/${sessionId}`);
    const data = await response.json();
    messages.querySelectorAll(".message").forEach((el) => el.remove());
    for (const message of data.history) {
      renderMessage(message);
    }
    updateEmptyHint();
  }

  // --- Session list ---
  async function loadSessions() {
    if (!sessionList) return;
    try {
      const res = await fetch("/api/chat/sessions");
      const data = await res.json();
      sessionList.innerHTML = "";
      for (const s of data.sessions) {
        const li = document.createElement("li");
        li.className = "list-group-item session-item" + (s.id === sessionId ? " active" : "");

        const link = document.createElement("a");
        link.className = "session-link text-decoration-none";
        link.href = `/chat?sessionId=${s.id}`;
        link.title = s.title;

        const titleEl = document.createElement("span");
        titleEl.className = "session-title";
        titleEl.textContent = s.title;

        const timeEl = document.createElement("span");
        timeEl.className = "session-time";
        timeEl.textContent = formatRelativeTime(s.updatedAtUtc);

        link.appendChild(titleEl);
        link.appendChild(timeEl);

        const delBtn = document.createElement("button");
        delBtn.className = "session-del";
        delBtn.type = "button";
        delBtn.textContent = "✕";
        delBtn.title = "Delete session";
        delBtn.addEventListener("click", async (e) => {
          e.preventDefault();
          e.stopPropagation();
          if (!confirm("Delete this session?")) return;
          await fetch(`/api/chat/${s.id}`, { method: "DELETE" });
          if (s.id === sessionId) {
            window.location.href = "/chat";
          } else {
            loadSessions();
          }
        });

        li.appendChild(link);
        li.appendChild(delBtn);
        sessionList.appendChild(li);
      }
    } catch { /* ignore */ }
  }

  // --- Session search ---
  if (sessionSearch) {
    sessionSearch.addEventListener("input", () => {
      const q = sessionSearch.value.trim().toLowerCase();
      sessionList.querySelectorAll(".session-item").forEach((li) => {
        const title = li.querySelector(".session-title")?.textContent.toLowerCase() ?? "";
        li.hidden = q !== "" && !title.includes(q);
      });
    });
  }

  // --- SignalR ---
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

  connection.on("MessageReceived", (message) => {
    if (message.role === "user") {
      clearOptimistic();
      hideTyping();          // remove indicator before appending user msg
      renderMessage(message);
      showTyping();          // re-add indicator below user msg
      loadSessions();
      return;
    }

    hideTyping();
    renderMessage(message);
    input.disabled = false;
    sendButton.disabled = false;
    input.focus();
    loadSessions();
  });

  connection.on("MessageFailed", (message) => {
    hideTyping();
    clearOptimistic();
    input.disabled = false;
    sendButton.disabled = false;
    const err = document.createElement("div");
    err.className = "message assistant";
    err.innerHTML = `<div class="text-danger">${escapeHtml(message)}</div>`;
    messages.appendChild(err);
    messages.scrollTop = messages.scrollHeight;
  });

  connection.on("SessionCleared", () => {
    messages.querySelectorAll(".message").forEach((el) => el.remove());
    clearOptimistic();
    updateEmptyHint();
  });

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!window._geminiConfigured) return;
    const text = input.value.trim();
    if (!text) return;

    input.value = "";
    input.disabled = true;
    sendButton.disabled = true;
    renderOptimisticUserMessage(text);
    showTyping();

    try {
      await connection.invoke("SendMessage", sessionId, courseSelect.value, modelType(), text);
    } catch {
      hideTyping();
      clearOptimistic();
      input.disabled = false;
      sendButton.disabled = false;
    }
  });

  clearButton.addEventListener("click", async () => {
    if (confirm("Clear the current session history?")) {
      await connection.invoke("ClearSession", sessionId);
    }
  });

  connection.start()
    .then(() => connection.invoke("JoinSession", sessionId))
    .then(loadHistory)
    .then(loadSessions)
    .catch((error) => {
      const errEl = document.createElement("div");
      errEl.className = "alert alert-danger m-3";
      errEl.textContent = "Connection error: " + error.toString();
      messages.appendChild(errEl);
    });
})();
