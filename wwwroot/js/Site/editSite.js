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
      preload: true,
      shouldLoad: () => true,

      load: async (query, callback) => {
        try {
          const url = `/api/partners/select?search=${encodeURIComponent(query || "")}`;
          const res = await fetch(url, { credentials: "same-origin" });
          if (!res.ok) throw new Error(`HTTP ${res.status}`);
          callback(await res.json());
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

  modalEl.addEventListener("shown.bs.modal", ensurePartnerTomSelect);

  /* ---------------- OPEN + LOAD ---------------- */

  document.addEventListener("click", async (e) => {
    const btn = e.target.closest(".edit-site-btn");
    if (!btn) return;
    e.preventDefault();

    const siteId = btn.dataset.siteId;
    if (!siteId) return;

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
    resetForm();

    try {
      const res = await fetch(`/api/SitesIndex/${siteId}`);
      if (!res.ok) throw new Error("Telephely nem található");
      const d = await res.json();

      set("editSiteId", d.siteId);
      set("editSiteName", d.siteName);
      setPartnerValue(d.partnerId, d.partnerName);

      set("editAddressLine1", d.addressLine1);
      set("editAddressLine2", d.addressLine2);
      set("editCity", d.city);
      set("editState", d.state);
      set("editPostalCode", d.postalCode);
      set("editCountry", d.country);

      set("editContactPerson1", d.contactPerson1);
      set("editContactPerson2", d.contactPerson2);
      set("editContactPerson3", d.contactPerson3);

      set("editPhone1", d.phone1);
      set("editPhone2", d.phone2);
      set("editPhone3", d.phone3);

      set("editMobilePhone1", d.mobilePhone1);
      set("editMobilePhone2", d.mobilePhone2);
      set("editMobilePhone3", d.mobilePhone3);

      set("editMessagingApp1", d.messagingApp1);
      set("editMessagingApp2", d.messagingApp2);
      set("editMessagingApp3", d.messagingApp3);

      set("editeMail1", d.eMail1);
      set("editeMail2", d.eMail2);

      set("editComment1", d.comment1);
      set("editComment2", d.comment2);

      set("editStatusId", d.statusId);
      setChecked("editIsPrimary", d.isPrimary === true);
      setChecked("editIsActive", d.isActive !== false);
    } catch (err) {
      console.error(err);
      window.c92?.showToast?.("error", err.message);
    }
  });

  /* ---------------- SAVE (PUT) ---------------- */

  document.addEventListener(
    "submit",
    async (e) => {
      const form = e.target;
      if (form.id !== "editSiteForm") return;
      e.preventDefault();

      if (!form.checkValidity()) {
        form.classList.add("was-validated");
        return;
      }

      const siteId = Number(get("editSiteId"));
      const partnerId = Number(partnerSelectEl?.tomselect?.getValue() || 0);
      if (!siteId || !partnerId) return;

      const dto = {
        SiteId: siteId,
        PartnerId: partnerId,
        SiteName: get("editSiteName") || null,
        AddressLine1: get("editAddressLine1") || null,
        AddressLine2: get("editAddressLine2") || null,
        City: get("editCity") || null,
        State: get("editState") || null,
        PostalCode: get("editPostalCode") || null,
        Country: get("editCountry") || null,

        ContactPerson1: get("editContactPerson1") || null,
        ContactPerson2: get("editContactPerson2") || null,
        ContactPerson3: get("editContactPerson3") || null,

        Phone1: get("editPhone1") || null,
        Phone2: get("editPhone2") || null,
        Phone3: get("editPhone3") || null,

        MobilePhone1: get("editMobilePhone1") || null,
        MobilePhone2: get("editMobilePhone2") || null,
        MobilePhone3: get("editMobilePhone3") || null,

        messagingApp1: get("editMessagingApp1") || null,
        messagingApp2: get("editMessagingApp2") || null,
        messagingApp3: get("editMessagingApp3") || null,

        eMail1: get("editeMail1") || null,
        eMail2: get("editeMail2") || null,

        Comment1: get("editComment1") || null,
        Comment2: get("editComment2") || null,

        StatusId: get("editStatusId") ? Number(get("editStatusId")) : null,
        IsPrimary: isChecked("editIsPrimary"),
        IsActive: isChecked("editIsActive")
      };

      const res = await fetch(`/api/SitesIndex/${siteId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto)
      });

      if (!res.ok) {
        window.c92?.showToast?.("error", "Mentés sikertelen");
        return;
      }

      const updated = await res.json();
      patchRow(updated);
      window.c92?.showToast?.("success", "Telephely frissítve");
      bootstrap.Modal.getInstance(modalEl)?.hide();
    },
    true
  );

  /* ---------------- TABLE PATCH ---------------- */

  function patchRow(s) {
    const tr = document.querySelector(`tr[data-site-id="${s.siteId}"]`);
    if (!tr) return;

    const tds = tr.querySelectorAll("td");
    if (tds.length < 11) return;

    tds[0].textContent = s.siteName || "—";
    tds[1].textContent = s.partnerName || "—";
    tds[2].textContent = s.addressLine1 || "—";
    tds[3].textContent = s.addressLine2 || "—";
    tds[4].textContent = s.city || "—";
    tds[5].textContent = s.postalCode || "—";
    tds[6].textContent = s.contactPerson1 || "—";
    tds[7].textContent = s.contactPerson2 || "—";
    tds[8].textContent = s.contactPerson3 || "—";

    const status = s.status?.name || "—";
    tds[9].innerHTML = `<span class="badge">${status}</span>`;
    tds[10].innerHTML = s.isPrimary ? `<span class="badge bg-primary">Elsődleges</span>` : "-";
  }

  /* ---------------- UTILS ---------------- */

  function resetForm() {
    [
      "editSiteId","editSiteName","editAddressLine1","editAddressLine2",
      "editCity","editState","editPostalCode","editCountry",
      "editContactPerson1","editContactPerson2","editContactPerson3",
      "editPhone1","editPhone2","editPhone3",
      "editMobilePhone1","editMobilePhone2","editMobilePhone3",
      "editMessagingApp1","editMessagingApp2","editMessagingApp3",
      "editeMail1","editeMail2",
      "editComment1","editComment2","editStatusId"
    ].forEach(id => set(id, ""));

    setChecked("editIsPrimary", false);
    setChecked("editIsActive", true);
    partnerSelectEl?.tomselect?.clear(true);
  }

  function set(id, val) {
    const el = document.getElementById(id);
    if (el) el.value = val ?? "";
  }
  function get(id) {
    return document.getElementById(id)?.value?.trim() || "";
  }
  function setChecked(id, val) {
    const el = document.getElementById(id);
    if (el) el.checked = val === true;
  }
  function isChecked(id) {
    return document.getElementById(id)?.checked === true;
  }
});
