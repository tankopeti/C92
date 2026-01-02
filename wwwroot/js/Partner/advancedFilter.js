// /js/Partner/advancedFilter.js
document.addEventListener('DOMContentLoaded', function () {
    const modal = document.getElementById('advancedFilterModal');
    const form = document.getElementById('advancedFilterForm');
    const applyBtn = document.getElementById('applyFilterBtn');
    const clearBtn = document.getElementById('clearFilterBtn');
    const statusSelect = document.getElementById('filterStatus');
    // const typeSelect = document.getElementById('filterType'); // ha van típus

    // Státuszok betöltése (újrahasznosítjuk a loadStatuses.js-t vagy külön endpoint)
    fetch('/api/Partners/statuses')
        .then(r => r.json())
        .then(statuses => {
            statuses.forEach(s => {
                const opt = document.createElement('option');
                opt.value = s.id;
                opt.textContent = s.name;
                statusSelect.appendChild(opt);
            });
        });

    // Szűrők alkalmazása
    applyBtn.addEventListener('click', function () {
        const filters = {
            name: document.getElementById('filterName').value.trim(),
            taxId: document.getElementById('filterTaxId').value.trim(),
            statusId: statusSelect.value,
            // typeId: typeSelect.value,
            city: document.getElementById('filterCity').value.trim(),
            postalCode: document.getElementById('filterPostalCode').value.trim(),
            emailDomain: document.getElementById('filterEmailDomain').value.trim(),
            activeOnly: document.getElementById('filterActiveOnly').checked
        };

        // Tábla újratöltése szűrőkkel (feltételezzük, hogy van egy loadPartners() függvényed)
        if (typeof loadPartners === 'function') {
            loadPartners(filters);
        }

        bootstrap.Modal.getInstance(modal).hide();
    });

    // Szűrők törlése
    clearBtn.addEventListener('click', function () {
        form.reset();
        statusSelect.value = '';
        // typeSelect.value = '';

        if (typeof loadPartners === 'function') {
            loadPartners({}); // üres szűrő = minden
        }
    });
});