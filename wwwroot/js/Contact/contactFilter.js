// wwwroot/js/Contact/contactFilter.js
// PartnerFilter.js mintára (ugyanazok az ID-ket várja):
// - tbody:            #contactsTableBody
// - load more btn:    #loadMoreBtn
// - spinner:          #loadMoreSpinner
// - container:        #loadMoreContainer
// - search input:     #searchInput
// - filter links:     .dropdown-menu a[data-filter]
document.addEventListener("DOMContentLoaded", () => {
  const tbody = document.getElementById("contactsTableBody");

  // Partner-minta ID-k
  const loadMoreBtn = document.getElementById("loadMoreBtn");
  const loadMoreSpinner = document.getElementById("loadMoreSpinner");
  const loadMoreContainer = document.getElementById("loadMoreContainer");

  const searchInput = document.getElementById("searchInput");

  if (!tbody) return;

  let currentPage = 1;

  // Egységesen a partner oldallal (ott 50)
  const pageSize = 20;

  let isLoading = false;
  let hasMore = true;

  // Contact oldalon most basic filter van: '', 'active', 'inactive'
  const filters = {
    search: "",
    filter: ""
  };

  /* ---------------- API ---------------- */

  function updateLoadMoreText(total) {
  if (!loadMoreBtn) return;

  const rendered = tbody.querySelectorAll("tr[data-contact-id]").length; // csak a valós sorok
  const remaining = Math.max(0, total - rendered);

  if (remaining <= 0) {
    loadMoreBtn.textContent = "Nincs több találat";
    loadMoreBtn.disabled = true;
    return;
  }

  // következő kattintással ennyit fog betölteni
  const nextBatch = Math.min(pageSize, remaining);

  // Példa: "Több betöltése (50 / 120) – betöltve: 100"
  loadMoreBtn.textContent = `Betöltve: ${rendered}/${total}`;
  loadMoreBtn.disabled = false;
}


  function buildUrl(page) {
    const p = new URLSearchParams({
      pageNumber: String(page),
      pageSize: String(pageSize)
    });

    if (filters.search) p.append("search", filters.search);
    if (filters.filter) p.append("filter", filters.filter);

    return `/api/Contact?${p.toString()}`;
  }

  async function loadContacts(reset = false) {
    if (isLoading) return;
    isLoading = true;

    loadMoreSpinner?.classList.remove("d-none");

    if (reset) {
      currentPage = 1;
      hasMore = true;

      tbody.innerHTML = `
        <tr>
          <td colspan="8" class="text-center py-5 text-muted">
            Betöltés...
          </td>
        </tr>`;
    }

    try {
      const res = await fetch(buildUrl(currentPage), {
        credentials: "same-origin",
        headers: { Accept: "application/json" }
      });

      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const total = parseInt(res.headers.get("X-Total-Count") || "0", 10);
      const data = await res.json();

      if (reset) tbody.innerHTML = "";

      if (Array.isArray(data)) data.forEach(addRow);

    hasMore = currentPage * pageSize < total;
    if (loadMoreContainer) loadMoreContainer.classList.remove("d-none");
    if (!hasMore) {
    loadMoreBtn.disabled = true;
    loadMoreBtn.textContent = "Nincs több találat";
    }

    updateLoadMoreText(total);


      // Üres állapot (partner mintára)
      if (reset && total === 0) {
        tbody.innerHTML = `
          <tr>
            <td colspan="8" class="text-center py-5 text-muted">
              Nincs találat.
            </td>
          </tr>`;
      }
    } catch (err) {
      console.error("Contact load error:", err);
      tbody.innerHTML = `
        <tr>
          <td colspan="8" class="text-center text-danger py-5">
            Hiba a kontaktok betöltésekor
          </td>
        </tr>`;
      loadMoreContainer?.classList.add("d-none");
    } finally {
      isLoading = false;
      loadMoreSpinner?.classList.add("d-none");
    }
  }

  function addRow(c) {
    // DTO (PascalCase), de safe camelCase fallback
    const id = c.contactId ?? c.ContactId;

    const firstName = c.firstName ?? c.FirstName ?? "";
    const lastName = c.lastName ?? c.LastName ?? "";
    const email = c.email ?? c.Email ?? "";
    const phone1 = c.phoneNumber ?? c.PhoneNumber ?? "";
    const phone2 = c.phoneNumber2 ?? c.PhoneNumber2 ?? "";
    const jobTitle = c.jobTitle ?? c.JobTitle ?? "";

    const statusObj = c.status ?? c.Status;
    const statusName =
      (typeof statusObj === "string" ? statusObj : statusObj?.name ?? statusObj?.Name) ??
      c.statusName ??
      c.StatusName ??
      "N/A";

    const partnerId = c.partnerId ?? c.PartnerId;

    const created = c.createdDate ?? c.CreatedDate;
    const updated = c.updatedDate ?? c.UpdatedDate;

    const badgeClass =
      statusName === "Aktív"
        ? "badge bg-success"
        : statusName === "Inaktív"
        ? "badge bg-secondary"
        : "badge bg-secondary";

    const partnerBadge = partnerId
      ? `<span class="badge bg-info ms-1" title="Hozzárendelve partnerhez">Partner</span>`
      : "";

    tbody.insertAdjacentHTML(
      "beforeend",
      `
<tr data-contact-id="${escapeHtml(id)}">
  <td class="text-nowrap">${escapeHtml(firstName)} ${escapeHtml(lastName)}</td>
  <td class="text-nowrap"><i class="bi bi-envelope me-1"></i>${escapeHtml(email || "")}</td>
  <td><i class="bi bi-telephone me-1"></i>${escapeHtml(phone1 || "")}${phone2 ? "; " + escapeHtml(phone2) : ""}</td>
  <td class="text-nowrap"><i class="bi bi-briefcase me-1"></i>${escapeHtml(jobTitle || "")}</td>
  <td class="text-nowrap">
    <span class="${badgeClass}">${escapeHtml(statusName)}</span>${partnerBadge}
  </td>
  <td class="text-nowrap">${formatDateTime(created)}</td>
  <td class="text-nowrap">${updated ? formatDateTime(updated) : "N/A"}</td>
  <td>
    <div class="btn-group btn-group-sm" role="group">
      <button type="button"
              class="btn btn-outline-info"
              data-bs-toggle="modal"
              data-bs-target="#viewContactModal"
              data-contact-id="${escapeHtml(id)}">
        <i class="bi bi-eye"></i>
      </button>

      <div class="dropdown">
        <button class="btn btn-outline-secondary dropdown-toggle btn-sm"
                type="button"
                data-bs-toggle="dropdown">
          <i class="bi bi-three-dots-vertical"></i>
        </button>

        <ul class="dropdown-menu dropdown-menu-end">
          <li>
            <a class="dropdown-item"
               href="#"
               data-bs-toggle="modal"
               data-bs-target="#editContactModal"
               data-contact-id="${escapeHtml(id)}">
              <i class="bi bi-pencil-square me-2"></i>Szerkesztés
            </a>
          </li>

          <li><hr class="dropdown-divider"></li>

          <li>
            <a class="dropdown-item text-danger"
               href="#"
               data-bs-toggle="modal"
               data-bs-target="#deleteContactModal"
               data-contact-id="${escapeHtml(id)}"
               data-contact-name="${escapeHtml(firstName)} ${escapeHtml(lastName)}">
              <i class="bi bi-trash me-2"></i>Törlés
            </a>
          </li>
        </ul>
      </div>
    </div>
  </td>
</tr>
      `
    );
  }

  function formatDateTime(val) {
    if (!val) return "N/A";
    const d = new Date(val);
    if (Number.isNaN(d.getTime())) return String(val);
    const pad = (n) => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(
      d.getHours()
    )}:${pad(d.getMinutes())}`;
  }

  function escapeHtml(s) {
    return String(s ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  /* ---------------- EVENTS ---------------- */

  // Keresés (partner mintára)
  searchInput?.addEventListener(
    "input",
    debounce((e) => {
      filters.search = e.target.value.trim();
      loadContacts(true);
    }, 400)
  );

  // Szűrő dropdown (data-filter)
  document.querySelectorAll(".dropdown-menu a[data-filter]").forEach((a) => {
    a.addEventListener("click", (e) => {
      e.preventDefault();
      filters.filter = (a.dataset.filter || "").trim();
      loadContacts(true);
    });
  });

  // Load more (partner mintára)
  loadMoreBtn?.addEventListener("click", () => {
    if (!hasMore) return;
    currentPage++;
    loadContacts(false);
  });

  /* ---------------- INIT ---------------- */

  // Ha a HTML még nem partner-minta ID-ket használ:
  // loadMoreBtn = null -> nem fog működni, ezért ezt logoljuk.
  if (!loadMoreBtn) {
    console.warn(
      'Hiányzik a "loadMoreBtn" (id="loadMoreBtn"). A gombod valószínűleg id="loadMoreContactsBtn". ' +
        "Egységesítsd az ID-ket a partner oldallal."
    );
  }

  loadContacts(true);
});

function debounce(fn, delay) {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), delay);
  };
}
