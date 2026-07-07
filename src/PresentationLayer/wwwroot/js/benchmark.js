(function () {
  const runBtn = document.getElementById("runBenchmark");
  const fullBtn = document.getElementById("runFullBenchmark");
  const confirmBtn = document.getElementById("confirmRun");
  const status = document.getElementById("benchmarkStatus");
  const runOptions = document.getElementById("runOptions");

  // Color palette
  const colors = {
    faith: "rgba(99, 102, 241, 0.8)",
    rel: "rgba(16, 185, 129, 0.8)",
    recall: "rgba(245, 158, 11, 0.8)",
    cite: "rgba(239, 68, 68, 0.8)",
    rag: "rgba(99, 102, 241, 0.8)",
    ft: "rgba(245, 158, 11, 0.8)"
  };

  function createBarChart(canvasId, labels, datasets) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || labels.length === 0) return;

    new Chart(canvas, {
      type: "bar",
      data: { labels, datasets },
      options: {
        responsive: true,
        scales: {
          y: { beginAtZero: true, max: 1.0, ticks: { stepSize: 0.2 } }
        },
        plugins: {
          legend: { position: "bottom" }
        }
      }
    });
  }

  // Render charts if data exists
  if (typeof modelLabels !== "undefined" && modelLabels.length > 0) {
    createBarChart("chartByModel", modelLabels, [
      { label: "Faithfulness", data: modelFaith, backgroundColor: colors.faith },
      { label: "Relevance", data: modelRel, backgroundColor: colors.rel },
      { label: "Recall", data: modelRecall, backgroundColor: colors.recall },
      { label: "Citation", data: modelCite, backgroundColor: colors.cite }
    ]);
  }

  if (typeof stratLabels !== "undefined" && stratLabels.length > 0) {
    createBarChart("chartByStrategy", stratLabels, [
      { label: "Faithfulness", data: stratFaith, backgroundColor: colors.faith },
      { label: "Relevance", data: stratRel, backgroundColor: colors.rel },
      { label: "Recall", data: stratRecall, backgroundColor: colors.recall },
      { label: "Citation", data: stratCite, backgroundColor: colors.cite }
    ]);
  }

  // RAG vs FT chart
  if (typeof ragFaith !== "undefined") {
    createBarChart("chartRagVsFt", ["Faithfulness", "Answer Relevance"], [
      { label: "RAG", data: [ragFaith, ragRel], backgroundColor: colors.rag },
      { label: "Fine-tuned", data: [ftFaith, ftRel], backgroundColor: colors.ft }
    ]);
  }

  // Toggle run options panel
  if (runBtn) {
    runBtn.addEventListener("click", () => {
      runOptions.classList.toggle("d-none");
    });
  }

  // Confirm run with options
  if (confirmBtn) {
    confirmBtn.addEventListener("click", async () => {
      const limit = parseInt(document.getElementById("questionLimit").value) || 5;
      const chunkingStrategy = document.getElementById("chunkingStrategy").value || null;
      const embeddingModel = document.getElementById("embeddingModel").value || null;

      confirmBtn.disabled = true;
      runBtn.disabled = true;
      showStatus("info", `Running benchmark: ${limit} questions...`);

      try {
        const response = await fetch("/api/evaluations/run", {
          method: "POST",
          headers: window.requestVerificationHeaders({ "Content-Type": "application/json" }),
          body: JSON.stringify({ limit, chunkingStrategy, embeddingModel })
        });
        const data = await response.json();
        if (!data.success) throw new Error(data.error || "Benchmark failed");

        showStatus("success", `Completed: ${data.count} evaluations. Reloading...`);
        setTimeout(() => window.location.reload(), 1500);
      } catch (error) {
        showStatus("danger", error.toString());
      } finally {
        confirmBtn.disabled = false;
        runBtn.disabled = false;
      }
    });
  }

  // Full benchmark (all strategies × all models)
  if (fullBtn) {
    fullBtn.addEventListener("click", async () => {
      const limit = parseInt(prompt("Number of questions per combination (1-50):", "5"));
      if (!limit || limit < 1 || limit > 50) return;

      fullBtn.disabled = true;
      runBtn.disabled = true;
      showStatus("info", "Starting benchmark in background...");

      try {
        const startResp = await fetch("/api/evaluations/run-full", {
          method: "POST",
          headers: window.requestVerificationHeaders({ "Content-Type": "application/json" }),
          body: JSON.stringify({ questionLimit: limit })
        });
        const startData = await startResp.json();
        if (!startData.success) throw new Error(startData.error || "Could not start benchmark.");

        // Poll progress every 3 seconds
        const pollInterval = setInterval(async () => {
          try {
            const progResp = await fetch("/api/evaluations/progress");
            const prog = await progResp.json();
            const label = prog.total > 0 ? ` (${prog.done}/${prog.total} — ${prog.percent}%)` : "";
            if (prog.error) {
              clearInterval(pollInterval);
              showStatus("danger", `Benchmark failed: ${prog.error}`);
              fullBtn.disabled = false;
              runBtn.disabled = false;
            } else if (!prog.running) {
              clearInterval(pollInterval);
              showStatus("success", `Full benchmark completed${label}. Reloading...`);
              setTimeout(() => window.location.reload(), 1500);
            } else {
              showStatus("info", `Benchmark running${label}... do not close this tab.`);
            }
          } catch { /* network blip, keep polling */ }
        }, 3000);
      } catch (error) {
        showStatus("danger", error.toString());
        fullBtn.disabled = false;
        runBtn.disabled = false;
      }
    });
  }

  function showStatus(type, message) {
    status.classList.remove("d-none", "alert-info", "alert-success", "alert-danger");
    status.classList.add(`alert-${type}`);
    status.textContent = message;
  }
})();
