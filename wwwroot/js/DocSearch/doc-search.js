(() => {
  "use strict";

  const MIN_CHARS = 2;
  const DEBOUNCE_MS = 250;
  const API_URL = "/api/documents/search";
  const TAKE = 20;

  const searchBox = document.getElementById("searchBox");
  const resultsContainer = document.getElementById("resultsContainer");
  const dropdown = document.getElementById("dropdownResults");

  if (!searchBox || !resultsContainer || !dropdown) return;

  let debounceTimer = null;
  let abortController = null;
  let lastQuery = "";

  function clearUI() {
    resultsContainer.innerHTML = "";
    dropdown.innerHTML = "";
    dropdown.style.display = "none";
  }

  function setDropdown(html) {
    dropdown.innerHTML = html;
    dropdown.style.display = html.trim() ? "block" : "none";
  }

  function escapeHtml(s) {
    return (s ?? "").replace(/[&<>"']/g, (c) => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));
  }

  function renderResults(items) {
    if (!items || items.length === 0) {
      resultsContainer.innerHTML = `<div class="mt-3 text-muted">Nincs találat.</div>`;
      setDropdown(`<div class="dropdown-empty px-3 py-2">Nincs találat</div>`);
      return;
    }

    // Full results
    resultsContainer.innerHTML = `
      <ul>
        ${items.map(d => `
          <li>
          ${d.partnerName ? `<div><small class="text-muted">Partner: ${escapeHtml(d.partnerName)}</small></div>` : ""}
            <strong>${escapeHtml(d.fileName)}</strong><br/>
            <small class="text-muted">${escapeHtml(d.filePath)}</small><br/>
            ${d.metadata ? Object.entries(d.metadata).map(([k,v]) =>
              `<span>${escapeHtml(k)}: ${escapeHtml(v)}</span><br/>`
            ).join("") : ""}
          </li>
        `).join("")}
      </ul>
    `;

    // Dropdown preview
    setDropdown(items.slice(0, 8).map(d => `
      <div class="dropdown-item px-3 py-2" role="option" tabindex="-1" data-filename="${escapeHtml(d.fileName)}">
        ${escapeHtml(d.fileName)}
      </div>
    `).join(""));
  }

  async function fetchResults(query) {
    if (abortController) abortController.abort();
    abortController = new AbortController();

    const url = `${API_URL}?term=${encodeURIComponent(query)}&take=${TAKE}`;

    try {
      const res = await fetch(url, {
        method: "GET",
        headers: { "Accept": "application/json" },
        credentials: "same-origin",
        cache: "no-store",
        signal: abortController.signal
      });

      if (!res.ok) {
        resultsContainer.innerHTML = `<div class="alert alert-warning mt-3">Hiba a keresés közben (HTTP ${res.status}).</div>`;
        dropdown.style.display = "none";
        return;
      }

      const data = await res.json();
      renderResults(data);
    } catch (err) {
      if (err?.name === "AbortError") return;
      resultsContainer.innerHTML = `<div class="alert alert-danger mt-3">Hiba a keresés közben.</div>`;
      dropdown.style.display = "none";
    }
  }

  function scheduleSearch() {
    const q = (searchBox.value || "").trim();
    if (q === lastQuery) return;
    lastQuery = q;

    if (debounceTimer) clearTimeout(debounceTimer);

    debounceTimer = setTimeout(() => {
      if (q.length < MIN_CHARS) {
        clearUI();
        if (abortController) abortController.abort();
        return;
      }
      fetchResults(q);
    }, DEBOUNCE_MS);
  }

  searchBox.addEventListener("input", scheduleSearch);

  document.addEventListener("click", (e) => {
    const t = e.target;
    if (!dropdown.contains(t) && t !== searchBox) dropdown.style.display = "none";
  });

  dropdown.addEventListener("click", (e) => {
    const item = e.target.closest(".dropdown-item");
    if (!item) return;
    const fileName = item.getAttribute("data-filename");
    if (fileName) {
      searchBox.value = fileName;
      dropdown.style.display = "none";
      lastQuery = "";
      scheduleSearch();
    }
  });

  clearUI();
})();
