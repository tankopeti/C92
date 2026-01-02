// /js/Site/sitesSearch.js (vagy tedd a meglévő sites index JS-be)
document.addEventListener("DOMContentLoaded", () => {
  const input = document.getElementById("siteSearchInput");
  if (!input) return;

  let currentFilter = "all"; // all | primary
  let debounceTimer = null;

  // Állítsd be: hol van a táblád tbody-ja?
  // (cseréld ki a szelektort a sajátodra, ha kell)
  const tbody = document.querySelector("#sitesTable tbody") || document.querySelector("table tbody");
  if (!tbody) return;

  function escapeHtml(str) {
    return String(str ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  async function fetchAllSites({ search, filter }) {
    const pageSize = 200; // nagyobb = kevesebb kérés
    let pageNumber = 1;
    let all = [];

    while (true) {
      const url =
        `/api/SitesIndex?pageNumber=${pageNumber}` +
        `&pageSize=${pageSize}` +
        `&search=${encodeURIComponent(search || "")}` +
        `&filter=${encodeURIComponent(filter || "")}`;

      const res = await fetch(url, { credentials: "same-origin", headers: { Accept: "application/json" } });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const chunk = await res.json();
      if (!Array.isArray(chunk) || chunk.length === 0) break;

      all = all.concat(chunk);

      if (chunk.length < pageSize) break; // utolsó oldal
      pageNumber++;
    }

    return all;
  }

  function renderRows(rows) {
    tbody.innerHTML = "";

    for (const s of rows) {
      const statusColor = s.status?.color || "#6c757d";
      const statusName = s.status?.name || "—";

      const tr = document.createElement("tr");
      tr.dataset.siteId = s.siteId;

      tr.innerHTML = `
        <td><i class="bi bi-building me-1"></i>${escapeHtml(s.siteName || "—")}</td>
        <td>${escapeHtml(s.partnerName || "—")}</td>
        <td>${escapeHtml(s.addressLine1 || "—")}</td>
        <td>${escapeHtml(s.addressLine2 || "—")}</td>
        <td>${escapeHtml(s.city || "—")}</td>
        <td>${escapeHtml(s.postalCode || "—")}</td>
        <td>${escapeHtml(s.contactPerson1 || "—")}</td>
        <td>${escapeHtml(s.contactPerson2 || "—")}</td>
        <td>${escapeHtml(s.contactPerson3 || "—")}</td>
        <td>
          <span class="badge" style="background:${escapeHtml(statusColor)};color:white">
            ${escapeHtml(statusName)}
          </span>
        </td>
        <td>${s.isPrimary ? `<span class="badge bg-primary">Elsődleges</span>` : `<span>-</span>`}</td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-primary edit-site-btn" data-site-id="${s.siteId}">
            <i class="bi bi-pencil"></i>
          </button>
        </td>
      `;

      tbody.appendChild(tr);
    }
  }

  async function refresh() {
    const search = input.value.trim();
    const filter = currentFilter === "primary" ? "primary" : "";

    try {
      // opcionális: loading jelzés
      input.classList.add("opacity-75");

      const rows = await fetchAllSites({ search, filter });
      renderRows(rows);
    } catch (e) {
      console.error(e);
      window.c92?.showToast?.("error", e.message || "Hiba a keresés közben");
    } finally {
      input.classList.remove("opacity-75");
    }
  }

  // dropdown kezelés
  document.addEventListener("click", (e) => {
    const a = e.target.closest(".dropdown-item[data-filter]");
    if (!a) return;

    e.preventDefault();
    currentFilter = a.dataset.filter || "all";

    document.querySelectorAll(".dropdown-item[data-filter]").forEach(x => x.classList.remove("active"));
    a.classList.add("active");

    refresh();
  });

  // input debounce
  input.addEventListener("input", () => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(refresh, 300);
  });

  // ha a form még létezik, ne submitoljon
  const form = input.closest("form");
  form?.addEventListener("submit", (e) => {
    e.preventDefault();
    refresh();
  });

  // első betöltés: ha van default searchTerm, töltsön
  // refresh();
});
