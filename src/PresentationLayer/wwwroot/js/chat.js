(function () {
  const sessionId = document.getElementById("sessionId").textContent.trim();
  const messages = document.getElementById("messages");
  const form = document.getElementById("chatForm");
  const input = document.getElementById("messageInput");
  const clearButton = document.getElementById("clearButton");

  function modelType() {
    return document.querySelector("input[name='modelType']:checked").value;
  }

  function renderMessage(message) {
    const wrapper = document.createElement("div");
    wrapper.className = `message ${message.role === "user" ? "user" : "assistant"}`;

    const meta = document.createElement("div");
    meta.className = "message-meta";
    meta.textContent = `${message.role} / ${message.modelType} / ${new Date(message.createdAtUtc).toLocaleTimeString()}`;
    wrapper.appendChild(meta);

    const body = document.createElement("div");
    body.textContent = message.content;
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

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

  connection.on("MessageReceived", renderMessage);
  connection.on("MessageFailed", message => alert(message));
  connection.on("SessionCleared", () => {
    messages.innerHTML = "";
  });

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const text = input.value.trim();
    if (!text) {
      return;
    }

    input.value = "";
    await connection.invoke("SendMessage", sessionId, modelType(), text);
  });

  clearButton.addEventListener("click", async () => {
    if (confirm("Clear the current session history?")) {
      await connection.invoke("ClearSession", sessionId);
    }
  });

  connection.start()
    .then(() => connection.invoke("JoinSession", sessionId))
    .then(loadHistory)
    .catch(error => alert(error.toString()));
})();
