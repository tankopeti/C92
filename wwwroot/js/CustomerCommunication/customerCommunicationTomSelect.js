document.addEventListener('DOMContentLoaded', () => {
  console.log('✅ customerCommunicationTomSelect.js betöltve (create/edit full lookups)');

  if (typeof TomSelect === 'undefined') {
    console.error('❌ TomSelect nincs betöltve! (TomSelect undefined)');
    return;
  }

  // Expose small API for editCommunication.js
  window.CustomerCommunicationTomSelect = {
    ensureEditInitialized,
    applyEditValues,     // call this from editCommunication.js after you fetch the record
    resetEdit
  };

  document.addEventListener('shown.bs.modal', (ev) => {
    const modal = ev.target?.closest?.('.modal') || ev.target;
    if (!modal?.id) return;

    if (modal.id === 'createCommunicationModal') initCreate(modal);
    if (modal.id === 'editCommunicationModal') ensureEditInitialized(modal);
    if (modal.id === 'filterModal') initFilter(modal);
  });


  // fallback
  const createModal = document.getElementById('createCommunicationModal');
  if (createModal) initCreate(createModal);

  const editModal = document.getElementById('editCommunicationModal');
  if (editModal) ensureEditInitialized(editModal);

  // -------------------------
  // CREATE
  // -------------------------
  function initCreate(modalEl) {
    if (modalEl.dataset.ccTsCreateReady === '1') return;

    const els = {
      type: modalEl.querySelector('#createCommunicationTypeId'),
      partner: modalEl.querySelector('#createPartnerId'),
      site: modalEl.querySelector('#createSiteId'),
      responsible: modalEl.querySelector('#createResponsibleContactId'),
      status: modalEl.querySelector('#createStatusId')
    };

    console.log('🔧 initCreate', mapExists(els));
    if (!allRequired(els)) return;

    modalEl.dataset.ccTsCreateReady = '1';

    // destroy old
    Object.values(els).forEach(destroyIfExists);

    // Init TomSelects
    const typeTs = createStaticLookupTomSelect(els.type, '/api/CustomerCommunication/types', {
      placeholder: '— Válasszon típust —'
    });

    const statusTs = createStaticLookupTomSelect(els.status, '/api/CustomerCommunication/statuses', {
      placeholder: '— Válasszon státuszt —'
    });

    const responsibleTs = createUsersTomSelect(els.responsible, '/api/CustomerCommunication', {
      placeholder: '— Válasszon felelőst —'
    });

    // Partner előbb, utána Site (stabil)
    const partnerTs = els.partner
      ? createPartnersTomSelect(els.partner, (partnerId) => {
          if (siteTs) onPartnerChanged(partnerId, siteTs);
        })
      : null;

    const siteTs = els.site
      ? createSitesTomSelect(els.site, () => partnerTs?.getValue() || '')
      : null;

    if (partnerTs && siteTs && !partnerTs.getValue()) siteTs.disable();

  }

    // -------------------------
  // FILTER (Advanced)
  // -------------------------
  function initFilter(modalEl) {
    if (modalEl.dataset.ccTsFilterReady === '1') return;

    const els = {
      type: modalEl.querySelector('#filterTypeId'),
      partner: modalEl.querySelector('#filterPartnerId'),
      site: modalEl.querySelector('#filterSiteId'),
      responsible: modalEl.querySelector('#filterResponsibleId'),
      status: modalEl.querySelector('#filterStatusId')
    };

    console.log('🔧 initFilter', mapExists(els));

    // itt lehet, hogy nem mind az 5 mező van még bent a modalban → legyen "soft"
    // de ha te már mindet beraktad, maradhat a strict is.
    const hasAny = !!(els.type || els.partner || els.site || els.responsible || els.status);
    if (!hasAny) return;

    modalEl.dataset.ccTsFilterReady = '1';

    Object.values(els).forEach(destroyIfExists);

    // ugyanazok az endpointok / builder-ek
    const typeTs = els.type
      ? createStaticLookupTomSelect(els.type, '/api/CustomerCommunication/types', { placeholder: '— Típus —' })
      : null;

    const statusTs = els.status
      ? createStaticLookupTomSelect(els.status, '/api/CustomerCommunication/statuses', { placeholder: '— Státusz —' })
      : null;

    const responsibleTs = els.responsible
      ? createUsersTomSelect(els.responsible, '/api/CustomerCommunication', { placeholder: '— Felelős —' })
      : null;

    // partner-site függés
    const siteTs = els.site
      ? createSitesTomSelect(els.site, () => partnerTs?.getValue())
      : null;

    const partnerTs = els.partner
      ? createPartnersTomSelect(els.partner, (partnerId) => {
          if (siteTs) onPartnerChanged(partnerId, siteTs);
        })
      : null;

    if (partnerTs && siteTs && !partnerTs.getValue()) siteTs.disable();

    // eltesszük ref-nek, hogy máshonnan is elérhető legyen
    modalEl._ccTsFilter = { els, typeTs, statusTs, responsibleTs, partnerTs, siteTs };
  }


  // -------------------------
  // EDIT
  // -------------------------
  function ensureEditInitialized(modalEl = document.getElementById('editCommunicationModal')) {
    if (!modalEl) return;
    if (modalEl.dataset.ccTsEditReady === '1') return;

    const els = {
      type: modalEl.querySelector('#editCommunicationTypeId'),
      partner: modalEl.querySelector('#editPartnerId'),
      site: modalEl.querySelector('#editSiteId'),
      responsible: modalEl.querySelector('#editResponsibleContactId'),
      status: modalEl.querySelector('#editStatusId')
    };

    console.log('🔧 ensureEditInitialized', mapExists(els));
    if (!allRequired(els)) return;

    modalEl.dataset.ccTsEditReady = '1';

    Object.values(els).forEach(destroyIfExists);

    const typeTs = createStaticLookupTomSelect(els.type, '/api/CustomerCommunication/types', {
      placeholder: '— Válasszon típust —'
    });

    const statusTs = createStaticLookupTomSelect(els.status, '/api/CustomerCommunication/statuses', {
      placeholder: '— Válasszon státuszt —'
    });

    const responsibleTs = createUsersTomSelect(els.responsible, '/api/CustomerCommunication', {
      placeholder: '— Válasszon felelőst —'
    });

    const siteTs = createSitesTomSelect(els.site, () => partnerTs.getValue());

    const partnerTs = createPartnersTomSelect(els.partner, (partnerId) => {
      onPartnerChanged(partnerId, siteTs);
    });

    // store references for later applyEditValues()
    modalEl._ccTs = { els, typeTs, statusTs, responsibleTs, partnerTs, siteTs };

    if (!partnerTs.getValue()) siteTs.disable();
  }

  function resetEdit() {
    const modalEl = document.getElementById('editCommunicationModal');
    if (!modalEl?._ccTs) return;

    const { typeTs, statusTs, responsibleTs, partnerTs, siteTs } = modalEl._ccTs;

    typeTs.clear(true);
    statusTs.clear(true);
    responsibleTs.clear(true);
    partnerTs.clear(true);
    siteTs.clear(true);
    siteTs.clearOptions();
    siteTs.disable();
  }

  /**
   * Ezt hívd meg editCommunication.js-ből a rekord betöltése után.
   * Várt mezők (ha van): communicationTypeId + communicationTypeName, statusId + statusName,
   * partnerId + partnerName, siteId + siteName, responsibleId + responsibleName
   */
  async function applyEditValues(dto) {
    const modalEl = document.getElementById('editCommunicationModal');
    if (!modalEl) return;

    ensureEditInitialized(modalEl);

    const refs = modalEl._ccTs;
    if (!refs) {
      console.warn('⚠️ Edit TomSelect refs missing');
      return;
    }

    const { typeTs, statusTs, responsibleTs, partnerTs, siteTs } = refs;

    // Type
    setTsValue(typeTs, dto.communicationTypeId, dto.communicationTypeName);

    // Status
    setTsValue(statusTs, dto.statusId, dto.statusName);

    // Responsible (Identity user)
    // nálad lehet responsibleContactId / responsibleId - itt rugalmasan kezeljük
    const respId =
                dto.responsibleId ||
                dto.responsibleContactId ||
                dto.currentResponsible?.responsibleId ||
                '';

const respName =
  dto.responsibleName ||
  dto.currentResponsible?.responsibleName ||
  dto.agentName ||
  '';

  setTsValue(responsibleTs, respId, respName);

    // Partner + Site (függés)
    const partnerId = dto.partnerId || '';
    const partnerName = dto.partnerName || '';
    const siteId = dto.siteId || '';
    const siteName = dto.siteName || '';

    if (!partnerId) {
      partnerTs.clear(true);
      siteTs.clear(true);
      siteTs.disable();
      return;
    }

    // partner beállítás
    setTsValue(partnerTs, partnerId, partnerName);

    // site előkészítés
    siteTs.enable();
    siteTs.clearOptions();

    // Beállíthatjuk azonnal a site-ot (nem kell megvárni a load-ot, de listát is kérünk)
    if (siteId) setTsValue(siteTs, siteId, siteName);

    // Betöltjük a partnerhez tartozó site listát (hogy utána kereshető legyen)
    await loadTomSelectOptions(siteTs, '');
  }

  // -------------------------
  // TomSelect builders
  // -------------------------
  function createPartnersTomSelect(selectEl, onChangeCb) {
    return new TomSelect(selectEl, {
        dropdownParent: 'body',
      valueField: 'id',
      labelField: 'text',
      searchField: ['text'],
      placeholder: '— Válasszon partnert —',
      preload: true,
      closeAfterSelect: true,
      allowEmptyOption: true,
      maxOptions: 50,
      load: async function (query, callback) {
        try {
          const url = buildUrl('/api/Lookups/Partners', { search: query || '' });
          const res = await fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
          if (!res.ok) return callback([]);
          const data = await res.json();
          callback(Array.isArray(data) ? data : []);
        } catch (e) {
          console.error('❌ partners load error', e);
          callback([]);
        }
      },
      onChange: (value) => onChangeCb?.(value || '')
    });
  }

  function createSitesTomSelect(selectEl, getPartnerId) {
    return new TomSelect(selectEl, {
        dropdownParent: 'body',
      valueField: 'id',
      labelField: 'text',
      searchField: ['text'],
      placeholder: '— Válasszon telephelyet —',
      preload: false,
      closeAfterSelect: true,
      allowEmptyOption: true,
      maxOptions: 50,
      load: async function (query, callback) {
        try {
          const partnerId = getPartnerId?.() || '';
          if (!partnerId) return callback([]);

          const url = buildUrl('/api/Lookups/Sites', { partnerId, search: query || '' });
          const res = await fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
          if (!res.ok) return callback([]);
          const data = await res.json();
          callback(Array.isArray(data) ? data : []);
        } catch (e) {
          console.error('❌ sites load error', e);
          callback([]);
        }
      }
    });
  }

  // types/statuses: a controllered ezeket adja {id,text}
  function createStaticLookupTomSelect(selectEl, url, opts = {}) {
    return new TomSelect(selectEl, {
        dropdownParent: 'body',
      valueField: 'id',
      labelField: 'text',
      searchField: ['text'],
      placeholder: opts.placeholder || '— Válasszon —',
      preload: true,
      closeAfterSelect: true,
      allowEmptyOption: true,
      maxOptions: 100,
      load: async function (query, callback) {
        try {
          // itt általában nem kell query, de nem baj
          const res = await fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
          if (!res.ok) return callback([]);
          const data = await res.json();
          callback(Array.isArray(data) ? data : []);
        } catch (e) {
          console.error('❌ lookup load error', { url, e });
          callback([]);
        }
      }
    });
  }

  // users: a controller GET /api/CustomerCommunication -> {Id, Name}
  function createUsersTomSelect(selectEl, url, opts = {}) {
    return new TomSelect(selectEl, {
        dropdownParent: 'body',
      valueField: 'id',
      labelField: 'text',
      searchField: ['text'],
      placeholder: opts.placeholder || '— Válasszon —',
      preload: true,
      closeAfterSelect: true,
      allowEmptyOption: true,
      maxOptions: 100,
      load: async function (query, callback) {
        try {
          const res = await fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
          if (!res.ok) return callback([]);

          const data = await res.json();
          // map: {Id, Name} -> {id, text}
          const mapped = Array.isArray(data)
            ? data.map(u => ({ id: u.id ?? u.Id ?? '', text: u.name ?? u.Name ?? '' }))
            : [];

          // client-side szűrés query-re (mert endpoint most nem keres)
          const q = (query || '').toLowerCase().trim();
          const filtered = q
            ? mapped.filter(x => (x.text || '').toLowerCase().includes(q))
            : mapped;

          callback(filtered);
        } catch (e) {
          console.error('❌ users load error', e);
          callback([]);
        }
      }
    });
  }

  // -------------------------
  // helpers
  // -------------------------
  function onPartnerChanged(partnerId, siteTs) {
    siteTs.clear(true);
    siteTs.clearOptions();

    if (!partnerId) {
      siteTs.disable();
      return;
    }

    siteTs.enable();
    siteTs.load('');
  }

  function setTsValue(ts, id, text) {
    if (!ts) return;
    if (id === undefined || id === null || String(id).trim() === '') {
      ts.clear(true);
      return;
    }
    const sid = String(id);
    const st = String(text || sid);
    ts.addOption({ id: sid, text: st });
    ts.setValue(sid, true);
  }

  function loadTomSelectOptions(ts, query) {
    return new Promise((resolve) => {
      try {
        ts.load(query || '', () => resolve());
      } catch {
        resolve();
      }
    });
  }

  function destroyIfExists(selectEl) {
    if (selectEl?.tomselect) {
      try { selectEl.tomselect.destroy(); } catch { }
    }
  }

  function buildUrl(base, params) {
    const p = new URLSearchParams();
    Object.entries(params || {}).forEach(([k, v]) => {
      if (v === undefined || v === null) return;
      const s = String(v);
      if (k !== 'search' && s.trim() === '') return;
      p.set(k, s);
    });
    return `${base}?${p.toString()}`;
  }

  function mapExists(obj) {
    const o = {};
    Object.entries(obj).forEach(([k, v]) => (o[k] = !!v));
    return o;
  }

  function allRequired(els) {
    // mind kell, mert azt kérted: type/status/responsible is TomSelect
    return !!(els.type && els.partner && els.site && els.responsible && els.status);
  }
});
