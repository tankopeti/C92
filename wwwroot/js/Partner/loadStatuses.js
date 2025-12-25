// /js/Partner/loadStatuses.js – Státuszok betöltése mindkét modalba
document.addEventListener('DOMContentLoaded', async function () {
    console.log('loadStatuses.js BETÖLTÖDÖTT – státuszok betöltése');

    const loadIntoSelect = async (selectId) => {
        const select = document.getElementById(selectId);
        if (!select) {
            console.warn(`Select elem nem található: #${selectId}`);
            return;
        }

        // Alap opció
        select.innerHTML = '<option value="">Válasszon...</option>';

        try {
            const response = await fetch('/api/Partners/statuses');
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const statuses = await response.json();

            statuses.forEach(status => {
                const option = document.createElement('option');
                option.value = status.id;
                option.textContent = status.name;
                select.appendChild(option);
            });

            console.log(`Státuszok betöltve #${selectId}-ba (${statuses.length} db)`);
        } catch (err) {
            console.error('Státusz betöltési hiba:', err);
            select.innerHTML += '<option value="">Hiba a betöltéskor</option>';
            window.c92.showToast('error', 'Nem sikerült betölteni a státuszokat');
        }
    };

    // Create modal – most már ID-vel
    await loadIntoSelect('partnerStatus');

    // Edit modal – ha létezik
    await loadIntoSelect('editPartnerStatus');
});