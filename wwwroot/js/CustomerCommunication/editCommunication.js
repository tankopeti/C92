document.addEventListener('DOMContentLoaded', () => {
  const tbody = document.getElementById('communicationsTableBody');
  const editModalEl = document.getElementById('editCommunicationModal');
  const form = document.getElementById('editCommunicationForm');

  const idEl = document.getElementById('editCommunicationId');
  const dateEl = document.getElementById('editDate');
  const subjectEl = document.getElementById('editSubject');
  const noteEl = document.getElementById('editNote');
  const metaEl = document.getElementById('editMetadata');

  const typeEl = document.getElementById('editCommunicationTypeId');
  const partnerEl = document.getElementById('editPartnerId');
  const siteEl = document.getElementById('editSiteId');
  const respEl = document.getElementById('editResponsibleContactId'); // id maradhat
  const statusEl = document.getElementById('editStatusId');

  if (!tbody) return;
  console.log('✅ editCommunication.js betöltve');

  tbody.addEventListener('click', async (e) => {
    const a = e.target.closest('.edit-communication-btn');
    if (!a) return;

    const id = a.getAttribute('data-communication-id');
    if (!id) return;

    e.preventDefault();
    console.log('✏️ edit communication:', id);

    try {
      const url = `/api/CustomerCommunicationGet?id=${encodeURIComponent(id)}`;
      const res = await fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const c = await res.json();

      console.log('🟦 edit GET raw:', c);

      window.CustomerCommunicationTomSelect?.resetEdit?.();

      // sima mezők
      if (idEl) idEl.value = String(getVal(c, ['customerCommunicationId', 'CustomerCommunicationId']) ?? id);
      if (dateEl) dateEl.value = String(getVal(c, ['dateIso', 'DateIso', 'date', 'Date']) ?? '').slice(0, 10);
      if (subjectEl) subjectEl.value = String(getVal(c, ['subject', 'Subject']) ?? '');
      if (noteEl) noteEl.value = String(getVal(c, ['note', 'Note']) ?? '');
      if (metaEl) metaEl.value = String(getVal(c, ['metadata', 'Metadata']) ?? '');

      // modal show -> TomSelect init biztos
      bootstrap.Modal.getOrCreateInstance(editModalEl).show();
      window.CustomerCommunicationTomSelect?.ensureEditInitialized?.(editModalEl);

      // TomSelect map (mind camelCase kulcsokkal!)
      const mapped = {
        communicationTypeId: getVal(c, ['communicationTypeId', 'CommunicationTypeId']),
        communicationTypeName: getVal(c, ['communicationTypeName', 'CommunicationTypeName', 'typeName', 'TypeName']),

        statusId: getVal(c, ['statusId', 'StatusId']),
        statusName: getVal(c, ['statusName', 'StatusName']),

        partnerId: getVal(c, ['partnerId', 'PartnerId']),
        partnerName: getVal(c, ['partnerName', 'PartnerName']),

        siteId: getVal(c, ['siteId', 'SiteId']),
        siteName: getVal(c, ['siteName', 'SiteName']),

        // felelős: nálad többféle lehet
        responsibleId: getVal(c, ['responsibleId', 'ResponsibleId', 'responsibleContactId', 'ResponsibleContactId']),
        responsibleName: getVal(c, ['responsibleName', 'ResponsibleName', 'agentName', 'AgentName'])
      };

      console.log('🧩 edit mapped:', mapped);

      // apply (TomSelect)
      await window.CustomerCommunicationTomSelect?.applyEditValues?.(mapped);

    } catch (err) {
      console.error('Edit load error:', err);
      showMessage('Hiba a szerkesztési adatok betöltésekor.');
    }
  });

  form?.addEventListener('submit', async (e) => {
    e.preventDefault();

    if (!form.checkValidity()) {
      form.classList.add('was-validated');
      return;
    }

const ts = respEl?.tomselect || respEl?._tomselect;
const responsibleContactId = ts ? ts.getValue() : (respEl?.value || '');

const payload = {
  customerCommunicationId: Number(idEl?.value || 0),

  communicationTypeId: typeEl?.value ? Number(typeEl.value) : null,
  partnerId: partnerEl?.value ? Number(partnerEl.value) : null,
  siteId: siteEl?.value ? Number(siteEl.value) : null,

  // ✅ EZT várja a backend
  responsibleContactId: responsibleContactId || null,

  dateIso: dateEl?.value || null,
  statusId: statusEl?.value ? Number(statusEl.value) : null,

  subject: subjectEl?.value || '',
  note: noteEl?.value || null,
  metadata: metaEl?.value || null
};



    console.log('💾 save payload:', payload);

    try {
      const token = getAntiForgeryToken(form);
      const res = await fetch('/api/CustomerCommunicationUpdate', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'RequestVerificationToken': token
        },
        body: JSON.stringify(payload)
      });

      console.log('📡 update status:', res.status);

      if (!res.ok) {
        const raw = await res.text();
        console.error('❌ update raw:', raw);
        throw new Error(`HTTP ${res.status}`);
      }

      bootstrap.Modal.getOrCreateInstance(editModalEl).hide();
      showMessage('Sikeres mentés.');
      window.dispatchEvent(new Event('cc:reload'));
    } catch (err) {
      console.error('Save error:', err);
      showMessage('Hiba mentés közben.');
    }
  });

  function getVal(obj, keys) {
    for (const k of keys) {
      if (obj && Object.prototype.hasOwnProperty.call(obj, k)) return obj[k];
    }
    return undefined;
  }

  function getAntiForgeryToken(scopeEl) {
    const local = scopeEl?.querySelector('input[name="__RequestVerificationToken"]');
    if (local?.value) return local.value;
    const any = document.querySelector('input[name="__RequestVerificationToken"]');
    return any?.value || '';
  }

  function showMessage(text) {
    const body = document.getElementById('messageModalBody');
    const modalEl = document.getElementById('messageModal');
    if (body) body.innerHTML = `<div>${escapeHtml(text)}</div>`;
    bootstrap.Modal.getOrCreateInstance(modalEl).show();
  }

  function escapeHtml(str) {
    return String(str ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }
});
