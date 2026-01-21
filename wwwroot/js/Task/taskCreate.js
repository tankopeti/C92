// wwwroot/js/Tasks/TaskCreate.js
(function () {
  'use strict';

  console.log('[TaskCreate] loaded file');

  document.addEventListener('DOMContentLoaded', () => {
    console.log('[TaskCreate] DOM loaded');

    if (typeof TomSelect === 'undefined') {
      console.error('[TaskCreate] TomSelect is undefined -> nincs betöltve a lib vagy rossz sorrend');
      return;
    }

    const modalEl = document.getElementById('newTaskModal');
    if (!modalEl) {
      console.warn('[TaskCreate] #newTaskModal nincs meg -> skip');
      return;
    }

    const formEl = document.getElementById('createTaskForm') || modalEl.querySelector('form');
    if (!formEl) {
      console.warn('[TaskCreate] create form nincs a modalban -> skip');
      return;
    }

    console.log('[TaskCreate] modal+form ok', { formId: formEl.id || '(no id)' });

    // ---- form elements
    const el = {
      type: formEl.querySelector('[name="TaskTypePMId"]'),
      status: formEl.querySelector('[name="TaskStatusPMId"]'),
      priority: formEl.querySelector('[name="TaskPriorityPMId"]'),
      assignedTo: formEl.querySelector('[name="AssignedToId"]'),

      partner: formEl.querySelector('[name="PartnerId"]'),
      site: formEl.querySelector('[name="SiteId"], #SiteId'),
    };

    // ---- endpoints
    const API = {
      taskTypes: '/api/tasks/tasktypes/select',
      taskStatuses: '/api/tasks/taskstatuses/select',
      taskPriorities: '/api/tasks/taskpriorities/select',
      users: '/api/users/select',

      partners: (q) => `/api/partners/select?search=${encodeURIComponent(q)}`,
      sitesByPartner: (partnerId) => `/api/sites/by-partner/${encodeURIComponent(partnerId)}?search=`
    };

    let currentPartnerId = '';

    // TomSelect instances
    let tsType = null;
    let tsStatus = null;
    let tsPriority = null;
    let tsAssignedTo = null;
    let tsPartner = null;
    let tsSite = null;

    async function fetchJson(url) {
      const r = await fetch(url, { headers: { 'Accept': 'application/json' } });
      if (!r.ok) {
        const t = await r.text().catch(() => '');
        throw new Error(`HTTP ${r.status} @ ${url} :: ${t}`);
      }
      return r.json();
    }

    function norm(arr) {
      return (arr || [])
        .map(x => ({
          id: String(x?.id ?? x?.value ?? ''),
          text: String(x?.text ?? x?.name ?? x?.label ?? '')
        }))
        .filter(x => x.id);
    }

    function hasOptions(selectEl) {
      if (!selectEl) return false;
      // ha több mint 1 option van (placeholder + valami), akkor van adat
      return (selectEl.options?.length || 0) > 1;
    }

    function setNativeOptions(selectEl, items, placeholder) {
      if (!selectEl) return;
      selectEl.innerHTML = '';
      const ph = document.createElement('option');
      ph.value = '';
      ph.textContent = placeholder || '-- Válasszon --';
      selectEl.appendChild(ph);

      (items || []).forEach(i => {
        const opt = document.createElement('option');
        opt.value = String(i.id);
        opt.textContent = String(i.text);
        selectEl.appendChild(opt);
      });
    }

    function destroy(ts) { try { ts?.destroy(); } catch {} }

    function initSimpleTom(selectEl) {
      if (!selectEl) return null;
      return new TomSelect(selectEl, {
        dropdownParent: document.body,
        openOnFocus: true,
        closeAfterSelect: true,
        allowEmptyOption: true
      });
    }

    async function ensureStaticSelect(selectEl, url, placeholder) {
      if (!selectEl) return;
      if (hasOptions(selectEl)) return; // SSR-ből már tele van

      // különben API-ból töltjük
      try {
        const data = await fetchJson(url);
        const items = norm(data);
        setNativeOptions(selectEl, items, placeholder);
      } catch (e) {
        console.error('[TaskCreate] ensureStaticSelect failed', url, e);
        setNativeOptions(selectEl, [], placeholder || '-- Nincs adat --');
      }
    }

    function initPartnerTom() {
      if (!el.partner) return null;

      const ts = new TomSelect(el.partner, {
        dropdownParent: document.body,
        valueField: 'id',
        labelField: 'text',
        searchField: 'text',
        loadThrottle: 300,
        preload: false,
        closeAfterSelect: true,
        openOnFocus: true,
        load: async function (q, cb) {
          try {
            if (!q || q.length < 2) return cb();
            const data = await fetchJson(API.partners(q));
            cb(norm(data));
          } catch (e) {
            console.error('[TaskCreate] partner load failed', e);
            cb();
          }
        }
      });

      ts.on('change', async (val) => {
        currentPartnerId = val ? String(val) : '';
        console.log('[TaskCreate] partner changed', currentPartnerId);

        if (!tsSite) return;

        tsSite.clear(true);
        tsSite.clearOptions();

        if (!currentPartnerId) {
          tsSite.disable();
          return;
        }

        tsSite.enable();
        try {
          const data = await fetchJson(API.sitesByPartner(currentPartnerId));
          tsSite.addOptions(norm(data));
          tsSite.refreshOptions(false);
        } catch (e) {
          console.error('[TaskCreate] sites load failed', e);
        }
      });

      return ts;
    }

    function initSiteTom() {
      if (!el.site) return null;

      const ts = new TomSelect(el.site, {
        dropdownParent: document.body,
        valueField: 'id',
        labelField: 'text',
        searchField: 'text',
        closeAfterSelect: true,
        openOnFocus: true
      });

      ts.disable(); // partner nélkül
      return ts;
    }

    modalEl.addEventListener('shown.bs.modal', async () => {
      console.log('[TaskCreate] shown.bs.modal -> init TomSelects');

      // destroy all
      [tsType, tsStatus, tsPriority, tsAssignedTo, tsPartner, tsSite].forEach(destroy);
      tsType = tsStatus = tsPriority = tsAssignedTo = tsPartner = tsSite = null;
      currentPartnerId = '';

      // 1) töltsük fel a "statikus" selecteket, ha üresek
      await Promise.allSettled([
        ensureStaticSelect(el.type, API.taskTypes, '-- Válasszon típust --'),
        ensureStaticSelect(el.status, API.taskStatuses, '-- Válasszon státuszt --'),
        ensureStaticSelect(el.priority, API.taskPriorities, '-- Válasszon prioritást --'),
        ensureStaticSelect(el.assignedTo, API.users, '-- Nincs kijelölve --')
      ]);

      // 2) init TomSelect ezeken
      tsType = initSimpleTom(el.type);
      tsStatus = initSimpleTom(el.status);
      tsPriority = initSimpleTom(el.priority);
      tsAssignedTo = initSimpleTom(el.assignedTo);

      // 3) partner + site (keresős + cascade)
      tsPartner = initPartnerTom();
      tsSite = initSiteTom();
    });

    modalEl.addEventListener('hidden.bs.modal', () => {
      [tsType, tsStatus, tsPriority, tsAssignedTo, tsPartner, tsSite].forEach(destroy);
      tsType = tsStatus = tsPriority = tsAssignedTo = tsPartner = tsSite = null;
      currentPartnerId = '';
    });
  });
})();
