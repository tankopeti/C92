// /js/Partner/deletePartner.js – Törlés + sor azonnali eltüntetése animációval
document.addEventListener('DOMContentLoaded', function () {
    console.log('deletePartner.js BETÖLTÖDÖTT – kész a törlésre');

    // 1. Törlés link kattintás – adatok átadása a modalnak
    document.addEventListener('click', function (e) {
        const deleteLink = e.target.closest('[data-bs-target="#deletePartnerModal"]');
        if (!deleteLink) return;

        const partnerId = deleteLink.dataset.partnerId;
        const partnerName = deleteLink.dataset.partnerName || 'ismeretlen nevű';

        const nameEl = document.getElementById('deletePartnerName');
        const idInput = document.getElementById('deletePartnerId');

        if (nameEl) nameEl.textContent = partnerName;
        if (idInput) idInput.value = partnerId;

        console.log(`Törlés modal megnyitva – Partner ID: ${partnerId}, Név: ${partnerName}`);
    });

    // 2. Törlés megerősítése
    document.addEventListener('click', async function (e) {
        const confirmBtn = e.target.closest('#confirmDeleteBtn');
        if (!confirmBtn) return;

        const partnerId = document.getElementById('deletePartnerId')?.value;
        if (!partnerId) {
            window.c92.showToast('error', 'Hiba: Partner ID hiányzik');
            return;
        }

        console.log(`Törlés megerősítve – Partner ID: ${partnerId}`);

        try {
            const response = await fetch(`/api/Partners/${partnerId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                const err = await response.json().catch(() => ({}));
                throw new Error(err.title || 'Hiba a törléskor');
            }

            window.c92.showToast('success', 'Partner sikeresen törölve!');

            // Modal bezárása
            const modalEl = document.getElementById('deletePartnerModal');
            const modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();

            // SOR ELTÜNTETÉSE SZÉP ANIMÁCIÓVAL
            const row = document.querySelector(`tr[data-partner-id="${partnerId}"]`);
            if (row) {
                row.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
                row.style.opacity = '0';
                row.style.transform = 'translateX(-20px)';
                setTimeout(() => {
                    row.remove();
                    // Ha nincs több sor, frissítjük az oldalt (üres tábla esetén)
                    if (!document.querySelector('tbody tr')) {
                        location.reload();
                    }
                }, 600);
            }

        } catch (err) {
            console.error('Törlési hiba:', err);
            window.c92.showToast('error', err.message || 'Nem sikerült törölni a partnert');
        }
    });
});