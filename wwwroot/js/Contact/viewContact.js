// /js/Contact/viewContact.js – Contact megtekintése (View modal) – partner stílusban
document.addEventListener("DOMContentLoaded", () => {
  document.addEventListener("click", async (e) => {
    // A listában a szem ikon buttonon data-bs-target="#viewContactModal" + data-contact-id van
    const btn =
      e.target.closest('[data-bs-target="#viewContactModal"][data-contact-id]') ||
      e.target.closest(".view-contact-btn"); // ha később akarsz ilyen class-t

    if (!btn) return;

    const contactId = btn.dataset.contactId;
    if (!contactId) {
      window.c92?.showToast?.("error", "Hiányzó Kontakt ID");
      return;
    }

    const modalEl = document.getElementById("viewContactModal");
    const contentEl = document.getElementById("viewContactContent"); // egységes konténer
    if (!modalEl || !contentEl) {
      console.error("viewContactModal vagy viewContactContent nem található");
      return;
    }

    // modal megnyitása
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();

    // footer link frissítése (PLACEHOLDER_CONTACT_ID)
    const detailsLink = document.getElementById("viewDetailsLink");
    if (detailsLink) {
      const href = detailsLink.getAttribute("href") || "";
      if (href.includes("PLACEHOLDER_CONTACT_ID")) {
        detailsLink.setAttribute(
          "href",
          href.replace("PLACEHOLDER_CONTACT_ID", encodeURIComponent(contactId))
        );
      }
    }

    contentEl.innerHTML = loadingHtml("Adatok betöltése...");

    try {
      const res = await fetch(`/api/Contact/${encodeURIComponent(contactId)}`, {
        credentials: "same-origin",
        headers: { Accept: "application/json" },
      });

      if (!res.ok) throw new Error(res.status === 404 ? "Kontakt nem található" : `HTTP ${res.status}`);

      const d = await res.json();

      const fullName = [d.firstName ?? d.FirstName, d.lastName ?? d.LastName].filter(Boolean).join(" ");
      const email = d.email ?? d.Email;
      const phone1 = d.phoneNumber ?? d.PhoneNumber;
      const phone2 = d.phoneNumber2 ?? d.PhoneNumber2;
      const jobTitle = d.jobTitle ?? d.JobTitle;

      const comment = d.comment ?? d.Comment;
      const comment2 = d.comment2 ?? d.Comment2;

      const isPrimary = d.isPrimary ?? d.IsPrimary;
      const created = d.createdDate ?? d.CreatedDate;
      const updated = d.updatedDate ?? d.UpdatedDate;

      const statusObj = d.status ?? d.Status;
      const statusName =
        (typeof statusObj === "string" ? statusObj : statusObj?.name ?? statusObj?.Name) ??
        d.statusName ??
        d.StatusName ??
        "N/A";

      // Nálad a ContactDto nem ad partnerName-t alapból → ha nincs, legalább írjuk ki az ID-t
      const partnerId = d.partnerId ?? d.PartnerId;
      const partnerName =
        d.partnerName ?? d.PartnerName ??
        (d.partner ?? d.Partner ? (d.partner ?? d.Partner).name ?? (d.partner ?? d.Partner).Name : null);

      // Headerben ugyanaz a minta, mint partnernél (név + ID + státusz badge)
      contentEl.innerHTML = `
        <div class="container-fluid">
          <!-- HEADER -->
          <div class="d-flex flex-column flex-md-row justify-content-between align-items-start gap-3 mb-3">
            <div>
              <h4 class="fw-bold mb-1">${escapeHtml(fullName || "Névtelen kontakt")}</h4>
              <div class="text-muted">
                <span class="me-2">ID: <strong>${escapeHtml(String(contactId))}</strong></span>
              </div>
            </div>

            <div class="text-md-end">
              <div class="mb-1">
                <span class="me-2">Státusz</span>
                <span class="badge bg-info text-white">${escapeHtml(statusName)}</span>
              </div>
              <div class="text-muted small">
                Frissítve: ${formatDateHU(updated) !== "—" ? formatDateHU(updated) : formatDateHU(created)}
              </div>
            </div>
          </div>

          ${section("Kapcsolat", `
            <div class="row g-3">
              ${kv("E-mail", mailto(email))}
              ${kv("Telefonszám", tel(phone1))}
              ${kv("Második telefonszám", tel(phone2))}
              ${kv("Beosztás", escapeHtml(jobTitle ?? "—"))}
            </div>
          `)}

          ${section("Hozzárendelés", `
            <div class="row g-3">
              ${kv("Státusz", escapeHtml(statusName))}
              ${kv("Partner", partnerId
                ? (partnerName
                    ? `<a href="../Partners/Details?id=${escapeAttr(String(partnerId))}" target="_blank" rel="noopener">${escapeHtml(partnerName)}</a>`
                    : `<a href="../Partners/Details?id=${escapeAttr(String(partnerId))}" target="_blank" rel="noopener">Partner #${escapeHtml(String(partnerId))}</a>`)
                : "—"
              )}
            </div>
          `)}

          ${section("További beállítások", `
            <div class="row g-3">
              ${kv("Elsődleges kontakt", isPrimary === true ? badge("Igen", "primary") : "Nem")}
            </div>
          `)}

          ${section("Jegyzetek", `
            <div class="p-3 bg-body-tertiary rounded-3">
              ${comment ? nl2br(escapeHtml(comment)) : '<span class="text-muted">Nincs megjegyzés.</span>'}
            </div>
            <div class="mt-3 p-3 bg-body-tertiary rounded-3">
              ${comment2 ? nl2br(escapeHtml(comment2)) : '<span class="text-muted">Nincs további megjegyzés.</span>'}
            </div>
          `)}

          ${section("Időbélyegek", `
            <div class="row g-3">
              ${kv("Létrehozva", formatDateTimeHU(created))}
              ${kv("Frissítve", formatDateTimeHU(updated))}
            </div>
          `)}
        </div>
      `;
    } catch (err) {
      console.error(err);
      contentEl.innerHTML = `
        <div class="alert alert-danger m-3">
          <strong>Hiba:</strong> ${escapeHtml(err.message || "Nem sikerült betölteni a kontakt adatait.")}
        </div>
      `;
      window.c92?.showToast?.("error", "Hiba a kontakt betöltésekor");
    }
  });

  /* ================== HELPERS (ugyanaz a minta, mint viewPartner.js) ================== */

  function loadingHtml(text) {
    return `
      <div class="text-center py-5">
        <div class="spinner-border text-primary" role="status"></div>
        <p class="mt-3 mb-0">${escapeHtml(text)}</p>
      </div>
    `;
  }

  function section(title, bodyHtml) {
    return `
      <hr class="my-4">
      <h5 class="mb-3">${escapeHtml(title)}</h5>
      ${bodyHtml}
    `;
  }

  function kv(label, value) {
    return `
      <div class="col-md-6">
        <div class="text-muted small">${escapeHtml(label)}</div>
        <div>${value == null || value === "" ? "—" : value}</div>
      </div>
    `;
  }

  function badge(text, type) {
    return `<span class="badge bg-${escapeAttr(type)}">${escapeHtml(text)}</span>`;
  }

  function mailto(email) {
    if (!email) return "—";
    const safe = escapeHtml(email);
    return `<a href="mailto:${escapeAttr(email)}">${safe}</a>`;
  }

  function tel(phone) {
    if (!phone) return "—";
    const safe = escapeHtml(phone);
    const telHref = String(phone).replace(/\s+/g, "");
    return `<a href="tel:${escapeAttr(telHref)}">${safe}</a>`;
  }

  function formatDateHU(val) {
    if (!val) return "—";
    const d = new Date(val);
    if (isNaN(d.getTime())) return "—";
    return d.toLocaleDateString("hu-HU");
  }

  function formatDateTimeHU(val) {
    if (!val) return "—";
    const d = new Date(val);
    if (isNaN(d.getTime())) return "—";
    const pad = (n) => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(
      d.getMinutes()
    )}`;
  }

  function escapeHtml(str) {
    return String(str ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  function escapeAttr(str) {
    return escapeHtml(str).replaceAll("`", "&#096;");
  }

  function nl2br(s) {
    return String(s).replace(/\n/g, "<br>");
  }
});
