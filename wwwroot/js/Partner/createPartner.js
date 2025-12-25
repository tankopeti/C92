// /js/Partner/createPartner.js – Kizárólag új partner létrehozása
document.addEventListener('DOMContentLoaded', function () {
    console.log('createPartner.js BETÖLTÖDÖTT – kész a létrehozásra');

    document.addEventListener('click', async function (e) {
        if (e.target.id === 'saveNewPartnerBtn' || e.target.closest('#saveNewPartnerBtn')) {
            e.preventDefault();

            const modal = document.getElementById('createPartnerModal');
            const form = document.getElementById('createPartnerForm');

            if (!form) {
                console.error('createPartnerForm nem található');
                window.c92.showToast('error', 'Hiba: Űrlap nem található');
                return;
            }

            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const formData = new FormData(form);
            const partnerDto = {
                name: formData.get('Name')?.trim() || null,
                companyName: formData.get('CompanyName')?.trim() || null,
                email: formData.get('Email')?.trim() || null,
                phoneNumber: formData.get('PhoneNumber')?.trim() || null,
                alternatePhone: formData.get('AlternatePhone')?.trim() || null,
                website: formData.get('Website')?.trim() || null,
                taxId: formData.get('TaxId')?.trim() || null,
                intTaxId: formData.get('IntTaxId')?.trim() || null,
                industry: formData.get('Industry')?.trim() || null,
                statusId: formData.get('StatusId') ? parseInt(formData.get('StatusId')) : null,
                addressLine1: formData.get('AddressLine1')?.trim() || null,
                addressLine2: formData.get('AddressLine2')?.trim() || null,
                city: formData.get('City')?.trim() || null,
                state: formData.get('State')?.trim() || null,
                postalCode: formData.get('PostalCode')?.trim() || null,
                country: formData.get('Country')?.trim() || 'Magyarország',
                notes: formData.get('Notes')?.trim() || null,
                sites: [],
                contacts: [],
                documents: []
            };

            if (!partnerDto.name) {
                window.c92.showToast('error', 'A partner neve kötelező!');
                return;
            }

            try {
                const response = await fetch('/api/Partners/CreatePartner', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(partnerDto)
                });

                if (!response.ok) {
                    const err = await response.json().catch(() => ({}));
                    const msg = err.errors?.General?.[0] || err.title || 'Hiba a mentéskor';
                    window.c92.showToast('error', msg);
                    return;
                }

                window.c92.showToast('success', 'Partner sikeresen létrehozva!');
                bootstrap.Modal.getInstance(modal)?.hide();
                form.reset();
                location.reload();
            } catch (err) {
                console.error('Create error:', err);
                window.c92.showToast('error', 'Hálózati hiba');
            }
        }
    });
});