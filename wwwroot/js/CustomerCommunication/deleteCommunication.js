// wwwroot/js/CustomerCommunication/deleteCommunication.js
document.addEventListener('DOMContentLoaded', () => {
  const tbody = document.getElementById('communicationsTableBody');
  const deleteModalEl = document.getElementById('deleteCommunicationModal');

  const subjectEl = document.getElementById('deleteCommunicationSubject');
  const idEl = document.getElementById('deleteCommunicationId');
  const confirmBtn = document.getElementById('confirmDeleteCommunicationBtn');

  if (!tbody) return;

  console.log('✅ deleteCommunication.js betöltve');

  tbody.addEventListener('click', (e) => {
    const a = e.target.closest('.delete-communication-btn');
    if (!a) return;

    e.preventDefault();

    const id = a.getAttribute('data-communication-id');
    const subject = a.getAttribute('data-subject') || '—';

    console.log('🗑️ open delete modal:', { id, subject });

    if (idEl) idEl.value = id || '';
    if (subjectEl) subjectEl.textContent = subject;

    bootstrap.Modal.getOrCreateInstance(deleteModalEl).show();
  });

  confirmBtn?.addEventListener('click', async () => {
    const id = idEl?.value;
    if (!id) return;

    console.log('🗑️ confirm delete:', id);

    try {
      const token = getAntiForgeryToken(deleteModalEl);
      const url = `/api/CustomerCommunicationDelete?id=${encodeURIComponent(id)}`;

      const res = await fetch(url, {
        method: 'DELETE',
        credentials: 'same-origin',
        headers: { 'RequestVerificationToken': token }
      });

      console.log('📡 delete status:', res.status);

      if (!res.ok) {
        const raw = await res.text();
        console.error('❌ delete raw:', raw);
        throw new Error(`HTTP ${res.status}`);
      }

      bootstrap.Modal.getOrCreateInstance(deleteModalEl).hide();
      showMessage('Sikeres törlés.');
      window.dispatchEvent(new Event('cc:reload'));
    } catch (err) {
      console.error('Delete error:', err);
      showMessage('Hiba törlés közben.');
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
