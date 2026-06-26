(function () {
  const sessionId = document.getElementById("sessionId").textContent.trim();
  const messages = document.getElementById("messages");
  const form = document.getElementById("chatForm");
  const input = document.getElementById("messageInput");
  const sendButton = document.getElementById("sendButton");
  const clearButton = document.getElementById("clearButton");
  const courseSelect = document.getElementById("courseId");
  const sessionList = document.getElementById("sessionList");

  function modelType() {
    return document.querySelector("input[name='modelType']:checked").value;
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
  function showTyping() {
    if (typingEl) return;
    typingEl = document.createElement("div");
    typingEl.className = "message assistant typing";
    typingEl.innerHTML = '<div class="typing-dots"><span></span><span></span><span></span></div>';
    messages.appendChild(typingEl);
    messages.scrollTop = messages.scrollHeight;
  }
  function hideTyping() {
    if (typingEl) { typingEl.remove(); typingEl = null; }
  }

  // --- Render message ---
  function renderMessage(message) {
    hideTyping();
    const wrapper = document.createElement("div");
    wrapper.className = `message ${message.role === "user" ? "user" : "assistant"}`;

    const meta = document.createElement("div");
    meta.className = "message-meta";
    meta.textContent = `${message.role} / ${message.modelType} / ${new Date(message.createdAtUtc).toLocaleTimeString()}`;
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
  }

  async function loadHistory() {
    const response = await fetch(`/api/chat/${sessionId}`);
    const data = await response.json();
    messages.innerHTML = "";
    for (const message of data.history) {
      renderMessage(message);
    }
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
        li.className = "list-group-item" + (s.id === sessionId ? " active" : "");
        const titleEl = document.createElement("a");
        titleEl.className = "session-title text-decoration-none" + (s.id === sessionId ? " text-white" : "");
        titleEl.href = `/chat?sessionId=${s.id}`;
        titleEl.textContent = s.title;
        const delBtn = document.createElement("button");
        delBtn.className = "session-del";
        delBtn.textContent = "✕";
        delBtn.title = "Delete session";
        delBtn.addEventListener("click", async (e) => {
          e.preventDefault();
          if (!confirm("Delete this session?")) return;
          await fetch(`/api/chat/${s.id}`, { method: "DELETE" });
          if (s.id === sessionId) { window.location.href = "/chat"; }
          else { loadSessions(); }
        });
        li.appendChild(titleEl);
        li.appendChild(delBtn);
        sessionList.appendChild(li);
      }
    } catch { /* ignore */ }
  }

  // --- SignalR ---
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

  connection.on("MessageReceived", (message) => {
    renderMessage(message);
    if (message.role !== "user") {
      input.disabled = false;
      sendButton.disabled = false;
      input.focus();
      loadSessions();
    }
  });
  connection.on("MessageFailed", message => {
    hideTyping();
    input.disabled = false;
    sendButton.disabled = false;
    const err = document.createElement("div");
    err.className = "message assistant";
    err.innerHTML = `<div class="text-danger">${escapeHtml(message)}</div>`;
    messages.appendChild(err);
    messages.scrollTop = messages.scrollHeight;
  });
  connection.on("SessionCleared", () => { messages.innerHTML = ""; });

  form.addEventListener("submit", async event => {
    event.preventDefault();
    if (!window._geminiConfigured) return;
    const text = input.value.trim();
    if (!text) return;

    input.value = "";
    input.disabled = true;
    sendButton.disabled = true;
    showTyping();
    await connection.invoke("SendMessage", sessionId, courseSelect.value, modelType(), text);
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
    .catch(error => {
      const errEl = document.createElement("div");
      errEl.className = "alert alert-danger m-3";
      errEl.textContent = "Connection error: " + error.toString();
      messages.appendChild(errEl);
    });
})();
