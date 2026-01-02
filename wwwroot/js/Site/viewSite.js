// /js/Site/viewSite.js
document.addEventListener('DOMContentLoaded', () => {
  document.addEventListener('click', async (e) => {
    const btn = e.target.closest('.view-site-btn');
    if (!btn) return;

    const siteId = btn.dataset.siteId;
    if (!siteId) return;

    const modalEl = document.getElementById('viewSiteModal');
    const contentEl = document.getElementById('viewSiteContent');
    if (!modalEl || !contentEl) return;

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
    contentEl.innerHTML = loadingHtml('Adatok betöltése...');

    try {
      const res = await fetch(`/api/SitesIndex/${encodeURIComponent(siteId)}`, {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });
      if (!res.ok) throw new Error(res.status === 404 ? 'Telephely nem található' : `HTTP ${res.status}`);

      const d = await res.json();

      contentEl.innerHTML = `
        <div class="container-fluid">
          <div class="d-flex justify-content-between align-items-start gap-3 mb-3">
            <div>
              <h4 class="fw-bold mb-1">${escapeHtml(d.siteName || d.SiteName || 'Telephely')}</h4>
              <div class="text-muted small">ID: <strong>${escapeHtml(String(d.siteId ?? d.SiteId ?? '—'))}</strong></div>
            </div>
            <div class="text-end">
              <div>
                ${renderStatus(d.status || d.Status)}
              </div>
              <div class="text-muted small mt-1">
                ${d.isPrimary || d.IsPrimary ? '<span class="badge bg-primary">Elsődleges</span>' : ''}
              </div>
            </div>
          </div>

          ${section('Partner', `
            <div class="row g-3">
              ${kv('Partner', escapeHtml(d.partner?.companyName || d.partner?.name || d.Partner?.CompanyName || d.Partner?.Name || '—'))}
              ${kv('PartnerId', escapeHtml(String(d.partnerId ?? d.PartnerId ?? '—')))}
            </div>
          `)}

          ${section('Cím', `
            <div class="row g-3">
              ${kv('Cím 1', escapeHtml(d.addressLine1 || d.AddressLine1 || '—'))}
              ${kv('Cím 2', escapeHtml(d.addressLine2 || d.AddressLine2 || '—'))}
              ${kv('Város', escapeHtml(d.city || d.City || '—'))}
              ${kv('Megye', escapeHtml(d.state || d.State || '—'))}
              ${kv('Irányítószám', escapeHtml(d.postalCode || d.PostalCode || '—'))}
              ${kv('Ország', escapeHtml(d.country || d.Country || '—'))}
            </div>
          `)}

          ${section('Kapcsolattartók', `
            <div class="row g-3">
              ${kv('Kapcsolattartó 1', escapeHtml(d.contactPerson1 || d.ContactPerson1 || '—'))}
              ${kv('Kapcsolattartó 2', escapeHtml(d.contactPerson2 || d.ContactPerson2 || '—'))}
              ${kv('Kapcsolattartó 3', escapeHtml(d.contactPerson3 || d.ContactPerson3 || '—'))}
            </div>
          `)}

          ${section('Megjegyzések', `
            <div class="p-3 bg-body-tertiary rounded-3">
              <div class="mb-2"><div class="text-muted small">Megjegyzés 1</div>${nl(d.comment1 || d.Comment1)}</div>
              <div><div class="text-muted small">Megjegyzés 2</div>${nl(d.comment2 || d.Comment2)}</div>
            </div>
          `)}
        </div>
      `;
    } catch (err) {
      console.error(err);
      contentEl.innerHTML = `<div class="alert alert-danger m-3"><strong>Hiba:</strong> ${escapeHtml(err.message || 'Nem sikerült betölteni')}</div>`;
      window.c92?.showToast?.('error', 'Hiba a telephely betöltésekor');
    }
  });

  function loadingHtml(text) {
    return `<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="mt-3 mb-0">${escapeHtml(text)}</p></div>`;
  }
  function section(title, body) { return `<hr class="my-4"><h5 class="mb-3">${escapeHtml(title)}</h5>${body}`; }
  function kv(label, value) { return `<div class="col-md-6"><div class="text-muted small">${escapeHtml(label)}</div><div>${value || '—'}</div></div>`; }

  function renderStatus(st) {
    const name = st?.name || st?.Name || '—';
    const color = st?.color || st?.Color || '#6c757d';
    return `<span class="badge" style="background:${escapeAttr(color)};color:white">${escapeHtml(name)}</span>`;
  }

  function nl(val) {
    const s = (val ?? '').toString().trim();
    return s ? s.replace(/\n/g, '<br>') : '<span class="text-muted">—</span>';
  }

  function escapeHtml(str) {
    return String(str ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }
  function escapeAttr(str) {
    return escapeHtml(str).replaceAll('`', '&#096;');
  }
});
