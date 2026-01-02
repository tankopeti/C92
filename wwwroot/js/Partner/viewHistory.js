// /js/Partner/viewHistory.js
document.addEventListener('DOMContentLoaded', function () {
    const historyModal = document.getElementById('partnerHistoryModal');
    const modalTitle = document.getElementById('history-partner-name');
    const loadingDiv = document.getElementById('history-loading');
    const contentDiv = document.getElementById('history-content');
    const emptyDiv = document.getElementById('history-empty');
    const listDiv = document.getElementById('history-list');

    // Modal megnyitásakor
    historyModal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget; // a gomb, ami megnyitotta
        const partnerId = button.getAttribute('data-partner-id');
        const partnerName = button.getAttribute('data-partner-name');

        modalTitle.textContent = partnerName;
        loadingDiv.classList.remove('d-none');
        contentDiv.classList.add('d-none');
        listDiv.innerHTML = '';

        // API hívás a történetért
        fetch(`/api/Partners/${partnerId}/history`)
            .then(response => {
                if (!response.ok) throw new Error('Hiba a történet betöltésekor');
                return response.json();
            })
            .then(data => {
                loadingDiv.classList.add('d-none');
                contentDiv.classList.remove('d-none');

                if (data.length === 0) {
                    emptyDiv.classList.remove('d-none');
                    return;
                }

                emptyDiv.classList.add('d-none');

                // Időrendi sorrend: legújabb felül
                data.sort((a, b) => new Date(b.changedAt) - new Date(a.changedAt));

                data.forEach(entry => {
                    const item = document.createElement('div');
                    item.className = 'border-start border-primary border-3 ps-3 py-3';

                    const actionBadge = entry.action === 'Created' ? 'success' :
                                        entry.action === 'Updated' ? 'warning' :
                                        entry.action === 'Deleted' ? 'danger' : 'secondary';

                    item.innerHTML = `
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <span class="badge bg-${actionBadge}">${getActionText(entry.action)}</span>
                                <strong>${entry.changedByName || 'Ismeretlen'}</strong>
                                <span class="text-muted small">– ${formatDate(entry.changedAt)}</span>
                            </div>
                        </div>
                        <div class="mt-2 text-muted">
                            ${entry.changes.replace(/; /g, '<br>')}
                        </div>
                    `;

                    listDiv.appendChild(item);
                    // Sorok közötti elválasztó
                    if (listDiv.children.length > 1) {
                        const divider = document.createElement('hr');
                        divider.className = 'my-3';
                        listDiv.insertBefore(divider, item);
                    }
                });
            })
            .catch(err => {
                loadingDiv.classList.add('d-none');
                contentDiv.classList.remove('d-none');
                listDiv.innerHTML = `<div class="alert alert-danger">Hiba: ${err.message}</div>`;
            });
    });

    // Segédfüggvények
    function getActionText(action) {
        return action === 'Created' ? 'Létrehozva' :
               action === 'Updated' ? 'Módosítva' :
               action === 'Deleted' ? 'Törölve' : action;
    }

    function formatDate(isoString) {
        return new Date(isoString).toLocaleString('hu-HU', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }
});