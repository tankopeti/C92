// wwwroot/js/CustomerCommunication/historyCommunication.js
document.addEventListener('DOMContentLoaded', () => {
  const tbody = document.getElementById('communicationsTableBody');
  const modalEl = document.getElementById('historyCommunicationModal');
  const tbodyEl = document.getElementById('historyCommunicationTbody');
  const subtitleEl = document.getElementById('historyCommunicationSubtitle');

  if (!tbody || !modalEl || !tbodyEl) return;

  console.log('✅ historyCommunication.js betöltve');

  tbody.addEventListener('click', async (e) => {
    const a = e.target.closest('.history-communication-btn');
    if (!a) return;

    e.preventDefault();
    const id = a.getAttribute('data-communication-id');
    if (!id) return;

    // modal nyitás azonnal + loading
    tbodyEl.innerHTML = `
      <tr>
        <td colspan="4" class="text-center py-4 text-muted">Betöltés...</td>
      </tr>`;
    if (subtitleEl) subtitleEl.textContent = `Kommunikáció #${id}`;

    bootstrap.Modal.getOrCreateInstance(modalEl).show();

    try {
      // ✅ ezt a controllerben adjuk hozzá: /api/CustomerCommunicationHistory?id=...
      const url = `/api/CustomerCommunicationHistory?id=${encodeURIComponent(id)}`;
      const res = await fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();

      if (!Array.isArray(data) || data.length === 0) {
        tbodyEl.innerHTML = `
          <tr>
            <td colspan="4" class="text-center py-4 text-muted">Nincs előzmény.</td>
          </tr>`;
        return;
      }

      tbodyEl.innerHTML = '';
      data.forEach(r => addRow(r));
    } catch (err) {
      console.error('History load error:', err);
      tbodyEl.innerHTML = `
        <tr>
          <td colspan="4" class="text-center py-4 text-danger">Hiba az előzmények betöltésekor.</td>
        </tr>`;
    }
  });

  function addRow(r) {
    const dt = r.changedAtText || '—';
    const action = r.action || '—';
    const user = r.changedByName || '—';
    const changes = r.changes || '—';

    tbodyEl.insertAdjacentHTML('beforeend', `
      <tr>
        <td class="text-nowrap">${escapeHtml(dt)}</td>
        <td class="text-nowrap"><span class="badge bg-secondary">${escapeHtml(action)}</span></td>
        <td class="text-nowrap">${escapeHtml(user)}</td>
        <td>${escapeHtml(changes)}</td>
      </tr>
    `);
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
