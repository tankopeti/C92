// wwwroot/js/CustomerCommunication/viewCommunication.js
document.addEventListener('DOMContentLoaded', () => {
  const tbody = document.getElementById('communicationsTableBody');
  const viewModalEl = document.getElementById('viewCommunicationModal');
  const viewContent = document.getElementById('viewCommunicationContent');

  if (!tbody) return;

  console.log('✅ viewCommunication.js betöltve');

  tbody.addEventListener('click', async (e) => {
    const btn = e.target.closest('.view-communication-btn');
    if (!btn) return;

    const id = btn.getAttribute('data-communication-id');
    if (!id) return;

    e.preventDefault();
    console.log('👁️ view communication:', id);

    if (viewContent) viewContent.innerHTML = `<div class="text-muted py-4">Betöltés...</div>`;

    try {
      // ✅ Endpoint: HTML partial (Sites mintára viewSiteContent)
      const url = `/api/CustomerCommunicationView?id=${encodeURIComponent(id)}`;
      const res = await fetch(url, { credentials: 'same-origin' });
      if (!res.ok) {
        const raw = await res.text();
        console.error('❌ view API raw:', raw);
        throw new Error(`HTTP ${res.status}`);
      }

      const html = await res.text();
      if (viewContent) viewContent.innerHTML = html;

      const modal = bootstrap.Modal.getOrCreateInstance(viewModalEl);
      modal.show();
    } catch (err) {
      console.error('View communication error:', err);
      showMessage('Hiba a kommunikáció betöltésekor.');
    }
  });

  function showMessage(text) {
    const body = document.getElementById('messageModalBody');
    const modalEl = document.getElementById('messageModal');
    if (body) body.innerHTML = `<div class="text-danger">${escapeHtml(text)}</div>`;
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
