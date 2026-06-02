(function () {
  const button = document.getElementById("runBenchmark");
  const status = document.getElementById("benchmarkStatus");

  if (!button) {
    return;
  }

  button.addEventListener("click", async () => {
    button.disabled = true;
    status.classList.remove("d-none", "alert-danger");
    status.classList.add("alert-info");
    status.textContent = "Running benchmark for 5 questions...";

    try {
      const response = await fetch("/api/evaluations/run", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ limit: 5 })
      });
      const data = await response.json();
      if (!data.success) {
        throw new Error("Benchmark failed");
      }

      status.textContent = `Ran ${data.count} questions. Reload the page to see the latest results.`;
    } catch (error) {
      status.classList.remove("alert-info");
      status.classList.add("alert-danger");
      status.textContent = error.toString();
    } finally {
      button.disabled = false;
    }
  });
})();
