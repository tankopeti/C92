// /wwwroot/js/Partner/loadStatuses.js
document.addEventListener('DOMContentLoaded', () => {
    console.log('loadStatuses.js BETÖLTÖDÖTT – státuszok betöltése');

    // Ezeket a selecteket töltjük fel
    const selectIds = [
        'partnerStatus',
        'editPartnerStatus',
        'filterStatus' // ✅ advanced filter modal
    ];

    const selects = selectIds
        .map(id => document.getElementById(id))
        .filter(Boolean);

    if (selects.length === 0) {
        console.warn('Nincs státusz select a lapon (partnerStatus/editPartnerStatus/filterStatus).');
        return;
    }

    fetch('/api/Partners/statuses', {
        headers: { 'Accept': 'application/json' },
        credentials: 'same-origin'
    })
        .then(r => {
            if (!r.ok) throw new Error(`HTTP ${r.status}`);
            return r.json();
        })
        .then(statuses => {
            selects.forEach(sel => {
                // Meghagyjuk a legelső üres optiont (placeholder)
                const firstEmpty = sel.querySelector('option[value=""]');
                const placeholderHtml = firstEmpty ? firstEmpty.outerHTML : '<option value="">-- Mindegy --</option>';

                sel.innerHTML = placeholderHtml;

                statuses.forEach(s => {
                    const opt = document.createElement('option');
                    opt.value = s.id;
                    opt.textContent = s.name;
                    sel.appendChild(opt);
                });

                // ✅ Ha TomSelect van rajta, frissítjük, hogy lássa az új optionöket
                if (sel.tomselect) {
                    sel.tomselect.sync();
                    sel.tomselect.refreshOptions(false);
                }

                console.log(`Státuszok betöltve #${sel.id}-ba (${statuses.length} db)`);
            });
        })
        .catch(err => {
            console.error('Hiba a státuszok betöltésekor:', err);
        });
});
