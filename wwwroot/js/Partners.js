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

        // Additional validations
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
                body: JSON.stringify(data) // Send data directly, not wrapped in { partner: data }
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
            const response = await fetch(`/api/Partners/${partnerId}`, {
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
                        <label class="form-label fw-bold">Név</label>
                        <p class="form-control-static">${site.siteName || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Cím</label>
                        <p class="form-control-static">${site.addressLine1 || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Város</label>
                        <p class="form-control-static">${site.city || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Ország</label>
                        <p class="form-control-static">${site.country || ''}</p>
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
                        <label class="form-label fw-bold">Szerepkör</label>
                        <p class="form-control-static">${contact.jobTitle || ''}</p>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek kapcsolattartók.</p>';

            const documentsContainer = document.getElementById('documents-content');
            documentsContainer.innerHTML = data.documents?.map(doc => `
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Név</label>
                        <p class="form-control-static">${doc.fileName || 'N/A'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Típus</label>
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
            const response = await fetch(`/api/Partners/${partnerId}`, {
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
                        <label class="form-label fw-bold">Név</label>
                        <input type="text" class="form-control site-name" value="${site.siteName || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Cím</label>
                        <input type="text" class="form-control site-address" value="${site.addressLine1 || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Város</label>
                        <input type="text" class="form-control site-city" value="${site.city || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Ország</label>
                        <input type="text" class="form-control site-country" value="${site.country || ''}" readonly>
                    </div>
                    <div class="col-12 text-end">
                        <button type="button" class="btn btn-warning edit-site-btn">Szerkesztés</button>
                        <button type="button" class="btn btn-danger remove-site-btn">Eltávolítás</button>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek telephelyek.</p>';

            const contactsContainer = document.getElementById('contacts-edit-content');
            contactsContainer.innerHTML = data.contacts?.map(contact => `
                <div class="row" data-contact-id="${contact.contactId}">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Név</label>
                        <input type="text" class="form-control contact-name" value="${contact.firstName} ${contact.lastName || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">E-mail</label>
                        <input type="email" class="form-control contact-email" value="${contact.email || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Telefonszám</label>
                        <input type="tel" class="form-control contact-phone" value="${contact.phoneNumber || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Szerepkör</label>
                        <input type="text" class="form-control contact-job-title" value="${contact.jobTitle || ''}" readonly>
                    </div>
                    <div class="col-12 text-end">
                        <button type="button" class="btn btn-warning edit-contact-btn">Szerkesztés</button>
                        <button type="button" class="btn btn-danger remove-contact-btn">Eltávolítás</button>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek kapcsolattartók.</p>';

            const documentsContainer = document.getElementById('documents-edit-content');
            documentsContainer.innerHTML = data.documents?.map(doc => `
                <div class="row" data-document-id="${doc.documentId}">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Név</label>
                        <input type="text" class="form-control document-name" value="${doc.fileName || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Típus</label>
                        <input type="text" class="form-control document-type" value="${doc.documentTypeName || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Feltöltés dátuma</label>
                        <input type="text" class="form-control document-upload-date" value="${doc.uploadDate?.split('T')[0] || ''}" readonly>
                    </div>
                    <div class="col-12 text-end">
                        <button type="button" class="btn btn-warning edit-document-btn">Szerkesztés</button>
                        <button type="button" class="btn btn-danger remove-document-btn">Eltávolítás</button>
                    </div>
                </div>
                <hr class="my-4">
            `).join('') || '<p>Nincsenek dokumentumok.</p>';

            const editModal = new bootstrap.Modal(document.getElementById('editPartnerModal'));
            editModal.show();

            // Initialize tabs
            setTimeout(() => {
                const tabTriggers = document.querySelectorAll('.nav-link');
                tabTriggers.forEach(tab => {
                    tab.addEventListener('click', () => new bootstrap.Tab(tab).show());
                });
                new bootstrap.Tab(document.querySelector('.nav-link.active')).show();
            }, 100);
        } catch (error) {
            console.error('Error loading edit partner:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
        }
    }

    // Add Site
    function addSite() {
        const sitesContainer = document.getElementById('sites-edit-content');
        const siteId = `new-${Date.now()}`;
        sitesContainer.innerHTML += `
            <div class="row" data-site-id="${siteId}">
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Név</label>
                    <input type="text" class="form-control site-name" required>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Cím</label>
                    <input type="text" class="form-control site-address" required>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Kiegészítő cím</label>
                    <input type="text" class="form-control site-address-line-2">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Város</label>
                    <input type="text" class="form-control site-city">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Megye</label>
                    <input type="text" class="form-control site-state">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Irányítószám</label>
                    <input type="text" class="form-control site-postal-code">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Ország</label>
                    <input type="text" class="form-control site-country" required>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Elsődleges</label>
                    <input type="checkbox" class="form-check-input site-is-primary">
                </div>
                <div class="col-12 text-end">
                    <button type="button" class="btn btn-success save-site-btn">Mentés</button>
                    <button type="button" class="btn btn-secondary cancel-site-btn">Mégse</button>
                </div>
                <hr class="my-4">
            </div>
        `;
    }

    // Add Contact
    function addContact() {
        const contactsContainer = document.getElementById('contacts-edit-content');
        const contactId = `new-${Date.now()}`;
        contactsContainer.innerHTML += `
            <div class="row" data-contact-id="${contactId}">
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Keresztnév</label>
                    <input type="text" class="form-control contact-first-name" required>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Vezetéknév</label>
                    <input type="text" class="form-control contact-last-name" required>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">E-mail</label>
                    <input type="email" class="form-control contact-email">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Telefonszám</label>
                    <input type="tel" class="form-control contact-phone">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Szerepkör</label>
                    <input type="text" class="form-control contact-job-title">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Megjegyzés</label>
                    <input type="text" class="form-control contact-comment">
                </div>
                <div class="col-12 text-end">
                    <button type="button" class="btn btn-success save-contact-btn">Mentés</button>
                    <button type="button" class="btn btn-secondary cancel-contact-btn">Mégse</button>
                </div>
                <hr class="my-4">
            </div>
        `;
    }

    // Add Document
    function addDocument() {
        const documentsContainer = document.getElementById('documents-edit-content');
        const documentId = `new-${Date.now()}`;
        documentsContainer.innerHTML += `
            <div class="row" data-document-id="${documentId}">
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Név</label>
                    <input type="text" class="form-control document-name" required>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Típus</label>
                    <select class="form-control document-type" required>
                        <!-- Populate with document types from server -->
                        <option value="">Válasszon...</option>
                        <!-- Add options dynamically via fetch to /api/DocumentTypes -->
                    </select>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label fw-bold">Fájl</label>
                    <input type="file" class="form-control document-file" required>
                </div>
                <div class="col-12 text-end">
                    <button type="button" class="btn btn-success save-document-btn">Mentés</button>
                    <button type="button" class="btn btn-secondary cancel-document-btn">Mégse</button>
                </div>
                <hr class="my-4">
            </div>
        `;
    }

    // Event Listeners
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

        if (event.target.id === 'addSiteBtn') {
            addSite();
        }

        if (event.target.id === 'addContactBtn') {
            addContact();
        }

        if (event.target.id === 'addDocumentBtn') {
            addDocument();
        }

        if (event.target.classList.contains('remove-site-btn')) {
            if (confirm('Biztosan eltávolítja ezt a telephelyet?')) {
                const siteId = event.target.closest('.row').getAttribute('data-site-id');
                await deleteSite(currentPartnerId, siteId);
                await loadEditPartner(currentPartnerId);
            }
        }

        if (event.target.classList.contains('remove-contact-btn')) {
            if (confirm('Biztosan eltávolítja ezt a kapcsolattartót?')) {
                const contactId = event.target.closest('.row').getAttribute('data-contact-id');
                await deleteContact(currentPartnerId, contactId);
                await loadEditPartner(currentPartnerId);
            }
        }

        if (event.target.classList.contains('remove-document-btn')) {
            if (confirm('Biztosan eltávolítja ezt a dokumentumot?')) {
                const documentId = event.target.closest('.row').getAttribute('data-document-id');
                await deleteDocument(documentId);
                await loadEditPartner(currentPartnerId);
            }
        }

        if (event.target.classList.contains('edit-site-btn')) {
            const row = event.target.closest('.row');
            row.querySelectorAll('input').forEach(input => input.removeAttribute('readonly'));
            event.target.classList.replace('edit-site-btn', 'save-site-btn');
            event.target.textContent = 'Mentés';
        }

        if (event.target.classList.contains('edit-contact-btn')) {
            const row = event.target.closest('.row');
            row.querySelectorAll('input').forEach(input => input.removeAttribute('readonly'));
            event.target.classList.replace('edit-contact-btn', 'save-contact-btn');
            event.target.textContent = 'Mentés';
        }

        if (event.target.classList.contains('edit-document-btn')) {
            const row = event.target.closest('.row');
            row.querySelectorAll('input, select').forEach(input => input.removeAttribute('readonly'));
            event.target.classList.replace('edit-document-btn', 'save-document-btn');
            event.target.textContent = 'Mentés';
        }

        if (event.target.classList.contains('save-site-btn')) {
            const row = event.target.closest('.row');
            const siteId = row.getAttribute('data-site-id');
            const siteData = {
                SiteId: siteId.startsWith('new-') ? 0 : parseInt(siteId),
                SiteName: row.querySelector('.site-name')?.value.trim() || '',
                AddressLine1: row.querySelector('.site-address')?.value.trim() || '',
                AddressLine2: row.querySelector('.site-address-line-2')?.value.trim() || null,
                City: row.querySelector('.site-city')?.value.trim() || null,
                State: row.querySelector('.site-state')?.value.trim() || null,
                PostalCode: row.querySelector('.site-postal-code')?.value.trim() || null,
                Country: row.querySelector('.site-country')?.value.trim() || '',
                IsPrimary: row.querySelector('.site-is-primary')?.checked || false
            };

            if (!siteData.SiteName || !siteData.AddressLine1 || !siteData.Country) {
                window.c92.showToast('error', 'Hiba: Név, Cím és Ország mezők kitöltése kötelező!');
                return;
            }

            await saveSite(currentPartnerId, siteData);
            await loadEditPartner(currentPartnerId);
        }

        if (event.target.classList.contains('save-contact-btn')) {
            const row = event.target.closest('.row');
            const contactId = row.getAttribute('data-contact-id');
            const contactData = {
                ContactId: contactId.startsWith('new-') ? 0 : parseInt(contactId),
                FirstName: row.querySelector('.contact-first-name')?.value.trim() || '',
                LastName: row.querySelector('.contact-last-name')?.value.trim() || '',
                Email: row.querySelector('.contact-email')?.value.trim() || null,
                PhoneNumber: row.querySelector('.contact-phone')?.value.trim() || null,
                JobTitle: row.querySelector('.contact-job-title')?.value.trim() || null,
                Comment: row.querySelector('.contact-comment')?.value.trim() || null,
                IsPrimary: false
            };

            if (!contactData.FirstName || !contactData.LastName) {
                window.c92.showToast('error', 'Hiba: Keresztnév és Vezetéknév mezők kitöltése kötelező!');
                return;
            }

            await saveContact(currentPartnerId, contactData);
            await loadEditPartner(currentPartnerId);
        }

        if (event.target.classList.contains('save-document-btn')) {
            const row = event.target.closest('.row');
            const documentId = row.getAttribute('data-document-id');
            const fileInput = row.querySelector('.document-file');
            const formData = new FormData();
            formData.append('PartnerId', currentPartnerId);
            formData.append('DocumentTypeId', row.querySelector('.document-type')?.value || '');
            formData.append('File', fileInput?.files[0]);

            await saveDocument(currentPartnerId, formData, documentId);
            await loadEditPartner(currentPartnerId);
        }

        if (event.target.classList.contains('cancel-site-btn') || event.target.classList.contains('cancel-contact-btn') || event.target.classList.contains('cancel-document-btn')) {
            await loadEditPartner(currentPartnerId);
        }
    });

async function deletePartner(partnerId) {
    if (!partnerId || isNaN(parseInt(partnerId))) {
        window.c92.showToast('error', 'Hiba: Érvénytelen partner azonosító!');
        return false;
    }

    try {
        // Fetch partner data to check for dependencies
        const partnerResponse = await fetch(`/api/Partners/${partnerId}`, {
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
            }
        });
        if (partnerResponse.ok) {
            const partner = await partnerResponse.json();
            // Delete associated sites
            for (const site of partner.sites || []) {
                await deleteSite(partnerId, site.siteId);
            }
            // Delete associated contacts
            for (const contact of partner.contacts || []) {
                await deleteContact(partnerId, contact.contactId);
            }
            // Delete associated documents
            for (const doc of partner.documents || []) {
                await deleteDocument(doc.documentId);
            }
        }

        // Delete the partner
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
        document.querySelector(`tr[data-partner-id="${partnerId}"]`)?.remove(); // Update UI
        return true;
    } catch (error) {
        console.error(`Error deleting partner ${partnerId}:`, error);
        window.c92.showToast('error', `Hiba: ${error.message}`);
        return false;
    }
}

    // Save Site
    async function saveSite(partnerId, siteData) {
        try {
            const response = await fetch(`/api/Partners/${partnerId}/Sites`, {
                method: siteData.SiteId ? 'PUT' : 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
                },
                body: JSON.stringify(siteData)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || JSON.stringify(error.errors) || 'Failed to save site');
            }

            const result = await response.json();
            window.c92.showToast('success', 'Telephely sikeresen mentve!');
            return result;
        } catch (error) {
            console.error('Error saving site:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
            return null;
        }
    }

    // Save Contact
    async function saveContact(partnerId, contactData) {
        try {
            const response = await fetch(`/api/Partners/${partnerId}/Contacts`, {
                method: contactData.ContactId ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(contactData)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || JSON.stringify(error.errors) || 'Failed to save contact');
            }

            const result = await response.json();
            window.c92.showToast('success', 'Kapcsolattartó sikeresen mentve!');
            return result;
        } catch (error) {
            console.error('Error saving contact:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
            return null;
        }
    }

    // Save Document
    async function saveDocument(partnerId, formData, documentId) {
        try {
            const response = await fetch(`/api/Partners/${partnerId}/Documents${documentId && !documentId.startsWith('new-') ? `/${documentId}` : ''}`, {
                method: documentId && !documentId.startsWith('new-') ? 'PUT' : 'POST',
                body: formData
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || JSON.stringify(error.errors) || 'Failed to save document');
            }

            const result = await response.json();
            window.c92.showToast('success', 'Dokumentum sikeresen mentve!');
            return result;
        } catch (error) {
            console.error('Error saving document:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
            return null;
        }
    }

    // Delete Site
    async function deleteSite(partnerId, siteId) {
        if (siteId.startsWith('new-')) return true; // Skip for unsaved sites
        try {
            const response = await fetch(`/api/Partners/${partnerId}/Sites/${siteId}`, {
                method: 'DELETE',
headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
            }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || JSON.stringify(error.errors) || 'Failed to delete site');
            }

            window.c92.showToast('success', 'Telephely sikeresen törölve!');
            return true;
        } catch (error) {
            console.error('Error deleting site:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
            return false;
        }
    }

    // Delete Contact
    async function deleteContact(partnerId, contactId) {
        if (contactId.startsWith('new-')) return true; // Skip for unsaved contacts
        try {
            const response = await fetch(`/api/Partners/${partnerId}/Contacts/${contactId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || JSON.stringify(error.errors) || 'Failed to delete contact');
            }

            window.c92.showToast('success', 'Kapcsolattartó sikeresen törölve!');
            return true;
        } catch (error) {
            console.error('Error deleting contact:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
            return false;
        }
    }

    // Delete Document
    async function deleteDocument(documentId) {
        if (documentId.startsWith('new-')) return true; // Skip for unsaved documents
        try {
            const response = await fetch(`/api/Partners/Documents/${documentId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || JSON.stringify(error.errors) || 'Failed to delete document');
            }

            window.c92.showToast('success', 'Dokumentum sikeresen törölve!');
            return true;
        } catch (error) {
            console.error('Error deleting document:', error);
            window.c92.showToast('error', `Hiba: ${error.message}`);
            return false;
        }
    }
});