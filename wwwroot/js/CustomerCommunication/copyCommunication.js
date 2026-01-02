// wwwroot/js/CustomerCommunication/copyCommunication.js
document.addEventListener('DOMContentLoaded', () => {
  const tbody = document.getElementById('communicationsTableBody');
  if (!tbody) return;

  console.log('✅ copyCommunication.js betöltve');

  tbody.addEventListener('click', async (e) => {
    const a = e.target.closest('.copy-communication-btn');
    if (!a) return;

    e.preventDefault();

    const id = a.getAttribute('data-communication-id');
    if (!id) return;

    console.log('📋 copy communication:', id);

    try {
      const token = getAntiForgeryToken(document.body);

      const res = await fetch('/api/CustomerCommunicationCopy', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'RequestVerificationToken': token
        },
        body: JSON.stringify({ id: id })
      });

      console.log('📡 copy status:', res.status);

      if (!res.ok) {
        const raw = await res.text();
        console.error('❌ copy raw:', raw);
        throw new Error(`HTTP ${res.status}`);
      }

      showMessage('Kommunikáció másolva.');
      window.dispatchEvent(new Event('cc:reload'));
    } catch (err) {
      console.error('Copy error:', err);
      showMessage('Hiba másolás közben.');
    }
  });

  function getAntiForgeryToken(scopeEl) {
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
