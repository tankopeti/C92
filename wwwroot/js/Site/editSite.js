// /js/Site/editSite.js
console.log("✅ editSite.js betöltve");

document.addEventListener("DOMContentLoaded", () => {
  const modalEl = document.getElementById("editSiteModal");
  if (!modalEl) return;

  const partnerSelectEl = document.getElementById("editPartnerId");
  let partnerTS = null;

  /* ---------------- TOMSELECT (Partner) ---------------- */

  function ensurePartnerTomSelect() {
    if (!partnerSelectEl) return null;
    if (partnerSelectEl.tomselect) return partnerSelectEl.tomselect;
    if (!window.TomSelect) return null;

    partnerTS = new TomSelect(partnerSelectEl, {
      valueField: "id",
      labelField: "text",
      searchField: ["text"],
      maxItems: 1,
      maxOptions: 50,
      create: false,
      allowEmptyOption: true,
      closeAfterSelect: true,
      dropdownParent: "body",

      // ✅ ne kelljen gépelni
      preload: true,
      shouldLoad: () => true,

      load: async (query, callback) => {
        try {
          const url = `/api/partners/select?search=${encodeURIComponent(query || "")}`;
          const res = await fetch(url, {
            credentials: "same-origin",
            headers: { Accept: "application/json" }
          });
          if (!res.ok) throw new Error(`HTTP ${res.status}`);
          const data = await res.json();
          callback(Array.isArray(data) ? data : []);
        } catch (e) {
          console.error("Partner TomSelect load error:", e);
          callback([]);
        }
      },

      onFocus() {
        this.refreshOptions(false);
        this.open();
      }
    });

    return partnerTS;
  }

  function setPartnerValue(partnerId, partnerName) {
    const ts = ensurePartnerTomSelect();
    if (!ts) {
      partnerSelectEl.value = partnerId ? String(partnerId) : "";
      return;
    }

    if (!partnerId) {
      ts.clear(true);
      return;
    }

    const id = String(partnerId);
    const text = partnerName || `Partner ${id}`;

    ts.addOption({ id, text });
    ts.setValue(id, true);
  }

  modalEl.addEventListener("shown.bs.modal", () => {
    ensurePartnerTomSelect();
  });

  /* ---------------- OPEN + LOAD DATA ---------------- */

  document.addEventListener("click", async (e) => {
    const btn = e.target.closest(".edit-site-btn");
    if (!btn) return;
    e.preventDefault();

    const siteId = btn.dataset.siteId;
    if (!siteId) return;

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
    resetForm();

    try {
      const res = await fetch(`/api/SitesIndex/${encodeURIComponent(siteId)}`, {
        credentials: "same-origin",
        headers: { Accept: "application/json" }
      });

      if (!res.ok) {
        throw new Error(res.status === 404 ? "Telephely nem található" : `HTTP ${res.status}`);
      }

      const d = await res.json();

      set("editSiteId", d.siteId ?? d.SiteId ?? siteId);
      set("editSiteName", d.siteName ?? d.SiteName ?? "");

      const pid = d.partnerId ?? d.PartnerId ?? "";
      const pname = d.partnerName ?? d.PartnerName ?? "";
      setPartnerValue(pid, pname);

      set("editAddressLine1", d.addressLine1 ?? d.AddressLine1 ?? "");
      set("editAddressLine2", d.addressLine2 ?? d.AddressLine2 ?? "");
      set("editCity", d.city ?? d.City ?? "");
      set("editState", d.state ?? d.State ?? "");
      set("editPostalCode", d.postalCode ?? d.PostalCode ?? "");
      set("editCountry", d.country ?? d.Country ?? "");
      set("editContactPerson1", d.contactPerson1 ?? d.ContactPerson1 ?? "");
      set("editContactPerson2", d.contactPerson2 ?? d.ContactPerson2 ?? "");
      set("editContactPerson3", d.contactPerson3 ?? d.ContactPerson3 ?? "");
      set("editComment1", d.comment1 ?? d.Comment1 ?? "");
      set("editComment2", d.comment2 ?? d.Comment2 ?? "");
      set("editStatusId", d.statusId ?? d.StatusId ?? "");
      setChecked("editIsPrimary", (d.isPrimary ?? d.IsPrimary) === true);
    } catch (err) {
      console.error(err);
      window.c92?.showToast?.("error", err.message || "Nem sikerült betölteni szerkesztéshez");
    }
  });

  /* ---------------- SAVE (AJAX PUT) ---------------- */
  // ✅ Csak EZ az egy submit handler legyen. (capture=true)
  document.addEventListener(
    "submit",
    async (e) => {
      const form = e.target;
      if (!(form instanceof HTMLFormElement)) return;
      if (form.id !== "editSiteForm") return;

      e.preventDefault();
      console.log("✅ editSiteForm submit elkapva");

      // bootstrap validation
      if (!form.checkValidity()) {
        form.classList.add("was-validated");
        return;
      }

      const saveBtn = form.querySelector('button[type="submit"]');
      if (saveBtn) saveBtn.disabled = true;

      try {
        const siteId = Number(get("editSiteId"));
        if (!siteId) {
          window.c92?.showToast?.("error", "Hiányzó SiteId");
          return;
        }

        const ts = partnerSelectEl?.tomselect;
        const partnerId = Number(ts ? ts.getValue() : (partnerSelectEl?.value || 0));

        if (!partnerId) {
          window.c92?.showToast?.("error", "Partner megadása kötelező");
          form.classList.add("was-validated");
          return;
        }

        const dto = {
        SiteId: siteId,
        PartnerId: partnerId,
        SiteName: get("editSiteName")?.trim() || null,
        AddressLine1: get("editAddressLine1")?.trim() || null,
        AddressLine2: get("editAddressLine2")?.trim() || null,
        City: get("editCity")?.trim() || null,
        State: get("editState")?.trim() || null,
        PostalCode: get("editPostalCode")?.trim() || null,
        Country: get("editCountry")?.trim() || null,
        ContactPerson1: get("editContactPerson1")?.trim() || null,
        ContactPerson2: get("editContactPerson2")?.trim() || null,
        ContactPerson3: get("editContactPerson3")?.trim() || null,
        Comment1: get("editComment1")?.trim() || null,
        Comment2: get("editComment2")?.trim() || null,
        StatusId: get("editStatusId") ? Number(get("editStatusId")) : null,
        IsPrimary: isChecked("editIsPrimary")
        };


        console.log("DTO:", dto);
        console.log("PUT:", `/api/SitesIndex/${siteId}`);

        const res = await fetch(`/api/SitesIndex/${encodeURIComponent(siteId)}`, {
          method: "PUT",
          credentials: "same-origin",
          headers: { "Content-Type": "application/json", Accept: "application/json" },
          body: JSON.stringify(dto)
        });

        console.log("PUT response:", res.status);

        if (!res.ok) {
        const raw = await res.text();
        console.error("PUT error raw:", raw);

        let err = {};
        try { err = raw ? JSON.parse(raw) : {}; } catch {}

        window.c92?.showToast?.(
            "error",
            err?.errors?.Id?.[0] ||
            err?.errors?.SiteId?.[0] ||
            err?.errors?.PartnerId?.[0] ||
            err?.title ||
            err?.message ||
            raw ||
            `HTTP ${res.status}`
        );
        return;
        }


        const updatedRow = await res.json().catch(() => null);
        console.log("PUT payload:", updatedRow);

        if (updatedRow) patchRow(updatedRow);

        window.c92?.showToast?.("success", "Telephely frissítve!");
        bootstrap.Modal.getInstance(modalEl)?.hide();
      } catch (err) {
        console.error(err);
        window.c92?.showToast?.("error", "Hálózati hiba");
      } finally {
        if (saveBtn) saveBtn.disabled = false;
      }
    },
    true
  );

  /* ---------------- TABLE PATCH ---------------- */

  function patchRow(s) {
    const tr = document.querySelector(`tr[data-site-id="${s.siteId}"]`);
    if (!tr) return;

    const tds = tr.querySelectorAll("td");
    if (tds.length < 11) return;

    tds[0].innerHTML = `<i class="bi bi-building me-1"></i>${escapeHtml(s.siteName || "—")}`;
    tds[1].textContent = s.partnerName || "—";
    tds[2].textContent = s.addressLine1 || "—";
    tds[3].textContent = s.addressLine2 || "—";
    tds[4].textContent = s.city || "—";
    tds[5].textContent = s.postalCode || "—";
    tds[6].textContent = s.contactPerson1 || "—";
    tds[7].textContent = s.contactPerson2 || "—";
    tds[8].textContent = s.contactPerson3 || "—";

    const statusColor = s.status?.color || "#6c757d";
    const statusName = s.status?.name || "—";
    tds[9].innerHTML = `<span class="badge" style="background:${escapeAttr(statusColor)};color:white">${escapeHtml(
      statusName
    )}</span>`;

    tds[10].innerHTML = s.isPrimary ? `<span class="badge bg-primary">Elsődleges</span>` : `<span>-</span>`;
  }

  /* ---------------- UTILS ---------------- */

  function resetForm() {
    const formEl = document.getElementById("editSiteForm");
    if (formEl) formEl.classList.remove("was-validated");

    [
      "editSiteId",
      "editSiteName",
      "editAddressLine1",
      "editAddressLine2",
      "editCity",
      "editState",
      "editPostalCode",
      "editCountry",
      "editContactPerson1",
      "editContactPerson2",
      "editContactPerson3",
      "editComment1",
      "editComment2",
      "editStatusId"
    ].forEach((id) => set(id, ""));

    setChecked("editIsPrimary", false);

    const ts = partnerSelectEl?.tomselect;
    if (ts) ts.clear(true);
    if (partnerSelectEl && !ts) partnerSelectEl.value = "";
  }

  function set(id, val) {
    const el = document.getElementById(id);
    if (el) el.value = val ?? "";
  }
  function get(id) {
    return document.getElementById(id)?.value ?? "";
  }
  function setChecked(id, val) {
    const el = document.getElementById(id);
    if (el) el.checked = val === true;
  }
  function isChecked(id) {
    return document.getElementById(id)?.checked === true;
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
});
