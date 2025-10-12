document.addEventListener('DOMContentLoaded', function () {
    // Create Partner Form Submission
    const savePartnerBtn = document.getElementById('savePartnerBtn');
    if (!savePartnerBtn) {
        console.error('Element with ID "savePartnerBtn" not found.');
        return;
    }

    savePartnerBtn.addEventListener('click', async function () {
        const form = document.getElementById('createPartnerForm');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const formData = new FormData(form);
        const partnerDto = {
            Name: formData.get('Name')?.trim() || null,
            CompanyName: formData.get('CompanyName')?.trim() || null,
            Email: formData.get('Email')?.trim() || null,
            PhoneNumber: formData.get('PhoneNumber')?.trim() || null,
            AlternatePhone: formData.get('AlternatePhone')?.trim() || null,
            Website: formData.get('Website')?.trim() || null,
            TaxId: formData.get('TaxId')?.trim() || null,
            IntTaxId: formData.get('IntTaxId')?.trim() || null,
            Industry: formData.get('Industry')?.trim() || null,
            StatusId: parseInt(formData.get('StatusId')) || null,
            AddressLine1: formData.get('AddressLine1')?.trim() || null,
            AddressLine2: formData.get('AddressLine2')?.trim() || null,
            City: formData.get('City')?.trim() || null,
            State: formData.get('State')?.trim() || null,
            PostalCode: formData.get('PostalCode')?.trim() || null,
            Country: formData.get('Country')?.trim() || null,
            BillingContactName: formData.get('BillingContactName')?.trim() || null,
            BillingEmail: formData.get('BillingEmail')?.trim() || null,
            PaymentTerms: formData.get('PaymentTerms')?.trim() || null,
            CreditLimit: parseFloat(formData.get('CreditLimit')) || null,
            PreferredCurrency: formData.get('PreferredCurrency')?.trim() || null,
            IsTaxExempt: formData.get('IsTaxExempt') === 'true',
            AssignedTo: formData.get('AssignedTo')?.trim() || null,
            PartnerGroupId: parseInt(formData.get('PartnerGroupId')) || null,
            LastContacted: formData.get('LastContacted') ? new Date(formData.get('LastContacted')).toISOString() : null,
            Notes: formData.get('Notes')?.trim() || null,
            Documents: [],
            Sites: [],
            Contacts: []
        };

        if (!partnerDto.Name) {
            window.c92.showToast('error', 'Hiba: A partner neve kötelező!');
            return;
        }

        try {
            const response = await fetch('/api/Partners/CreatePartner', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                },
                body: JSON.stringify(partnerDto)
            });

            if (!response.ok) {
                const error = await response.json();
                const errorMessage = error.message || JSON.stringify(error.errors) || 'Failed to create partner';
                console.error('Error:', errorMessage);
                window.c92.showToast('error', `Hiba: ${errorMessage}`);
                return;
            }

            const result = await response.json();
            window.c92.showToast('success', `Partner sikeresen létrehozva, ID: ${result.partnerId}`);
            form.reset();
            bootstrap.Modal.getInstance(document.getElementById('createPartnerModal')).hide();
            window.location.reload();
        } catch (error) {
            console.error('Error creating partner:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
        }
    });

    // Edit Partner Form Submission
    document.getElementById('editPartnerForm').addEventListener('submit', async function (event) {
        event.preventDefault();
        const formData = new FormData(this);
        const data = {
            PartnerId: parseInt(formData.get('PartnerId')),
            Name: formData.get('Name')?.trim() || null,
            CompanyName: formData.get('CompanyName')?.trim() || null,
            Email: formData.get('Email')?.trim() || null,
            PhoneNumber: formData.get('PhoneNumber')?.trim() || null,
            AlternatePhone: formData.get('AlternatePhone')?.trim() || null,
            Website: formData.get('Website')?.trim() || null,
            TaxId: formData.get('TaxId')?.trim() || null,
            IntTaxId: formData.get('IntTaxId')?.trim() || null,
            Industry: formData.get('Industry')?.trim() || null,
            StatusId: parseInt(formData.get('StatusId')) || null,
            AddressLine1: formData.get('AddressLine1')?.trim() || null,
            AddressLine2: formData.get('AddressLine2')?.trim() || null,
            City: formData.get('City')?.trim() || null,
            State: formData.get('State')?.trim() || null,
            PostalCode: formData.get('PostalCode')?.trim() || null,
            Country: formData.get('Country')?.trim() || null,
            BillingContactName: formData.get('BillingContactName')?.trim() || null,
            BillingEmail: formData.get('BillingEmail')?.trim() || null,
            PaymentTerms: formData.get('PaymentTerms')?.trim() || null,
            CreditLimit: parseFloat(formData.get('CreditLimit')) || null,
            PreferredCurrency: formData.get('PreferredCurrency')?.trim() || null,
            IsTaxExempt: formData.get('IsTaxExempt') === 'true',
            AssignedTo: formData.get('AssignedTo')?.trim() || null,
            PartnerGroupId: parseInt(formData.get('PartnerGroupId')) || null,
            LastContacted: formData.get('LastContacted') ? new Date(formData.get('LastContacted')).toISOString() : null,
            Notes: formData.get('Notes')?.trim() || null,
            Documents: [],
            Sites: [],
            Contacts: []
        };

        if (!data.Name) {
            window.c92.showToast('error', 'Hiba: A partner neve kötelező!');
            return;
        }

        if (data.Website && !/^(https?:\/\/)?([\w-]+\.)+[\w-]{2,}(\/.*)?$/.test(data.Website)) {
            window.c92.showToast('error', 'Hiba: Érvénytelen weboldal URL!');
            return;
        }
        if (data.BillingEmail && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.BillingEmail)) {
            window.c92.showToast('error', 'Hiba: Érvénytelen számlázási email cím!');
            return;
        }
        if (data.AlternatePhone && !/^\+?[\d\s-]{7,}$/.test(data.AlternatePhone)) {
            window.c92.showToast('error', 'Hiba: Érvénytelen másodlagos telefonszám!');
            return;
        }
        if (data.PreferredCurrency && data.PreferredCurrency.length !== 3) {
            window.c92.showToast('error', 'Hiba: Az alap valuta pontosan 3 karakter hosszú kell legyen (pl. USD)!');
            return;
        }

        try {
            const response = await fetch(`/api/Partners/${data.PartnerId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const error = await response.json();
                const errorMessage = error.errors?.General?.[0] || error.title || 'Failed to update partner';
                console.error('Update response error:', errorMessage);
                window.c92.showToast('error', `Hiba: ${errorMessage}`);
                return;
            }

            window.c92.showToast('success', 'Partner sikeresen frissítve!');
            bootstrap.Modal.getInstance(document.getElementById('editPartnerModal')).hide();
            window.location.reload();
        } catch (error) {
            console.error('Error updating partner:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
        }
    });

    // Load Partner for View Modal
    async function loadPartner(partnerId) {
        try {
            const response = await fetch(`/api/Partners/${partnerId}?t=${Date.now()}`, {
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || JSON.stringify(error.errors) || `Partner ${partnerId} not found`);
            }

            const data = await response.json();
            console.log('Sites data:', JSON.stringify(data.sites, null, 2)); // Debug log
            console.log('Documents data:', JSON.stringify(data.documents, null, 2)); // Debug log
            document.getElementById('viewPartnerName').textContent = data.name || '';
            document.getElementById('viewPartnerCompanyName').textContent = data.companyName || '';
            document.getElementById('viewPartnerEmail').textContent = data.email || '';
            document.getElementById('viewPartnerPhone').textContent = data.phoneNumber || '';
            document.getElementById('viewPartnerAlternatePhone').textContent = data.alternatePhone || '';
            document.getElementById('viewPartnerWebsite').textContent = data.website || '';
            document.getElementById('viewPartnerAddressLine1').textContent = data.addressLine1 || '';
            document.getElementById('viewPartnerAddressLine2').textContent = data.addressLine2 || '';
            document.getElementById('viewPartnerCity').textContent = data.city || '';
            document.getElementById('viewPartnerState').textContent = data.state || '';
            document.getElementById('viewPartnerPostalCode').textContent = data.postalCode || '';
            document.getElementById('viewPartnerCountry').textContent = data.country || '';
            document.getElementById('viewPartnerTaxId').textContent = data.taxId || '';
            document.getElementById('viewPartnerIntTaxId').textContent = data.intTaxId || '';
            document.getElementById('viewPartnerIndustry').textContent = data.industry || '';
            document.getElementById('viewPartnerStatus').textContent = data.status?.name || '';
            document.getElementById('viewPartnerBillingContactName').textContent = data.billingContactName || '';
            document.getElementById('viewPartnerBillingEmail').textContent = data.billingEmail || '';
            document.getElementById('viewPartnerPaymentTerms').textContent = data.paymentTerms || '';
            document.getElementById('viewPartnerCreditLimit').textContent = data.creditLimit?.toString() || '';
            document.getElementById('viewPartnerPreferredCurrency').textContent = data.preferredCurrency || '';
            document.getElementById('viewPartnerIsTaxExempt').textContent = data.isTaxExempt ? 'Igen' : 'Nem';
            document.getElementById('viewPartnerAssignedTo').textContent = data.assignedTo || '';
            document.getElementById('viewPartnerPartnerGroupId').textContent = data.partnerGroupId || '';
            document.getElementById('viewPartnerLastContacted').textContent = data.lastContacted?.split('T')[0] || '';
            document.getElementById('viewPartnerNotes').textContent = data.notes || '';

            const sitesContainer = document.getElementById('sites-content');
            sitesContainer.innerHTML = data.sites?.map(site => `
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Telephely neve</label>
                        <p class="form-control-static">${site.siteName || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Cím 1</label>
                        <p class="form-control-static">${site.addressLine1 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Cím 2</label>
                        <p class="form-control-static">${site.addressLine2 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Város</label>
                        <p class="form-control-static">${site.city || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Állam/Megye</label>
                        <p class="form-control-static">${site.state || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Irányítószám</label>
                        <p class="form-control-static">${site.postalCode || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Ország</label>
                        <p class="form-control-static">${site.country || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Elsődleges</label>
                        <p class="form-control-static">${site.isPrimary ? 'Igen' : 'Nem'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Kapcsolattartó 1</label>
                        <p class="form-control-static">${site.contactPerson1 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Kapcsolattartó 2</label>
                        <p class="form-control-static">${site.contactPerson2 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Kapcsolattartó 3</label>
                        <p class="form-control-static">${site.contactPerson3 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Megjegyzés 1</label>
                        <p class="form-control-static">${site.comment1 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Megjegyzés 2</label>
                        <p class="form-control-static">${site.comment2 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Státusz</label>
                        <p class="form-control-static">${site.status?.name || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Státusz ID</label>
                        <p class="form-control-static">${site.statusId || ''}</p>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek telephelyek.</p>';

            const contactsContainer = document.getElementById('contacts-content');
            contactsContainer.innerHTML = data.contacts?.map(contact => `
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Név</label>
                        <p class="form-control-static">${contact.firstName} ${contact.lastName || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">E-mail</label>
                        <p class="form-control-static">${contact.email || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Telefonszám</label>
                        <p class="form-control-static">${contact.phoneNumber || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Másodlagos telefonszám</label>
                        <p class="form-control-static">${contact.phoneNumber2 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Szerepkör</label>
                        <p class="form-control-static">${contact.jobTitle || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Megjegyzés</label>
                        <p class="form-control-static">${contact.comment || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Másodlagos megjegyzés</label>
                        <p class="form-control-static">${contact.comment2 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Elsődleges kapcsolattartó</label>
                        <p class="form-control-static">${contact.isPrimary ? 'Igen' : 'Nem'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Státusz</label>
                        <p class="form-control-static">${contact.status?.name || ''}</p>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek kapcsolattartók.</p>';

            const documentsContainer = document.getElementById('documents-content');
            documentsContainer.innerHTML = data.documents?.map(doc => `
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Dokumentum ID</label>
                        <p class="form-control-static">${doc.documentId || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Fájlnév</label>
                        <p class="form-control-static"><a href="${doc.filePath || '#'}" target="_blank">${doc.fileName || 'N/A'}</a></p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Fájl elérési út</label>
                        <p class="form-control-static">${doc.filePath || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Dokumentum típus ID</label>
                        <p class="form-control-static">${doc.documentTypeId || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Dokumentum típus neve</label>
                        <p class="form-control-static">${doc.documentTypeName || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Feltöltés dátuma</label>
                        <p class="form-control-static">${doc.uploadDate?.split('T')[0] || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Feltöltötte</label>
                        <p class="form-control-static">${doc.uploadedBy || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Telephely ID</label>
                        <p class="form-control-static">${doc.siteId || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Partner ID</label>
                        <p class="form-control-static">${doc.partnerId || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Partner neve</label>
                        <p class="form-control-static">${doc.partnerName || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Státusz</label>
                        <p class="form-control-static">${doc.status !== null ? doc.status : 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Dokumentum linkek</label>
                        <p class="form-control-static">${doc.documentLinks?.length > 0 ? doc.documentLinks.map(l => `ID: ${l.id}, Modul: ${l.moduleId}, Rekord: ${l.recordId}`).join('; ') : 'N/A'}</p>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek dokumentumok.</p>';

            const viewModal = new bootstrap.Modal(document.getElementById('viewPartnerModal'));
            viewModal.show();
        } catch (error) {
            console.error('Error loading partner:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
        }
    }

    // Load Partner for Edit Modal
    let currentPartnerId = null;
    async function loadEditPartner(partnerId) {
        try {
            const modalElement = document.getElementById('editPartnerModal');
            const editModal = bootstrap.Modal.getOrCreateInstance(modalElement);

            const response = await fetch(`/api/Partners/${partnerId}?t=${Date.now()}`, {
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || JSON.stringify(error.errors) || `Partner ${partnerId} not found`);
            }

            const data = await response.json();
            currentPartnerId = data.partnerId;
            document.getElementById('editPartnerId').value = data.partnerId || '';
            document.getElementById('editPartnerName').value = data.name || '';
            document.getElementById('editPartnerCompanyName').value = data.companyName || '';
            document.getElementById('editPartnerEmail').value = data.email || '';
            document.getElementById('editPartnerPhone').value = data.phoneNumber || '';
            document.getElementById('editPartnerAlternatePhone').value = data.alternatePhone || '';
            document.getElementById('editPartnerWebsite').value = data.website || '';
            document.getElementById('editPartnerAddressLine1').value = data.addressLine1 || '';
            document.getElementById('editPartnerAddressLine2').value = data.addressLine2 || '';
            document.getElementById('editPartnerCity').value = data.city || '';
            document.getElementById('editPartnerState').value = data.state || '';
            document.getElementById('editPartnerPostalCode').value = data.postalCode || '';
            document.getElementById('editPartnerCountry').value = data.country || '';
            document.getElementById('editPartnerTaxId').value = data.taxId || '';
            document.getElementById('editPartnerIntTaxId').value = data.intTaxId || '';
            document.getElementById('editPartnerIndustry').value = data.industry || '';
            document.getElementById('editPartnerStatus').value = data.statusId || '';
            document.getElementById('editPartnerBillingContactName').value = data.billingContactName || '';
            document.getElementById('editPartnerBillingEmail').value = data.billingEmail || '';
            document.getElementById('editPartnerPaymentTerms').value = data.paymentTerms || '';
            document.getElementById('editPartnerCreditLimit').value = data.creditLimit || '';
            document.getElementById('editPartnerPreferredCurrency').value = data.preferredCurrency || '';
            document.getElementById('editPartnerIsTaxExempt').value = data.isTaxExempt ? 'true' : 'false';
            document.getElementById('editPartnerAssignedTo').value = data.assignedTo || '';
            document.getElementById('editPartnerPartnerGroupId').value = data.partnerGroupId || '';
            document.getElementById('editPartnerLastContacted').value = data.lastContacted?.split('T')[0] || '';
            document.getElementById('editPartnerNotes').value = data.notes || '';

            const sitesContainer = document.getElementById('sites-edit-content');
            sitesContainer.innerHTML = data.sites?.map(site => `
                <div class="row" data-site-id="${site.siteId}">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Telephely neve</label>
                        <input type="text" class="form-control" value="${site.siteName || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Cím 1</label>
                        <input type="text" class="form-control" value="${site.addressLine1 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Cím 2</label>
                        <input type="text" class="form-control" value="${site.addressLine2 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Város</label>
                        <input type="text" class="form-control" value="${site.city || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Állam/Megye</label>
                        <input type="text" class="form-control" value="${site.state || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Irányítószám</label>
                        <input type="text" class="form-control" value="${site.postalCode || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Ország</label>
                        <input type="text" class="form-control" value="${site.country || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Elsődleges</label>
                        <input type="checkbox" class="form-check-input" ${site.isPrimary ? 'checked' : ''} disabled>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Kapcsolattartó 1</label>
                        <input type="text" class="form-control" value="${site.contactPerson1 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Kapcsolattartó 2</label>
                        <input type="text" class="form-control" value="${site.contactPerson2 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Kapcsolattartó 3</label>
                        <input type="text" class="form-control" value="${site.contactPerson3 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Megjegyzés 1</label>
                        <input type="text" class="form-control" value="${site.comment1 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Megjegyzés 2</label>
                        <input type="text" class="form-control" value="${site.comment2 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Státusz</label>
                        <input type="text" class="form-control" value="${site.status?.name || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Státusz ID</label>
                        <input type="text" class="form-control" value="${site.statusId || ''}" readonly>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek telephelyek.</p>';

            const contactsContainer = document.getElementById('contacts-edit-content');
            contactsContainer.innerHTML = data.contacts?.map(contact => `
                <div class="row" data-contact-id="${contact.contactId}">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Név</label>
                        <input type="text" class="form-control" value="${contact.firstName} ${contact.lastName || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">E-mail</label>
                        <input type="email" class="form-control" value="${contact.email || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Telefonszám</label>
                        <input type="tel" class="form-control" value="${contact.phoneNumber || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Másodlagos telefonszám</label>
                        <input type="tel" class="form-control" value="${contact.phoneNumber2 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Szerepkör</label>
                        <input type="text" class="form-control" value="${contact.jobTitle || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Megjegyzés</label>
                        <input type="text" class="form-control" value="${contact.comment || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Másodlagos megjegyzés</label>
                        <input type="text" class="form-control" value="${contact.comment2 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Elsődleges kapcsolattartó</label>
                        <input type="checkbox" class="form-check-input" ${contact.isPrimary ? 'checked' : ''} disabled>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Státusz</label>
                        <input type="text" class="form-control" value="${contact.status?.name || ''}" readonly>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek kapcsolattartók.</p>';

            const documentsContainer = document.getElementById('documents-edit-content');
            documentsContainer.innerHTML = data.documents?.map(doc => `
                <div class="row" data-document-id="${doc.documentId}">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Dokumentum ID</label>
                        <input type="text" class="form-control" value="${doc.documentId || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Fájlnév</label>
                        <input type="text" class="form-control" value="${doc.fileName || 'N/A'}" readonly>
                        <a href="${doc.filePath || '#'}" target="_blank" class="btn btn-link">Letöltés</a>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Fájl elérési út</label>
                        <input type="text" class="form-control" value="${doc.filePath || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Dokumentum típus ID</label>
                        <input type="text" class="form-control" value="${doc.documentTypeId || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Dokumentum típus neve</label>
                        <input type="text" class="form-control" value="${doc.documentTypeName || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Feltöltés dátuma</label>
                        <input type="text" class="form-control" value="${doc.uploadDate?.split('T')[0] || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Feltöltötte</label>
                        <input type="text" class="form-control" value="${doc.uploadedBy || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Telephely ID</label>
                        <input type="text" class="form-control" value="${doc.siteId || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Partner ID</label>
                        <input type="text" class="form-control" value="${doc.partnerId || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Partner neve</label>
                        <input type="text" class="form-control" value="${doc.partnerName || 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Státusz</label>
                        <input type="text" class="form-control" value="${doc.status !== null ? doc.status : 'N/A'}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Dokumentum linkek</label>
                        <input type="text" class="form-control" value="${doc.documentLinks?.length > 0 ? doc.documentLinks.map(l => `ID: ${l.id}, Modul: ${l.moduleId}, Rekord: ${l.recordId}`).join('; ') : 'N/A'}" readonly>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek dokumentumok.</p>';

            editModal.show();

            // Initialize tabs
            const tabTriggers = document.querySelectorAll('.nav-link');
            tabTriggers.forEach(tab => {
                // Remove existing listeners to prevent duplicates
                tab.removeEventListener('click', tab.showHandler);
                tab.showHandler = () => new bootstrap.Tab(tab).show();
                tab.addEventListener('click', tab.showHandler);
            });
            const activeTab = document.querySelector('.nav-link.active');
            if (activeTab) {
                new bootstrap.Tab(activeTab).show();
            }

            // Ensure backdrop is removed when modal is hidden
            modalElement.addEventListener('hidden.bs.modal', function handleHidden() {
                editModal.dispose();
                document.querySelectorAll('.modal-backdrop').forEach(backdrop => backdrop.remove());
                document.body.classList.remove('modal-open');
                document.body.style.removeProperty('overflow');
                document.body.style.removeProperty('padding-right');
                modalElement.removeEventListener('hidden.bs.modal', handleHidden); // Clean up listener
            }, { once: true });

        } catch (error) {
            console.error('Error loading edit partner:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
        }
    }

    document.addEventListener('click', async function (event) {
        const deleteLink = event.target.closest('[data-bs-target="#deletePartnerModal"]');
        if (deleteLink) {
            const partnerId = deleteLink.getAttribute('data-partner-id');
            const partnerName = deleteLink.getAttribute('data-partner-name') || '';
            document.getElementById('deletePartnerId').value = partnerId;
            document.getElementById('deletePartnerName').textContent = partnerName;
        }

        if (event.target.id === 'confirmDeletePartnerBtn') {
            const partnerId = document.getElementById('deletePartnerId').value;
            await deletePartner(partnerId);
        }

        const viewBtn = event.target.closest('.view-partner-btn');
        if (viewBtn) {
            await loadPartner(viewBtn.getAttribute('data-partner-id'));
        }

        const editBtn = event.target.closest('.edit-partner-btn');
        if (editBtn) {
            await loadEditPartner(editBtn.getAttribute('data-partner-id'));
        }
    });

    async function deletePartner(partnerId) {
        if (!partnerId || isNaN(parseInt(partnerId))) {
            window.c92.showToast('error', 'Hiba: Érvénytelen partner azonosító!');
            return false;
        }

        try {
            const partnerResponse = await fetch(`/api/Partners/${partnerId}`, {
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                }
            });

            const response = await fetch(`/api/Partners/${partnerId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                }
            });

            if (!response.ok) {
                let errorMessage = 'Failed to delete partner';
                try {
                    const error = await response.json();
                    errorMessage = error.message || error.errors?.General?.[0] || `HTTP ${response.status}: ${response.statusText}`;
                } catch (e) {
                    errorMessage = `HTTP ${response.status}: ${response.statusText}`;
                }
                throw new Error(errorMessage);
            }

            window.c92.showToast('success', 'Partner sikeresen törölve!');
            bootstrap.Modal.getInstance(document.getElementById('deletePartnerModal'))?.hide();
            document.querySelector(`tr[data-partner-id="${partnerId}"]`)?.remove();
            return true;
        } catch (error) {
            console.error(`Error deleting partner ${partnerId}:`, error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
            return false;
        }
    }
});