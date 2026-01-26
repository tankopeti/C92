// wwwroot/js/taskAdvancedFilter.js
(() => {
  "use strict";

  const MODAL_ID = "advancedFilterModal";
  const APPLY_EVENT = "tasks:applyFilters";
  const CLEAR_EVENT = "tasks:clearFilters";

  function $(sel, root = document) {
    return root.querySelector(sel);
  }

  function safeInitTomSelect(selectEl, opts = {}) {
    if (!selectEl) return null;
    if (selectEl.tomselect) return selectEl.tomselect; // already init
    if (typeof TomSelect === "undefined") return null;

    return new TomSelect(selectEl, {
      allowEmptyOption: true,
      create: false,
      plugins: ["clear_button"],
      ...opts,
    });
  }

  function buildParamsFromForm(form) {
    const fd = new FormData(form);
    const params = new URLSearchParams();

    for (const [k, v] of fd.entries()) {
      const value = (v ?? "").toString().trim();
      if (!value) continue;               // üres értékeket dobjuk
      params.set(k, value);
    }

    // biztosítsuk, hogy szűrésnél mindig 1. oldalról induljon
    params.set("page", "1");

    return params;
  }

  function updateUrlQuery(params) {
    const url = new URL(window.location.href);
    url.search = params.toString();
    window.history.replaceState({}, "", url.toString());
  }

  function dispatchApply(params) {
    const obj = Object.fromEntries(params.entries());
    window.dispatchEvent(
      new CustomEvent(APPLY_EVENT, { detail: { params: obj, urlParams: params } })
    );
  }

  function dispatchClear() {
    window.dispatchEvent(new CustomEvent(CLEAR_EVENT));
  }

  function setupPartnerSiteDependency(partnerSelect, siteSelect) {
    if (!partnerSelect || !siteSelect) return;

    // Mentsük le az összes telephely option-t (partnerId-val együtt)
    const allSiteOptions = Array.from(siteSelect.options).map(o => ({
      value: o.value,
      text: o.text,
      partnerId: o.dataset.partnerId || "",
      selected: o.selected,
      disabled: o.disabled,
    }));

    const placeholder = allSiteOptions.find(o => o.value === "") || {
      value: "",
      text: "-- Minden --",
      partnerId: "",
      selected: false,
      disabled: false,
    };

    function rebuildSitesForPartner(partnerId) {
      const currentValue = siteSelect.value;

      // Szűrt lista: üres + partnerhez tartozók (vagy mind, ha nincs partner)
      const filtered = [placeholder].concat(
        allSiteOptions
          .filter(o => o.value !== "")
          .filter(o => !partnerId || o.partnerId === partnerId)
      );

      // Újraépítés
      siteSelect.innerHTML = "";
      for (const o of filtered) {
        const opt = document.createElement("option");
        opt.value = o.value;
        opt.textContent = o.text;
        if (o.partnerId) opt.dataset.partnerId = o.partnerId;
        siteSelect.appendChild(opt);
      }

      // Próbáljuk visszaállítani a kiválasztott telephelyet, ha még érvényes
      const stillExists = Array.from(siteSelect.options).some(o => o.value === currentValue);
      siteSelect.value = stillExists ? currentValue : "";

      // TomSelect esetén is frissítsünk
      if (siteSelect.tomselect) {
        const ts = siteSelect.tomselect;
        ts.clearOptions();
        Array.from(siteSelect.options).forEach(o => {
          ts.addOption({ value: o.value, text: o.textContent });
        });
        ts.refreshOptions(false);

        // állítsuk be a value-t a TS-ben is
        ts.setValue(siteSelect.value || "");
      }
    }

    // inicializáció
    rebuildSitesForPartner(partnerSelect.value);

    // onchange: partner változás -> telephelyek szűkítése
    partnerSelect.addEventListener("change", () => {
      rebuildSitesForPartner(partnerSelect.value);
    });

    // Ha partner TS, akkor is működjön (TomSelect change esemény ugyanúgy megy)
  }

  function wireUp() {
    const modalEl = document.getElementById(MODAL_ID);
    if (!modalEl) return;

    const form = $("form", modalEl);
    if (!form) return;

    // Selectek
    const partnerSelect = $("#partnerFilterSelect", modalEl);
    const siteSelect = $("#siteFilterSelect", modalEl);

    // Opcionális TomSelect
    safeInitTomSelect(partnerSelect, { placeholder: "Partner..." });
    safeInitTomSelect(siteSelect, { placeholder: "Telephely..." });

    // Partner -> Site függés (data-partner-id alapján)
    setupPartnerSiteDependency(partnerSelect, siteSelect);

    // Form submit: param építés + event + fallback URL frissítés
    form.addEventListener("submit", (e) => {
      e.preventDefault();

      const params = buildParamsFromForm(form);

      // 1) UI: URL frissítés (hogy reloadnál is megmaradjon)
      updateUrlQuery(params);

      // 2) App: event a LoadMore/AJAX rétegnek
      dispatchApply(params);

      // 3) Fallback: ha nincs JS-es lista újratöltés, navigáljunk (klasszikus GET)
      // Ha van listener, az megfogja és nem kell navigálni.
      // Heurisztika: ha nincs listener, akkor menjünk.
      // (Nem tudjuk biztosan detektálni a listenereket, ezért inkább "opt-in" navigálás:)
      if (!window.__TASKS_FILTER_AJAX__) {
        window.location.href = `${window.location.pathname}?${params.toString()}`;
      }

      // Modal bezárás
      try {
        const bsModal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
        bsModal.hide();
      } catch {
        // ha bootstrap nincs, ignoráljuk
      }
    });

    // A te gombod onclick-kal submit-ol – ez is a form submitre fut be.

    // "Szűrők törlése" linket okosítjuk (ha szeretnéd, maradhat nélküle is)
    const clearBtn = $('.modal-footer a.btn-outline-danger', modalEl);
    if (clearBtn) {
      clearBtn.addEventListener("click", () => {
        dispatchClear();
        // a link amúgy is navigálni fog ./Index-re, az oké
      });
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", wireUp);
  } else {
    wireUp();
  }
})();
