// /js/Contact/editContact.js
// - API-ból tölti a kontaktot szerkesztésre
// - AJAX PUT-tal ment (oldal reload nélkül)
// - zöld toast + modal close + sor frissítés

document.addEventListener("DOMContentLoaded", function () {
  console.log("editContact.js (AJAX) BETÖLTÖDÖTT");

  const modalEl = document.getElementById("editContactModal");
  const form = document.getElementById("editContactForm");
  if (!modalEl || !form) return;

  /* ================== TOMSELECT (MODAL ONLY) ================== */

  function ensureTomSelect(selectEl) {
    if (!selectEl || selectEl.tomselect || !window.TomSelect) return;

    new TomSelect(selectEl, {
      create: false,
      allowEmptyOption: true,
      closeAfterSelect: true,
      dropdownParent: "body",
      placeholder: selectEl.getAttribute("data-placeholder") || "Válasszon..."
    });
  }

  function refreshTomSelect(selectEl) {
    if (!selectEl?.tomselect) return;
    selectEl.tomselect.sync();
    selectEl.tomselect.refreshOptions(false);
  }

  modalEl.addEventListener("shown.bs.modal", () => {
    ensureTomSelect(document.getElementById("editStatusId"));
    ensureTomSelect(document.getElementById("editPartnerId"));

    setTimeout(() => {
      refreshTomSelect(document.getElementById("editStatusId"));
      refreshTomSelect(document.getElementById("editPartnerId"));
    }, 80);
  });

  /* ================== HELPERS ================== */

  const $ = (id) => document.getElementById(id);

  const setValue = (id, value) => {
    const el = $(id);
    if (el) el.value = value ?? "";
  };

  const setChecked = (id, value) => {
    const el = $(id);
    if (el) el.checked = value === true;
  };

  function escapeHtml(s) {
    return String(s ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  /* ================== OPEN + LOAD ================== */

  document.addEventListener("click", async (e) => {
    const btn =
      e.target.closest('[data-bs-target="#editContactModal"][data-contact-id]') ||
      e.target.closest(".edit-contact-btn");

    if (!btn) return;

    const contactId = btn.dataset.contactId;
    if (!contactId) {
      window.c92?.showToast?.("error", "Hiányzó Kontakt ID");
      return;
    }

    bootstrap.Modal.getOrCreateInstance(modalEl).show();

    // reset
    setValue("editContactId", contactId);
    setValue("editFirstName", "");
    setValue("editLastName", "");
    setValue("editEmail", "");
    setValue("editPhoneNumber", "");
    setValue("editPhoneNumber2", "");
    setValue("editJobTitle", "");
    setValue("editStatusId", "");
    setValue("editPartnerId", "");
    setChecked("editIsPrimary", false);
    setValue("editComment", "");
    setValue("editComment2", "");

    try {
      const res = await fetch(`/api/Contact/${encodeURIComponent(contactId)}`, {
        credentials: "same-origin",
        headers: { Accept: "application/json" }
      });

      if (!res.ok) throw new Error(res.status === 404 ? "Kontakt nem található" : `HTTP ${res.status}`);

      const d = await res.json();

      setValue("editContactId", d.contactId ?? d.ContactId ?? contactId);
      setValue("editFirstName", d.firstName ?? d.FirstName ?? "");
      setValue("editLastName", d.lastName ?? d.LastName ?? "");
      setValue("editEmail", d.email ?? d.Email ?? "");
      setValue("editPhoneNumber", d.phoneNumber ?? d.PhoneNumber ?? "");
      setValue("editPhoneNumber2", d.phoneNumber2 ?? d.PhoneNumber2 ?? "");
      setValue("editJobTitle", d.jobTitle ?? d.JobTitle ?? "");
      setValue("editStatusId", (d.statusId ?? d.StatusId) ?? "");
      setValue("editPartnerId", (d.partnerId ?? d.PartnerId) ?? "");
      setChecked("editIsPrimary", (d.isPrimary ?? d.IsPrimary) === true);
      setValue("editComment", d.comment ?? d.Comment ?? "");
      setValue("editComment2", d.comment2 ?? d.Comment2 ?? "");

      refreshTomSelect($("editStatusId"));
      refreshTomSelect($("editPartnerId"));
    } catch (err) {
      console.error(err);
      window.c92?.showToast?.("error", err.message || "Nem sikerült betölteni a kontaktot");
    }
  });

  /* ================== SUBMIT (AJAX PUT) ================== */

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const contactId = $("editContactId")?.value;
    if (!contactId) {
      window.c92?.showToast?.("error", "Hiányzó Kontakt ID");
      return;
    }

    const dto = {
      firstName: $("editFirstName")?.value?.trim() || null,
      lastName: $("editLastName")?.value?.trim() || null,
      email: $("editEmail")?.value?.trim() || null,
      phoneNumber: $("editPhoneNumber")?.value?.trim() || null,
      phoneNumber2: $("editPhoneNumber2")?.value?.trim() || null,
      jobTitle: $("editJobTitle")?.value?.trim() || null,
      comment: $("editComment")?.value || null,
      comment2: $("editComment2")?.value || null,
      isPrimary: $("editIsPrimary")?.checked === true,
      statusId: $("editStatusId")?.value ? parseInt($("editStatusId").value, 10) : null,
      partnerId: $("editPartnerId")?.value ? parseInt($("editPartnerId").value, 10) : null
    };

    try {
      const res = await fetch(`/api/Contact/${encodeURIComponent(contactId)}`, {
        method: "PUT",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "Accept": "application/json",
          "X-Requested-With": "XMLHttpRequest"
        },
        body: JSON.stringify(dto)
      });

      const payload = await res.json().catch(() => ({}));

      if (!res.ok || payload.success === false) {
        window.c92?.showToast?.("error", payload.message || "Hiba a mentéskor");
        return;
      }

      const updated = payload.data;

      patchRow(updated);

      window.c92?.showToast?.("success", payload.message || "Kontakt sikeresen frissítve!");
      bootstrap.Modal.getInstance(modalEl)?.hide();
    } catch (err) {
      console.error(err);
      window.c92?.showToast?.("error", "Hálózati hiba");
    }
  });

  /* ================== TABLE ROW PATCH ================== */

  function patchRow(d) {
    const id = d.contactId ?? d.ContactId;
    const tr = document.querySelector(`tr[data-contact-id="${CSS.escape(String(id))}"]`);
    if (!tr) return;

    const first = d.firstName ?? d.FirstName ?? "";
    const last = d.lastName ?? d.LastName ?? "";
    const email = d.email ?? d.Email ?? "";
    const phone1 = d.phoneNumber ?? d.PhoneNumber ?? "";
    const phone2 = d.phoneNumber2 ?? d.PhoneNumber2 ?? "";
    const job = d.jobTitle ?? d.JobTitle ?? "";

    const statusObj = d.status ?? d.Status;
    const statusName =
      (typeof statusObj === "string"
        ? statusObj
        : statusObj?.name ?? statusObj?.Name) || "N/A";

    const tds = tr.querySelectorAll("td");
    if (tds.length < 8) return;

    tds[0].textContent = `${first} ${last}`.trim();
    tds[1].innerHTML = `<i class="bi bi-envelope me-1"></i>${escapeHtml(email)}`;
    tds[2].innerHTML =
      `<i class="bi bi-telephone me-1"></i>${escapeHtml(phone1)}` +
      (phone2 ? `; ${escapeHtml(phone2)}` : "");
    tds[3].innerHTML = `<i class="bi bi-briefcase me-1"></i>${escapeHtml(job)}`;

    const badgeClass =
      statusName === "Aktív"
        ? "badge bg-success"
        : statusName === "Inaktív"
        ? "badge bg-secondary"
        : "badge bg-secondary";

    const hasPartnerBadge = tds[4].querySelector(".badge.bg-info") != null;

    tds[4].innerHTML =
      `<span class="${badgeClass}">${escapeHtml(statusName)}</span>` +
      (hasPartnerBadge
        ? ` <span class="badge bg-info ms-1" title="Hozzárendelve partnerhez">Partner</span>`
        : "");
  }
});
