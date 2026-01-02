// /js/Site/siteTomSelect.js
document.addEventListener("DOMContentLoaded", () => {
  // csak modal selecteknél
  function ensureTomSelect(selectEl, loadUrlBuilder) {
    if (!selectEl) return;
    if (!window.TomSelect) {
      console.warn("TomSelect nincs betöltve");
      return;
    }
    if (selectEl.tomselect) return;

    new TomSelect(selectEl, {
      valueField: "id",
      labelField: "text",
      searchField: ["text"],
      maxItems: 1,
      maxOptions: 50,
      create: false,
      allowEmptyOption: true,
      closeAfterSelect: true,
      dropdownParent: "body",
      placeholder: selectEl.getAttribute("data-placeholder") || "Válasszon...",
      load: async (query, cb) => {
        try {
          const url = loadUrlBuilder(query);
          const res = await fetch(url, { credentials: "same-origin", headers: { "Accept": "application/json" } });
          if (!res.ok) throw new Error(`HTTP ${res.status}`);
          const data = await res.json();
          cb(data);
        } catch (e) {
          console.error("Partner load error:", e);
          cb([]);
        }
      }
    });
  }

  function refreshTomSelect(selectEl) {
    if (!selectEl?.tomselect) return;
    selectEl.tomselect.sync();
    selectEl.tomselect.refreshOptions(false);
  }

  // Create modal
  const createModal = document.getElementById("createSiteModal");
  if (createModal) {
    createModal.addEventListener("shown.bs.modal", () => {
      const partnerSelect = document.getElementById("createPartnerId");
      ensureTomSelect(partnerSelect, (q) => `/api/partners/select?search=${encodeURIComponent(q || "")}`);
      setTimeout(() => refreshTomSelect(partnerSelect), 50);
    });
  }

  // Edit modal (ha 1 darab edit modalod van, nem siteId-s)
  const editModal = document.getElementById("editSiteModal");
  if (editModal) {
    editModal.addEventListener("shown.bs.modal", () => {
      const partnerSelect = document.getElementById("editPartnerId");
      ensureTomSelect(partnerSelect, (q) => `/api/partners/select?search=${encodeURIComponent(q || "")}`);
      setTimeout(() => refreshTomSelect(partnerSelect), 50);
    });
  }

  // Ha még több edit modalod van (pl. id="editSiteModal_123"), akkor:
  document.addEventListener("shown.bs.modal", (e) => {
    const modalEl = e.target;
    if (!modalEl?.id?.startsWith("editSiteModal_")) return;

    const partnerSelect = modalEl.querySelector('select[name="partnerId"]');
    ensureTomSelect(partnerSelect, (q) => `/api/partners/select?search=${encodeURIComponent(q || "")}`);
    setTimeout(() => refreshTomSelect(partnerSelect), 50);
  });
});
