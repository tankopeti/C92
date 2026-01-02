document.addEventListener('DOMContentLoaded', () => {
  const modalEl = document.getElementById('createCommunicationModal');
  const saveBtn = document.getElementById('createCommunicationSaveBtn');

  if (!modalEl || !saveBtn) return;
  console.log('✅ createCommunication.js betöltve');

  saveBtn.addEventListener('click', async () => {
    try {
      const form = modalEl.querySelector('form');
      if (!form) return;

      if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return;
      }

      // értékek (TomSelect a select.value-t szinkronban tartja)
      const typeId = document.getElementById('createCommunicationTypeId')?.value;
      const partnerId = document.getElementById('createPartnerId')?.value;
      const siteId = document.getElementById('createSiteId')?.value || null;
      const responsibleSelect = document.getElementById('createResponsibleContactId');
    const responsibleId =
    responsibleSelect?.tomselect?.getValue?.() ||
    responsibleSelect?.value ||
    null;
    console.log('🧠 responsibleId(from TomSelect):', responsibleId);

      const date = document.getElementById('createDate')?.value;
      const statusId = document.getElementById('createStatusId')?.value;
      const subject = document.getElementById('createSubject')?.value;
      const note = document.getElementById('createNote')?.value || null;
      const metadata = document.getElementById('createMetadata')?.value || null;
      const initialPost = document.getElementById('createInitialPost')?.value || '';

const payload = {
  customerCommunicationId: 0,
  communicationTypeId: Number(typeId),
  partnerId: partnerId ? Number(partnerId) : null,
  siteId: siteId ? Number(siteId) : null,
  statusId: Number(statusId),
  date: date, // "yyyy-MM-dd"
  subject: subject,
  note: note,
  metadata: metadata,

  // ✅ ezek KELLENEK a DTO validáció miatt
  currentResponsible: responsibleId
    ? { responsibleId: responsibleId }
    : null,

  responsibleHistory: [],

  posts: initialPost.trim()
    ? [{ content: initialPost.trim() }]
    : []
};


      console.log('🟩 create payload:', payload);

      const token = getAntiForgeryToken(form);
      const res = await fetch('/api/CustomerCommunication', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'RequestVerificationToken': token
        },
        body: JSON.stringify(payload)
      });

      console.log('📡 create status:', res.status);

      if (!res.ok) {
        const raw = await res.text();
        console.error('❌ create raw:', raw);
        showMessage('Hiba a létrehozáskor. (Részletek a konzolban)');
        return;
      }

      // success
      bootstrap.Modal.getOrCreateInstance(modalEl).hide();
      showMessage('Sikeres létrehozás.');
      form.reset();
      form.classList.remove('was-validated');

      window.dispatchEvent(new Event('cc:reload'));
    } catch (err) {
      console.error('Create error:', err);
      showMessage('Hiba a létrehozáskor.');
    }
  });

  function getAntiForgeryToken(scopeEl) {
    const local = scopeEl?.querySelector('input[name="__RequestVerificationToken"]');
    if (local?.value) return local.value;
    const any = document.querySelector('input[name="__RequestVerificationToken"]');
    return any?.value || '';
  }

  function showMessage(text) {
    const body = document.getElementById('messageModalBody');
    const m = document.getElementById('messageModal');
    if (body) body.innerHTML = `<div>${escapeHtml(text)}</div>`;
    bootstrap.Modal.getOrCreateInstance(m).show();
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
