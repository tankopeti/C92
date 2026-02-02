// /wwwroot/js/Site/createSite.js
console.log("✅ createSite.js betöltve");

document.addEventListener("DOMContentLoaded", () => {
  const modalEl = document.getElementById("createSiteModal");
  const form = document.getElementById("createSiteForm");

  if (!modalEl || !form) {
    console.warn("createSiteModal vagy createSiteForm hiányzik");
    return;
  }

  const partnerSelectEl = document.getElementById("createPartnerId");

  // CSRF (ha nálad kell – nálad a task modalban is így van)
  const csrf =
    document.querySelector('meta[name="csrf-token"]')?.content ||
    document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
    (document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1]
      ? decodeURIComponent(document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1])
      : "") ||
    "";

  function valByName(name) {
    return (form.querySelector(`[name="${name}"]`)?.value ?? "").trim();
  }

  function nullIfEmpty(v) {
    const s = (v ?? "").toString().trim();
    return s ? s : null;
  }

  function isCheckedById(id) {
    return document.getElementById(id)?.checked === true;
  }

  function resetForm() {
    form.classList.remove("was-validated");
    form.reset();

    // TomSelect partner clear
    const ts = partnerSelectEl?.tomselect;
    if (ts) ts.clear(true);

    // defaultok (ha kellenek)
    const country = form.querySelector('[name="country"]');
    if (country && !country.value) country.value = "Magyarország";

    const isActive = document.getElementById("createSiteIsActive");
    if (isActive) isActive.checked = true;
  }

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    // bootstrap validation
    if (!form.checkValidity()) {
      form.classList.add("was-validated");
      return;
    }

    const ts = partnerSelectEl?.tomselect;
    const partnerId = Number(ts ? ts.getValue() : (partnerSelectEl?.value || 0));

    if (!partnerId) {
      window.c92?.showToast?.("error", "Partner megadása kötelező");
      form.classList.add("was-validated");
      return;
    }

    const statusIdRaw = valByName("statusId");
    const dto = {
      // create
      siteId: 0,
      partnerId,

      siteName: nullIfEmpty(valByName("siteName")),
      addressLine1: nullIfEmpty(valByName("addressLine1")),
      addressLine2: nullIfEmpty(valByName("addressLine2")),
      city: nullIfEmpty(valByName("city")),
      state: nullIfEmpty(valByName("state")),
      postalCode: nullIfEmpty(valByName("postalCode")),
      country: nullIfEmpty(valByName("country")),

      contactPerson1: nullIfEmpty(valByName("contactPerson1")),
      contactPerson2: nullIfEmpty(valByName("contactPerson2")),
      contactPerson3: nullIfEmpty(valByName("contactPerson3")),

      phone1: nullIfEmpty(valByName("phone1")),
      phone2: nullIfEmpty(valByName("phone2")),
      phone3: nullIfEmpty(valByName("phone3")),

      mobilePhone1: nullIfEmpty(valByName("mobilePhone1")),
      mobilePhone2: nullIfEmpty(valByName("mobilePhone2")),
      mobilePhone3: nullIfEmpty(valByName("mobilePhone3")),

      messagingApp1: nullIfEmpty(valByName("messagingApp1")),
      messagingApp2: nullIfEmpty(valByName("messagingApp2")),
      messagingApp3: nullIfEmpty(valByName("messagingApp3")),

      eMail1: nullIfEmpty(valByName("eMail1")),
      eMail2: nullIfEmpty(valByName("eMail2")),

      comment1: nullIfEmpty(valByName("comment1")),
      comment2: nullIfEmpty(valByName("comment2")),

      statusId: statusIdRaw ? Number(statusIdRaw) : null,

      isPrimary: isCheckedById("createSiteIsPrimary"),
      isActive: isCheckedById("createSiteIsActive")
    };

    console.log("📦 create dto:", dto);

    const saveBtn = form.querySelector('button[type="submit"]');
    if (saveBtn) saveBtn.disabled = true;

    try {
      const res = await fetch("/api/SitesIndex", {
        method: "POST",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "Accept": "application/json",
          ...(csrf ? { "RequestVerificationToken": csrf } : {})
        },
        body: JSON.stringify(dto)
      });

      console.log("POST /api/SitesIndex status:", res.status);

      if (!res.ok) {
        const raw = await res.text().catch(() => "");
        let err = {};
        try {
          err = raw ? JSON.parse(raw) : {};
        } catch {}
        window.c92?.showToast?.(
          "error",
          err?.errors?.PartnerId?.[0] ||
            err?.errors?.SiteName?.[0] ||
            err?.title ||
            err?.message ||
            raw ||
            `HTTP ${res.status}`
        );
        return;
      }

      const createdRow = await res.json().catch(() => null);
      console.log("✅ createdRow:", createdRow);

      window.c92?.showToast?.("success", "Telephely létrehozva!");

      // ✅ close modal
      bootstrap.Modal.getInstance(modalEl)?.hide();

      // ✅ 1) Lista frissítése oldalújratöltés nélkül:
      // Ha a listád filterezett / lapozott, a legstabilabb a reload.
      window.Sites?.reload?.();

      // ✅ 2) Alternatíva: beszúrás a lista elejére (ha ezt preferálod reload helyett)
      // window.Sites?.prependRow?.(createdRow);

      // ✅ reset form a következő create-hez
      resetForm();
    } catch (err) {
      console.error(err);
      window.c92?.showToast?.("error", "Hálózati hiba");
    } finally {
      if (saveBtn) saveBtn.disabled = false;
    }
  });

  // ha bezárod a modalt, takarítsunk
  modalEl.addEventListener("hidden.bs.modal", () => {
    resetForm();
  });
});
