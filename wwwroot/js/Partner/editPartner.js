// /js/Partner/editPartner.js – Szerkesztés, csak a létező mezőkkel
document.addEventListener('DOMContentLoaded', function () {
    console.log('editPartner.js BETÖLTÖDÖTT – kész a szerkesztésre');

    document.addEventListener('click', async function (e) {
        const editBtn = e.target.closest('.edit-partner-btn');
        if (!editBtn) return;

        const partnerId = editBtn.dataset.partnerId;
        if (!partnerId) {
            window.c92.showToast('error', 'Hiba: Partner ID hiányzik');
            return;
        }

        console.log(`Szerkesztés – Partner ID: ${partnerId}`);

        const modalEl = document.getElementById('editPartnerModal');
        if (!modalEl) {
            console.error('editPartnerModal nem található');
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

        try {
            const response = await fetch(`/api/Partners/${partnerId}`);
            if (!response.ok) throw new Error('Partner nem található');

            const data = await response.json();

            // CSAK a létező mezőket töltjük ki (a te modalod alapján)
            const setValue = (id, value) => {
                const el = document.getElementById(id);
                if (el) el.value = value || '';
            };

            setValue('editPartnerId', data.partnerId);
            setValue('editPartnerName', data.name);
            setValue('editPartnerCompanyName', data.companyName);
            setValue('editPartnerEmail', data.email);
            setValue('editPartnerPhone', data.phoneNumber);
            setValue('editPartnerAlternatePhone', data.alternatePhone);
            setValue('editPartnerWebsite', data.website);
            setValue('editPartnerAddressLine1', data.addressLine1);
            setValue('editPartnerAddressLine2', data.addressLine2);
            setValue('editPartnerCity', data.city);
            setValue('editPartnerState', data.state);
            setValue('editPartnerPostalCode', data.postalCode);
            setValue('editPartnerCountry', data.country);
            setValue('editPartnerTaxId', data.taxId);
            setValue('editPartnerStatus', data.statusId);
            setValue('editPartnerNotes', data.notes);

            // Listák readonly megjelenítése (ha vannak konténerek)
            const setContent = (id, html) => {
                const el = document.getElementById(id);
                if (el) el.innerHTML = html;
            };

            setContent('sites-edit-content', data.sites?.length > 0
                ? data.sites.map(s => `<div class="alert alert-info mb-2">${s.siteName} – ${s.city}</div>`).join('')
                : '<p class="text-muted">Nincsenek telephelyek.</p>');

            setContent('contacts-edit-content', data.contacts?.length > 0
                ? data.contacts.map(c => `<div class="alert alert-secondary mb-2">${c.firstName} ${c.lastName} – ${c.email || 'nincs email'}</div>`).join('')
                : '<p class="text-muted">Nincsenek kapcsolattartók.</p>');

            setContent('documents-edit-content', data.documents?.length > 0
                ? data.documents.map(d => `<div class="alert alert-light mb-2"><a href="${d.filePath}" target="_blank">${d.fileName}</a></div>`).join('')
                : '<p class="text-muted">Nincsenek dokumentumok.</p>');

        } catch (err) {
            console.error('Edit betöltési hiba:', err);
            window.c92.showToast('error', 'Nem sikerült betölteni a partnert szerkesztésre');
        }
    });

    // Mentés (form submit)
    const editForm = document.getElementById('editPartnerForm');
    if (editForm) {
        editForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const formData = new FormData(this);
            const partnerDto = {
                partnerId: parseInt(formData.get('PartnerId')),
                name: formData.get('Name')?.trim() || null,
                companyName: formData.get('CompanyName')?.trim() || null,
                email: formData.get('Email')?.trim() || null,
                phoneNumber: formData.get('PhoneNumber')?.trim() || null,
                alternatePhone: formData.get('AlternatePhone')?.trim() || null,
                website: formData.get('Website')?.trim() || null,
                taxId: formData.get('TaxId')?.trim() || null,
                addressLine1: formData.get('AddressLine1')?.trim() || null,
                addressLine2: formData.get('AddressLine2')?.trim() || null,
                city: formData.get('City')?.trim() || null,
                state: formData.get('State')?.trim() || null,
                postalCode: formData.get('PostalCode')?.trim() || null,
                country: formData.get('Country')?.trim() || null,
                notes: formData.get('Notes')?.trim() || null,
                statusId: formData.get('StatusId') ? parseInt(formData.get('StatusId')) : null
            };

            if (!partnerDto.name) {
                window.c92.showToast('error', 'A partner neve kötelező!');
                return;
            }

            try {
                const response = await fetch(`/api/Partners/${partnerDto.partnerId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(partnerDto)
                });

                if (!response.ok) {
                    const err = await response.json().catch(() => ({}));
                    window.c92.showToast('error', err.errors?.General?.[0] || err.title || 'Hiba a mentéskor');
                    return;
                }

                window.c92.showToast('success', 'Partner sikeresen frissítve!');
                bootstrap.Modal.getInstance(document.getElementById('editPartnerModal'))?.hide();
                location.reload();
            } catch (err) {
                console.error('Edit mentési hiba:', err);
                window.c92.showToast('error', 'Hálózati hiba');
            }
        });
    }
});