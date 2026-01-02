// /js/Site/deleteSite.js
document.addEventListener('DOMContentLoaded', () => {
  const modalEl = document.getElementById('deleteSiteModal');
  const confirmBtn = document.getElementById('confirmDeleteSiteBtn');
  if (!modalEl || !confirmBtn) return;

  document.addEventListener('click', (e) => {
    const btn = e.target.closest('.delete-site-btn');
    if (!btn) return;
    e.preventDefault();

    const siteId = btn.dataset.siteId;
    const name = btn.dataset.siteName || 'Telephely';
    if (!siteId) return;

    document.getElementById('deleteSiteId').value = siteId;
    document.getElementById('deleteSiteName').textContent = name;

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
  });

  confirmBtn.addEventListener('click', async () => {
    const siteId = document.getElementById('deleteSiteId').value;
    if (!siteId) return;

    confirmBtn.disabled = true;

    try {
      const res = await fetch(`/api/SitesIndex/${encodeURIComponent(siteId)}`, {
        method: 'DELETE',
        credentials: 'same-origin'
      });

      if (!res.ok) throw new Error(res.status === 404 ? 'Telephely nem található' : `HTTP ${res.status}`);

      // sor törlés
      document.querySelector(`tr[data-site-id="${siteId}"]`)?.remove();

      window.c92?.showToast?.('success', 'Telephely törölve!');
      bootstrap.Modal.getInstance(modalEl)?.hide();
    } catch (err) {
      console.error(err);
      window.c92?.showToast?.('error', err.message || 'Nem sikerült törölni');
    } finally {
      confirmBtn.disabled = false;
    }
  });
});
