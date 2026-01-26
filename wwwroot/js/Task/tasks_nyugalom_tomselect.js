// wwwroot/js/Task/tasks_nyugalom_tomselect.js
document.addEventListener('DOMContentLoaded', () => {
  console.log('%c[NYUGALOM][TS] TomSelect modul aktív', 'color:#9c27b0;font-weight:bold;font-size:14px');

  // ---------------------- helpers ----------------------
  function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text == null ? '' : String(text);
    return div.innerHTML;
  }

  function ensureHiddenPartner(form, idAttr) {
    let partnerInput = document.getElementById(idAttr);
    if (!partnerInput) {
      partnerInput = document.createElement('input');
      partnerInput.type = 'hidden';
      partnerInput.name = 'PartnerId';
      partnerInput.id = idAttr;
      partnerInput.value = '';
      form.appendChild(partnerInput);
    }
    return partnerInput;
  }

  function fillTaskTypesOnce(form, selectSelector) {
    const taskTypeSelect = form.querySelector(selectSelector);
    if (!taskTypeSelect) return;

    if (taskTypeSelect.dataset.loaded) return;
    taskTypeSelect.dataset.loaded = '1';

    fetch('/api/tasks/tasktypes/select', { headers: { 'Accept': 'application/json' } })
      .then(r => (r.ok ? r.json() : Promise.reject(r)))
      .then(list => {
        const keepFirst = taskTypeSelect.querySelector('option[value=""]') || null;
        taskTypeSelect.innerHTML = '';
        if (keepFirst) taskTypeSelect.appendChild(keepFirst);
        else {
          const opt0 = document.createElement('option');
          opt0.value = '';
          opt0.textContent = '-- Válasszon --';
          taskTypeSelect.appendChild(opt0);
        }

        (list || []).forEach(x => {
          const opt = document.createElement('option');
          opt.value = x.id;
          opt.textContent = x.text;
          taskTypeSelect.appendChild(opt);
        });

        // ha valaki korábban már beleírta a value-t, triggereljük
        try { taskTypeSelect.dispatchEvent(new Event('change', { bubbles: true })); } catch { }
      })
      .catch(err => console.error('[NYUGALOM][TS] tasktypes load failed', err));
  }

  function initSiteTomSelect(modalEl, form, siteSelect, partnerInput) {
    if (!siteSelect) return null;

    if (siteSelect.tomselect) return siteSelect.tomselect;

    if (typeof TomSelect === 'undefined') {
      console.error('[NYUGALOM][TS] TomSelect nincs betöltve (TomSelect is undefined)');
      return null;
    }

    const ts = new TomSelect(siteSelect, {
      maxOptions: 300,
      valueField: 'id',
      labelField: 'text',
      searchField: ['text', 'partnerName', 'partnerDetails', 'phone'],
      placeholder: 'Keresés telephely névre, városra vagy partnerre...',
      load: function (query, callback) {
        if (!query || query.length < 2) return callback();
        fetch(`/api/nyugalom/sites/all/select?search=${encodeURIComponent(query)}`)
          .then(r => (r.ok ? r.json() : Promise.reject(r)))
          .then(callback)
          .catch(() => callback());
      },
      render: {
option: function (item, escape) {
  return `
    <div class="py-2 px-3">
      <div class="font-medium text-gray-900">
        ${escape(item.text)}
      </div>

      <div class="text-sm text-gray-600 mt-1">
        Partner: <strong>${escape(item.partnerDetails || '–')}</strong>
      </div>

      ${item.phone ? `
        <div class="text-sm text-gray-500 mt-1">
          📞 ${escape(item.phone)}
        </div>
      ` : ''}
    </div>
  `;
}
,
item: function (item, escape) {
  return `
    <div class="inline-flex items-center gap-2">
      <span class="font-medium">${escape(item.text)}</span>

      ${item.phone ? `
        <span class="text-xs text-gray-500">
          📞 ${escape(item.phone)}
        </span>
      ` : ''}

      <span class="text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
        ${escape(item.partnerName || 'nincs partner')}
      </span>
    </div>
  `;
}
,
        no_results: function () {
          return '<div class="py-2 px-3 text-center text-gray-500">Nincs találat</div>';
        }
      },
      onItemAdd: function (value) {
        const item = this.options[value];
        if (!item) return;

        partnerInput.value = item.partnerId || '';

        const infoEl = modalEl.querySelector('.selected-partner-info, #selected-partner-info');
        if (infoEl) {
          infoEl.innerHTML = `Partner: <strong>${escapeHtml(item.partnerDetails || '–')}</strong>`;
          infoEl.classList.remove('d-none');
        }
      },
      onItemRemove: function () {
        partnerInput.value = '';
        const infoEl = modalEl.querySelector('.selected-partner-info, #selected-partner-info');
        if (infoEl) infoEl.classList.add('d-none');
      }
    });

    return ts;
  }

  // ====================== 1) CREATE MODAL ======================
  const createModalEl = document.getElementById('newTaskModal');
  if (!createModalEl) {
    console.warn('[NYUGALOM][TS] Modal nem található: #newTaskModal');
  } else {
    let createSiteTS = null;

    createModalEl.addEventListener('shown.bs.modal', () => {
      const form = createModalEl.querySelector('form');
      if (!form) return;

      fillTaskTypesOnce(form, '#TaskTypePMId, select[name="TaskTypePMId"]');

      const partnerInput = ensureHiddenPartner(form, 'autoPartnerId');

      const siteSelect = form.querySelector('#SiteId, select[name="SiteId"]');
      createSiteTS = initSiteTomSelect(createModalEl, form, siteSelect, partnerInput);
    });

    createModalEl.addEventListener('hidden.bs.modal', () => {
      if (createSiteTS) {
        createSiteTS.clear();
        createSiteTS.clearOptions();
      }
    });
  }

  // ====================== 2) EDIT MODAL ======================
  const editModalEl = document.getElementById('editTaskModal');
  if (!editModalEl) {
    console.warn('[NYUGALOM][TS] Modal nem található: #editTaskModal (edit TomSelect skip)');
  } else {
    let editSiteTS = null;

    editModalEl.addEventListener('shown.bs.modal', () => {
      const form = editModalEl.querySelector('form');
      if (!form) return;

      // Edit TaskTypes feltöltés
      fillTaskTypesOnce(form, '#EditTaskTypePMId, select[name="TaskTypePMId"]');

      // Edit partner hidden
      const partnerInput = ensureHiddenPartner(form, 'editAutoPartnerId');

      // Edit Site TomSelect
      const siteSelect = form.querySelector('#EditSiteId, select[name="SiteId"]');
      editSiteTS = initSiteTomSelect(editModalEl, form, siteSelect, partnerInput);
    });

    editModalEl.addEventListener('hidden.bs.modal', () => {
      if (editSiteTS) {
        editSiteTS.clear();
        editSiteTS.clearOptions();
      }
    });
  }

  // ====================== 3) FILTER MODAL ======================
  const advancedFilterModalEl = document.getElementById('advancedFilterModal');
  if (advancedFilterModalEl) {
    console.log('%c[NYUGALOM][TS] Filter modal megtalálva', 'color: orange; font-weight: bold');

    let siteTomSelectInFilter = null;

    advancedFilterModalEl.addEventListener('shown.bs.modal', function () {
      const siteSelect = advancedFilterModalEl.querySelector('select[name="siteId"]');
      if (!siteSelect) return;

      if (siteSelect.tomselect) {
        siteTomSelectInFilter = siteSelect.tomselect;
        return;
      }

      if (typeof TomSelect === 'undefined') {
        console.error('[NYUGALOM][TS] TomSelect nincs betöltve (filter)');
        return;
      }

      try {
        siteTomSelectInFilter = new TomSelect(siteSelect, {
          maxOptions: 300,
          valueField: 'id',
          labelField: 'text',
          searchField: ['text', 'partnerName', 'partnerDetails', 'phone'],
          placeholder: 'Keresés telephely névre, városra vagy partnerre...',
          load: function (query, callback) {
            if (!query || query.length < 2) return callback();
            fetch(`/api/nyugalom/sites/all/select?search=${encodeURIComponent(query)}`)
              .then(r => (r.ok ? r.json() : Promise.reject(r)))
              .then(callback)
              .catch(() => callback());
          },
          render: {
option: function (item, escape) {
  return `
    <div class="py-2 px-3">
      <div class="font-medium text-gray-900">
        ${escape(item.text)}
      </div>

      <div class="text-sm text-gray-600 mt-1">
        Partner: <strong>${escape(item.partnerDetails || '–')}</strong>
      </div>

      ${item.phone ? `
        <div class="text-sm text-gray-500 mt-1">
          📞 ${escape(item.phone)}
        </div>
      ` : ''}
    </div>
  `;
}
,
item: function (item, escape) {
  return `
    <div class="inline-flex items-center gap-2">
      <span class="font-medium">${escape(item.text)}</span>

      ${item.phone ? `
        <span class="text-xs text-gray-500">
          📞 ${escape(item.phone)}
        </span>
      ` : ''}

      <span class="text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
        ${escape(item.partnerName || 'nincs partner')}
      </span>
    </div>
  `;
}
,
            no_results: function () {
              return '<div class="py-2 px-3 text-center text-gray-500">Nincs találat</div>';
            }
          }
        });
      } catch (err) {
        console.error('[NYUGALOM][TS] filter TomSelect init exception', err);
      }
    });

    advancedFilterModalEl.addEventListener('hidden.bs.modal', function () {
      if (siteTomSelectInFilter) {
        siteTomSelectInFilter.clear();
        siteTomSelectInFilter.clearOptions();
      }
    });
  }
});
