document.addEventListener('DOMContentLoaded', function () {
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
            Name: formData.get('Name'),
            CompanyName: formData.get('CompanyName') || null,
            Email: formData.get('Email') || null,
            PhoneNumber: formData.get('PhoneNumber') || null,
            AlternatePhone: formData.get('AlternatePhone') || null,
            Website: formData.get('Website') || null,
            TaxId: formData.get('TaxId') || null,
            IntTaxId: formData.get('IntTaxId') || null,
            Industry: formData.get('Industry') || null,
            AddressLine1: formData.get('AddressLine1') || null,
            AddressLine2: formData.get('AddressLine2') || null,
            City: formData.get('City') || null,
            State: formData.get('State') || null,
            PostalCode: formData.get('PostalCode') || null,
            Country: formData.get('Country') || null,
            StatusId: formData.get('StatusId') ? parseInt(formData.get('StatusId')) : null,
            LastContacted: formData.get('LastContacted') || null,
            Notes: formData.get('Notes') || null,
            AssignedTo: formData.get('AssignedTo') || null,
            BillingContactName: formData.get('BillingContactName') || null,
            BillingEmail: formData.get('BillingEmail') || null,
            PaymentTerms: formData.get('PaymentTerms') || null,
            CreditLimit: formData.get('CreditLimit') ? parseFloat(formData.get('CreditLimit')) : null,
            PreferredCurrency: formData.get('PreferredCurrency') || null,
            IsTaxExempt: formData.get('IsTaxExempt') === 'true'
        };

        try {
            console.log('Sending partnerDto:', partnerDto);
            const response = await fetch('/api/Partners/CreatePartner', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(partnerDto)
            });

            if (!response.ok) {
                const contentType = response.headers.get('content-type');
                let errorMessage = 'Failed to create partner';
                if (contentType && contentType.includes('application/json')) {
                    const error = await response.json();
                    errorMessage = error.message || errorMessage;
                } else {
                    errorMessage = await response.text() || errorMessage;
                }
                console.error('Error:', errorMessage);
                window.c92.showToast('error', 'Failed to create partner: ' + errorMessage);
                return;
            }

            const result = await response.json();
            window.c92.showToast('success', `Partner created successfully with ID: ${result.partnerId}`);
            form.reset();
            bootstrap.Modal.getInstance(document.getElementById('createPartnerModal')).hide();
            window.location.reload();
        } catch (error) {
            console.error('Error:', error);
            window.c92.showToast('error', 'An error occurred while creating the partner: ' + error.message);
        }
    });

    console.log('status.js loaded');

    let currentPartnerId = null;

    async function loadPartner(partnerId) {
        try {
            console.log('Fetching partner with ID:', partnerId);
            const response = await fetch(`/CRM/Partners/GetPartner?id=${partnerId}`, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                const contentType = response.headers.get('content-type');
                let errorMessage = `Partner ${partnerId} not found`;
                if (contentType && contentType.includes('application/json')) {
                    const error = await response.json();
                    errorMessage = error.message || errorMessage || JSON.stringify(error.errors);
                } else {
                    errorMessage = await response.text() || errorMessage;
                }
                throw new Error(errorMessage);
            }

            const data = await response.json();
            console.log('Fetched partner data:', JSON.stringify(data, null, 2));

            // Populate View Modal
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
            const statusBadge = document.getElementById('viewPartnerStatusBadge');
            statusBadge.textContent = data.status ? data.status.name : 'N/A';
            statusBadge.style.backgroundColor = data.status ? data.status.color : '#6c757d';
            statusBadge.style.color = data.status && data.status.color === '#ffc107' ? 'black' : 'white';
            document.getElementById('viewPartnerBillingContactName').textContent = data.billingContactName || '';
            document.getElementById('viewPartnerBillingEmail').textContent = data.billingEmail || '';
            document.getElementById('viewPartnerPaymentTerms').textContent = data.paymentTerms || '';
            document.getElementById('viewPartnerCreditLimit').textContent = data.creditLimit || '';
            document.getElementById('viewPartnerPreferredCurrency').textContent = data.preferredCurrency || '';
            document.getElementById('viewPartnerIsTaxExempt').textContent = data.isTaxExempt ? 'Igen' : 'Nem';
            document.getElementById('viewPartnerAssignedTo').textContent = data.assignedTo || '';
            document.getElementById('viewPartnerPartnerGroupId').textContent = data.partnerGroupId || '';
            document.getElementById('viewPartnerLastContacted').textContent = data.lastContacted ? new Date(data.lastContacted).toLocaleDateString() : '';
            document.getElementById('viewPartnerNotes').textContent = data.notes || '';

            // Populate Sites
            const sitesContainer = document.getElementById('sites-content');
            sitesContainer.innerHTML = '';
            if (data.sites && data.sites.length > 0) {
                sitesContainer.innerHTML = `
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Név</th>
                                <th>Cím</th>
                                <th>Város</th>
                                <th>Irányítószám</th>
                                <th>Státusz</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${data.sites.map(site => `
                                <tr>
                                    <td>${site.name || ''}</td>
                                    <td>${site.addressLine1 || ''}</td>
                                    <td>${site.city || ''}</td>
                                    <td>${site.postalCode || ''}</td>
                                    <td><span class="badge" style="background-color: ${site.status ? site.status.color : '#6c757d'}; color: ${site.status && site.status.color === '#ffc107' ? 'black' : 'white'}">${site.status ? site.status.name : 'N/A'}</span></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                `;
            } else {
                sitesContainer.innerHTML = '<p>Nincsenek telephelyek.</p>';
            }

            // Populate Contacts
            const contactsContainer = document.getElementById('contacts-content');
            contactsContainer.innerHTML = '';
            if (data.contacts && data.contacts.length > 0) {
                contactsContainer.innerHTML = `
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Név</th>
                                <th>Email</th>
                                <th>Telefonszám</th>
                                <th>Munkakör</th>
                                <th>Elsődleges</th>
                                <th>Státusz</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${data.contacts.map(contact => `
                                <tr>
                                    <td>${contact.firstName || ''} ${contact.lastName || ''}</td>
                                    <td>${contact.email || ''}</td>
                                    <td>${contact.phoneNumber || ''}</td>
                                    <td>${contact.jobTitle || ''}</td>
                                    <td>${contact.isPrimary ? 'Igen' : 'Nem'}</td>
                                    <td><span class="badge" style="background-color: ${contact.status ? contact.status.color : '#6c757d'}; color: ${contact.status && contact.status.color === '#ffc107' ? 'black' : 'white'}">${contact.status ? contact.status.name : 'N/A'}</span></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                `;
            } else {
                contactsContainer.innerHTML = '<p>Nincsenek kapcsolattartók.</p>';
            }

            // Populate Documents
            const documentsContainer = document.getElementById('documents-content');
            documentsContainer.innerHTML = '';
            if (data.documents && data.documents.length > 0) {
                documentsContainer.innerHTML = `
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Név</th>
                                <th>Típus</th>
                                <th>Feltöltés dátuma</th>
                                <th>Státusz</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${data.documents.map(doc => `
                                <tr>
                                    <td>${doc.name || ''}</td>
                                    <td>${doc.documentTypeName || 'N/A'}</td>
                                    <td>${doc.uploadDate ? new Date(doc.uploadDate).toLocaleDateString() : 'N/A'}</td>
                                    <td><span class="badge" style="background-color: ${doc.status ? doc.status.color : '#6c757d'}; color: ${doc.status && doc.status.color === '#ffc107' ? 'black' : 'white'}">${doc.status ? doc.status.name : 'N/A'}</span></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                `;
            } else {
                documentsContainer.innerHTML = '<p>Nincsenek dokumentumok.</p>';
            }

            const viewModal = new bootstrap.Modal(document.getElementById('viewPartnerModal'));
            viewModal.show();
        } catch (err) {
            console.error('Error loading partner:', err);
            window.c92.showToast('error', 'Hiba: ' + err.message);
        }
    }

    async function loadEditPartner(partnerId) {
        try {
            console.log('Loading edit data for partner with ID:', partnerId);
            const response = await fetch(`/CRM/Partners/GetPartner?id=${partnerId}`, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                const contentType = response.headers.get('content-type');
                let errorMessage = `Partner ${partnerId} not found`;
                if (contentType && contentType.includes('application/json')) {
                    const error = await response.json();
                    errorMessage = error.message || errorMessage || JSON.stringify(error.errors);
                } else {
                    errorMessage = await response.text() || errorMessage;
                }
                throw new Error(errorMessage);
            }

            const data = await response.json();
            console.log('Fetched edit partner data:', JSON.stringify(data, null, 2));

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
            document.getElementById('editPartnerStatusId').value = data.statusId || '';
            document.getElementById('editPartnerBillingContactName').value = data.billingContactName || '';
            document.getElementById('editPartnerBillingEmail').value = data.billingEmail || '';
            document.getElementById('editPartnerPaymentTerms').value = data.paymentTerms || '';
            document.getElementById('editPartnerCreditLimit').value = data.creditLimit || '';
            document.getElementById('editPartnerPreferredCurrency').value = data.preferredCurrency || '';
            document.getElementById('editPartnerIsTaxExempt').value = data.isTaxExempt ? 'true' : 'false';
            document.getElementById('editPartnerAssignedTo').value = data.assignedTo || '';
            document.getElementById('editPartnerPartnerGroupId').value = data.partnerGroupId || '';
            document.getElementById('editPartnerLastContacted').value = data.lastContacted ? data.lastContacted.split('T')[0] : '';
            document.getElementById('editPartnerNotes').value = data.notes || '';

            // Populate Sites
            const sitesContainer = document.getElementById('sites-edit-content');
            sitesContainer.innerHTML = '';
            if (data.sites && data.sites.length > 0) {
                sitesContainer.innerHTML = `
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Név</th>
                                <th>Cím</th>
                                <th>Város</th>
                                <th>Irányítószám</th>
                                <th>Státusz</th>
                                <th>Műveletek</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${data.sites.map(site => `
                                <tr data-site-id="${site.siteId}">
                                    <td><input type="text" class="form-control site-name" value="${site.name || ''}" readonly></td>
                                    <td><input type="text" class="form-control site-address" value="${site.addressLine1 || ''}" readonly></td>
                                    <td><input type="text" class="form-control site-city" value="${site.city || ''}" readonly></td>
                                    <td><input type="text" class="form-control site-postal-code" value="${site.postalCode || ''}" readonly></td>
                                    <td><select class="form-control site-status-id" disabled><option value="${site.statusId || ''}" selected>${site.status ? site.status.name : 'N/A'}</option></select></td>
                                    <td>
                                        <button class="btn btn-sm btn-warning edit-site-btn">Szerkesztés</button>
                                        <button class="btn btn-sm btn-danger remove-site-btn">Eltávolítás</button>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                `;
            } else {
                sitesContainer.innerHTML = '<p>Nincsenek telephelyek.</p>';
            }

            // Populate Contacts
            const contactsContainer = document.getElementById('contacts-edit-content');
            contactsContainer.innerHTML = '';
            if (data.contacts && data.contacts.length > 0) {
                contactsContainer.innerHTML = `
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Név</th>
                                <th>Email</th>
                                <th>Telefonszám</th>
                                <th>Munkakör</th>
                                <th>Elsődleges</th>
                                <th>Státusz</th>
                                <th>Műveletek</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${data.contacts.map(contact => `
                                <tr data-contact-id="${contact.contactId}">
                                    <td><input type="text" class="form-control contact-first-name" value="${contact.firstName || ''}" readonly></td>
                                    <td><input type="text" class="form-control contact-last-name" value="${contact.lastName || ''}" readonly></td>
                                    <td><input type="email" class="form-control contact-email" value="${contact.email || ''}" readonly></td>
                                    <td><input type="text" class="form-control contact-phone" value="${contact.phoneNumber || ''}" readonly></td>
                                    <td><input type="text" class="form-control contact-job-title" value="${contact.jobTitle || ''}" readonly></td>
                                    <td><input type="checkbox" class="form-control contact-is-primary" ${contact.isPrimary ? 'checked' : ''} disabled></td>
                                    <td><select class="form-control contact-status-id" disabled><option value="${contact.statusId || ''}" selected>${contact.status ? contact.status.name : 'N/A'}</option></select></td>
                                    <td>
                                        <button class="btn btn-sm btn-warning edit-contact-btn">Szerkesztés</button>
                                        <button class="btn btn-sm btn-danger remove-contact-btn">Eltávolítás</button>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                `;
            } else {
                contactsContainer.innerHTML = '<p>Nincsenek kapcsolattartók.</p>';
            }

            // Populate Documents
            const documentsContainer = document.getElementById('documents-edit-content');
            documentsContainer.innerHTML = '';
            if (data.documents && data.documents.length > 0) {
                documentsContainer.innerHTML = `
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Név</th>
                                <th>Típus</th>
                                <th>Feltöltés dátuma</th>
                                <th>Státusz</th>
                                <th>Műveletek</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${data.documents.map(doc => `
                                <tr data-document-id="${doc.documentId}">
                                    <td><input type="text" class="form-control document-name" value="${doc.name || ''}" readonly></td>
                                    <td><input type="text" class="form-control document-type" value="${doc.documentTypeName || 'N/A'}" readonly></td>
                                    <td><input type="text" class="form-control document-upload-date" value="${doc.uploadDate ? new Date(doc.uploadDate).toLocaleDateString() : 'N/A'}" readonly></td>
                                    <td><select class="form-control document-status-id" disabled><option value="${doc.statusId || ''}" selected>${doc.status ? doc.status.name : 'N/A'}</option></select></td>
                                    <td>
                                        <button class="btn btn-sm btn-warning edit-document-btn">Szerkesztés</button>
                                        <button class="btn btn-sm btn-danger remove-document-btn">Eltávolítás</button>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                `;
            } else {
                documentsContainer.innerHTML = '<p>Nincsenek dokumentumok.</p>';
            }

            const editModal = new bootstrap.Modal(document.getElementById('editPartnerModal'));
            editModal.show();

            // Initialize tabs
            setTimeout(() => {
                console.log('Initializing tabs');
                const tabTriggers = document.querySelectorAll('.nav-link');
                tabTriggers.forEach(tab => {
                    tab.addEventListener('click', () => {
                        new bootstrap.Tab(tab).show();
                        console.log(`Switched to tab: ${tab.getAttribute('data-bs-target')}`);
                    });
                });
                new bootstrap.Tab(document.querySelector('.nav-link.active')).show();
            }, 100);
        } catch (err) {
            console.error('Error loading edit partner:', err);
            window.c92.showToast('error', 'Hiba: ' + err.message);
        }
    }

    function addSite() {
        const sitesContainer = document.getElementById('sites-edit-content');
        if (!sitesContainer) {
            console.error('Sites container not found');
            return;
        }
        const siteId = `new-${Date.now()}`;
        const newRow = document.createElement('div');
        newRow.className = 'row';
        newRow.setAttribute('data-site-id', siteId);
        newRow.innerHTML = `
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Név</label>
                <input type="text" class="form-control site-name" required>
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Cím</label>
                <input type="text" class="form-control site-address">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Város</label>
                <input type="text" class="form-control site-city">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Irányítószám</label>
                <input type="text" class="form-control site-postal-code">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Státusz</label>
                <select class="form-control site-status-id">
                    <option value="">Válasszon...</option>
                    <option value="1">Active</option>
                    <option value="2">Inactive</option>
                    <option value="3" selected>Prospect</option>
                </select>
            </div>
            <div class="col-12 text-end">
                <button type="button" class="btn btn-success save-site-btn">Mentés</button>
                <button type="button" class="btn btn-secondary cancel-site-btn">Mégse</button>
            </div>
            <hr class="my-4">
        `;
        sitesContainer.appendChild(newRow);
        console.log('Added new site with data-site-id:', siteId);
    }

    function addContact() {
        const contactsContainer = document.getElementById('contacts-edit-content');
        if (!contactsContainer) {
            console.error('Contacts container not found');
            return;
        }
        const contactId = `new-${Date.now()}`;
        const newRow = document.createElement('div');
        newRow.className = 'row';
        newRow.setAttribute('data-contact-id', contactId);
        newRow.innerHTML = `
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Vezetéknév</label>
                <input type="text" class="form-control contact-first-name" required>
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Keresztnév</label>
                <input type="text" class="form-control contact-last-name" required>
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">E-mail</label>
                <input type="email" class="form-control contact-email">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Telefonszám</label>
                <input type="text" class="form-control contact-phone">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Munkakör</label>
                <input type="text" class="form-control contact-job-title">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Megjegyzés</label>
                <input type="text" class="form-control contact-comment">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Elsődleges</label>
                <select class="form-control contact-is-primary">
                    <option value="false">Nem</option>
                    <option value="true">Igen</option>
                </select>
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Státusz</label>
                <select class="form-control contact-status-id">
                    <option value="">Válasszon...</option>
                    <option value="1" selected>Active</option>
                    <option value="2">Inactive</option>
                    <option value="3">Prospect</option>
                </select>
            </div>
            <div class="col-12 text-end">
                <button type="button" class="btn btn-success save-contact-btn">Mentés</button>
                <button type="button" class="btn btn-secondary cancel-contact-btn">Mégse</button>
            </div>
            <hr class="my-4">
        `;
        contactsContainer.appendChild(newRow);
        newRow.querySelector('.contact-first-name').focus();
        console.log('Added new contact with data-contact-id:', contactId);
    }

    function addDocument() {
        const documentsContainer = document.getElementById('documents-edit-content');
        if (!documentsContainer) {
            console.error('Documents container not found');
            return;
        }
        const documentId = `new-${Date.now()}`;
        const newRow = document.createElement('div');
        newRow.className = 'row';
        newRow.setAttribute('data-document-id', documentId);
        newRow.innerHTML = `
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Név</label>
                <input type="text" class="form-control document-name" required>
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Típus</label>
                <input type="text" class="form-control document-type">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Feltöltés dátuma</label>
                <input type="date" class="form-control document-upload-date">
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Státusz</label>
                <select class="form-control document-status-id">
                    <option value="">Válasszon...</option>
                    <option value="1" selected>Active</option>
                    <option value="2">Inactive</option>
                    <option value="3">Prospect</option>
                </select>
            </div>
            <div class="col-12 text-end">
                <button type="button" class="btn btn-success save-document-btn">Mentés</button>
                <button type="button" class="btn btn-secondary cancel-document-btn">Mégse</button>
            </div>
            <hr class="my-4">
        `;
        documentsContainer.appendChild(newRow);
        console.log('Added new document with data-document-id:', documentId);
    }

    document.addEventListener('click', async function (event) {
        console.log('Click event detected on:', event.target);
        const viewBtn = event.target.closest('.view-partner-btn');
        if (viewBtn) {
            const partnerId = viewBtn.getAttribute('data-partner-id');
            console.log('View button clicked, partnerId:', partnerId);
            if (partnerId) {
                await loadPartner(partnerId);
            } else {
                console.error('No partnerId found on view button');
                window.c92.showToast('error', 'Hiba: Nincs megadva partner ID');
            }
        }

        const editBtn = event.target.closest('.edit-partner-btn');
        if (editBtn) {
            const partnerId = editBtn.getAttribute('data-partner-id');
            console.log('Edit button clicked, partnerId:', partnerId);
            if (partnerId) {
                await loadEditPartner(partnerId);
            } else {
                console.error('No partnerId found for edit');
                window.c92.showToast('error', 'Hiba: Nincs megadva partner ID a szerkesztéshez');
            }
        }

        if (event.target.id === 'addSiteBtn') {
            console.log('Add Site button clicked');
            addSite();
        }

        if (event.target.id === 'addContactBtn') {
            console.log('Add Contact button clicked');
            addContact();
        }

        if (event.target.id === 'addDocumentBtn') {
            console.log('Add Document button clicked');
            addDocument();
        }

        if (event.target.classList.contains('remove-site-btn')) {
            console.log('Remove Site button clicked');
            if (confirm('Biztosan eltávolítja ezt a telephelyet?')) {
                const siteId = event.target.closest('.row').getAttribute('data-site-id');
                await deleteSite(siteId);
                await loadEditPartner(currentPartnerId);
            }
        }

        if (event.target.classList.contains('remove-contact-btn')) {
            console.log('Remove Contact button clicked');
            if (confirm('Biztosan eltávolítja ezt a kapcsolattartót?')) {
                const contactId = event.target.closest('.row').getAttribute('data-contact-id');
                await deleteContact(contactId);
                await loadEditPartner(currentPartnerId);
            }
        }

        if (event.target.classList.contains('remove-document-btn')) {
            console.log('Remove Document button clicked');
            if (confirm('Biztosan eltávolítja ezt a dokumentumot?')) {
                const documentId = event.target.closest('.row').getAttribute('data-document-id');
                await deleteDocument(documentId);
                await loadEditPartner(currentPartnerId);
            }
        }

        if (event.target.classList.contains('edit-site-btn')) {
            console.log('Edit Site button clicked');
            const row = event.target.closest('.row');
            if (!row) {
                console.error('No row found for edit-site-btn');
                window.c92.showToast('error', 'Hiba: Nem található a sor a szerkesztéshez.');
                return;
            }
            const inputs = row.querySelectorAll('input');
            const select = row.querySelector('select');
            inputs.forEach(input => {
                input.removeAttribute('readonly');
                console.log('Removed readonly from input:', input.className);
            });
            if (select) {
                select.removeAttribute('disabled');
                console.log('Removed disabled from select:', select.className);
            }
            event.target.classList.remove('edit-site-btn');
            event.target.classList.add('save-site-btn');
            event.target.textContent = 'Mentés';
            console.log('Edit mode enabled for row with data-site-id:', row.getAttribute('data-site-id'));
        }

        if (event.target.classList.contains('edit-contact-btn')) {
            console.log('Edit Contact button clicked');
            const row = event.target.closest('.row');
            if (!row) {
                console.error('No row found for edit-contact-btn');
                window.c92.showToast('error', 'Hiba: Nem található a sor a szerkesztéshez.');
                return;
            }
            const inputs = row.querySelectorAll('input');
            const select = row.querySelector('select');
            inputs.forEach(input => {
                input.removeAttribute('readonly');
                console.log('Removed readonly from input:', input.className);
                if (input.classList.contains('contact-first-name')) {
                    input.focus();
                }
            });
            if (select) {
                select.removeAttribute('disabled');
                console.log('Removed disabled from select:', select.className);
            }
            event.target.classList.remove('edit-contact-btn');
            event.target.classList.add('save-contact-btn');
            event.target.textContent = 'Mentés';
            console.log('Edit mode enabled for row with data-contact-id:', row.getAttribute('data-contact-id'));
        }

        if (event.target.classList.contains('edit-document-btn')) {
            console.log('Edit Document button clicked');
            const row = event.target.closest('.row');
            if (!row) {
                console.error('No row found for edit-document-btn');
                window.c92.showToast('error', 'Hiba: Nem található a sor a szerkesztéshez.');
                return;
            }
            const inputs = row.querySelectorAll('input');
            const select = row.querySelector('select');
            inputs.forEach(input => {
                input.removeAttribute('readonly');
                console.log('Removed readonly from input:', input.className);
            });
            if (select) {
                select.removeAttribute('disabled');
                console.log('Removed disabled from select:', select.className);
            }
            event.target.classList.remove('edit-document-btn');
            event.target.classList.add('save-document-btn');
            event.target.textContent = 'Mentés';
            console.log('Edit mode enabled for row with data-document-id:', row.getAttribute('data-document-id'));
        }

        if (event.target.classList.contains('save-site-btn')) {
            console.log('Save Site button clicked');
            const row = event.target.closest('.row');
            if (!row) {
                console.error('No row found for save-site-btn');
                window.c92.showToast('error', 'Hiba: Nem található a sor a mentéshez.');
                return;
            }
            const siteId = row.getAttribute('data-site-id');
            if (!siteId) {
                console.error('No data-site-id attribute found on row');
                window.c92.showToast('error', 'Hiba: Hiányzik a site azonosító.');
                return;
            }
            const siteData = {
                partnerId: currentPartnerId,
                siteId: siteId.startsWith('new-') ? null : parseInt(siteId),
                name: row.querySelector('.site-name')?.value.trim() || '',
                addressLine1: row.querySelector('.site-address')?.value.trim() || '',
                city: row.querySelector('.site-city')?.value.trim() || null,
                postalCode: row.querySelector('.site-postal-code')?.value.trim() || null,
                statusId: row.querySelector('.site-status-id')?.value ? parseInt(row.querySelector('.site-status-id').value) : null
            };
            if (!siteData.name) {
                console.error('Required fields missing:', siteData);
                window.c92.showToast('error', 'Hiba: Név mező kitöltése kötelező!');
                return;
            }
            console.log('Saving site data:', siteData);
            const url = siteId.startsWith('new-') ? `/api/Sites?partnerId=${currentPartnerId}` : `/api/Sites/${siteId}?partnerId=${currentPartnerId}`;
            const method = siteId.startsWith('new-') ? 'POST' : 'PUT';
            const result = await saveSite({ url, method, data: siteData });
            if (result && siteId.startsWith('new-')) {
                row.setAttribute('data-site-id', result.siteId);
            }
            setTimeout(() => {
                row.querySelectorAll('input').forEach(input => input.setAttribute('readonly', true));
                row.querySelectorAll('select').forEach(select => select.setAttribute('disabled', true));
                event.target.classList.remove('save-site-btn');
                event.target.classList.add('edit-site-btn');
                event.target.textContent = 'Szerkesztés';
                loadEditPartner(currentPartnerId);
            }, 500);
        }

        if (event.target.classList.contains('save-contact-btn')) {
            console.log('Save Contact button clicked');
            const row = event.target.closest('.row');
            if (!row) {
                console.error('No row found for save-contact-btn');
                window.c92.showToast('error', 'Hiba: Nem található a sor a mentéshez.');
                return;
            }
            const contactId = row.getAttribute('data-contact-id');
            if (!contactId) {
                console.error('No data-contact-id attribute found on row');
                window.c92.showToast('error', 'Hiba: Hiányzik a contact azonosító.');
                return;
            }
            const contactData = {
                contactId: contactId.startsWith('new-') ? null : parseInt(contactId),
                partnerId: currentPartnerId,
                firstName: row.querySelector('.contact-first-name')?.value.trim() || '',
                lastName: row.querySelector('.contact-last-name')?.value.trim() || '',
                email: row.querySelector('.contact-email')?.value.trim() || null,
                phoneNumber: row.querySelector('.contact-phone')?.value.trim() || null,
                jobTitle: row.querySelector('.contact-job-title')?.value.trim() || null,
                comment: row.querySelector('.contact-comment')?.value.trim() || null,
                isPrimary: row.querySelector('.contact-is-primary')?.value === 'true',
                statusId: row.querySelector('.contact-status-id')?.value ? parseInt(row.querySelector('.contact-status-id').value) : null
            };
            if (!contactData.firstName || !contactData.lastName) {
                console.error('Required fields missing:', contactData);
                window.c92.showToast('error', 'Hiba: Vezetéknév és Keresztnév mezők kitöltése kötelező!');
                return;
            }
            console.log('Saving contact data:', contactData);
            const url = contactId.startsWith('new-') ? `/api/partners/${currentPartnerId}/contacts` : `/api/partners/${currentPartnerId}/contacts/${contactId}`;
            const method = contactId.startsWith('new-') ? 'POST' : 'PUT';
            const result = await saveContact({ url, method, data: contactData });
            if (result && contactId.startsWith('new-')) {
                row.setAttribute('data-contact-id', result.contactId);
            }
            setTimeout(() => {
                row.querySelectorAll('input').forEach(input => input.setAttribute('readonly', true));
                row.querySelectorAll('select').forEach(select => select.setAttribute('disabled', true));
                event.target.classList.remove('save-contact-btn');
                event.target.classList.add('edit-contact-btn');
                event.target.textContent = 'Szerkesztés';
                loadEditPartner(currentPartnerId);
            }, 500);
        }

        if (event.target.classList.contains('save-document-btn')) {
            console.log('Save Document button clicked');
            const row = event.target.closest('.row');
            if (!row) {
                console.error('No row found for save-document-btn');
                window.c92.showToast('error', 'Hiba: Nem található a sor a mentéshez.');
                return;
            }
            const documentId = row.getAttribute('data-document-id');
            if (!documentId) {
                console.error('No data-document-id attribute found on row');
                window.c92.showToast('error', 'Hiba: Hiányzik a document azonosító.');
                return;
            }
            const documentData = {
                documentId: documentId.startsWith('new-') ? null : parseInt(documentId),
                partnerId: currentPartnerId,
                name: row.querySelector('.document-name')?.value.trim() || '',
                documentTypeName: row.querySelector('.document-type')?.value.trim() || null,
                uploadDate: row.querySelector('.document-upload-date')?.value || null,
                statusId: row.querySelector('.document-status-id')?.value ? parseInt(row.querySelector('.document-status-id').value) : null
            };
            if (!documentData.name) {
                console.error('Required fields missing:', documentData);
                window.c92.showToast('error', 'Hiba: Név mező kitöltése kötelező!');
                return;
            }
            console.log('Saving document data:', documentData);
            const result = await saveDocument(documentData);
            if (result && documentId.startsWith('new-')) {
                row.setAttribute('data-document-id', result.documentId);
            }
            setTimeout(() => {
                row.querySelectorAll('input').forEach(input => input.setAttribute('readonly', true));
                row.querySelectorAll('select').forEach(select => select.setAttribute('disabled', true));
                event.target.classList.remove('save-document-btn');
                event.target.classList.add('edit-document-btn');
                event.target.textContent = 'Szerkesztés';
                loadEditPartner(currentPartnerId);
            }, 500);
        }

        if (event.target.classList.contains('cancel-site-btn')) {
            console.log('Cancel Site button clicked');
            await loadEditPartner(currentPartnerId);
        }

        if (event.target.classList.contains('cancel-contact-btn')) {
            console.log('Cancel Contact button clicked');
            await loadEditPartner(currentPartnerId);
        }

        if (event.target.classList.contains('cancel-document-btn')) {
            console.log('Cancel Document button clicked');
            await loadEditPartner(currentPartnerId);
        }
    });

    async function saveSite({ url, method, data }) {
        try {
            console.log(`Saving site, URL: ${url}, Method: ${method}, Data:`, data);
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            if (!response.ok) {
                const errorText = await response.text();
                console.error(`Save site failed, Status: ${response.status}, Error: ${errorText || 'No response body'}`);
                throw new Error(`Failed to save site: ${errorText || response.statusText}`);
            }
            const result = response.status === 204 ? { siteId: data.siteId } : await response.json();
            console.log('Site saved:', result);
            window.c92.showToast('success', 'Telephely sikeresen mentve!');
            return result;
        } catch (err) {
            console.error('Error saving site:', err);
            window.c92.showToast('error', 'Hiba: ' + err.message);
        }
    }

    async function saveContact({ url, method, data }) {
        try {
            console.log(`Saving contact, URL: ${url}, Method: ${method}, Data:`, data);
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            if (!response.ok) {
                const contentType = response.headers.get('content-type');
                let errorMessage = 'Failed to save contact';
                if (contentType && contentType.includes('application/json')) {
                    const error = await response.json();
                    errorMessage = error.error || error.message || JSON.stringify(error.errors) || errorMessage;
                } else {
                    errorMessage = await response.text() || errorMessage;
                }
                console.error(`Save contact failed, Status: ${response.status}, Error: ${errorMessage}`);
                throw new Error(errorMessage);
            }
            const result = response.status === 204 ? {} : await response.json();
            console.log('Contact saved:', result);
            window.c92.showToast('success', 'Kapcsolattartó sikeresen mentve!');
            return result;
        } catch (err) {
            console.error('Error saving contact:', err);
            window.c92.showToast('error', `Hiba a kapcsolattartó mentése közben: ${err.message}`);
            return null;
        }
    }

    async function saveDocument(documentData) {
        try {
            const url = documentData.documentId ? `/api/Documents/${documentData.documentId}` : '/api/Documents';
            const method = documentData.documentId ? 'PUT' : 'POST';
            console.log(`Saving document, URL: ${url}, Method: ${method}, Data:`, documentData);
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(documentData)
            });
            if (!response.ok) {
                const errorText = await response.text();
                console.error(`Save document failed, Status: ${response.status}, Error: ${errorText}`);
                throw new Error(`Failed to save document: ${errorText || response.statusText}`);
            }
            const result = await response.json();
            console.log('Document saved:', result);
            window.c92.showToast('success', 'Dokumentum sikeresen mentve!');
            return result;
        } catch (err) {
            console.error('Error saving document:', err);
            window.c92.showToast('error', 'Hiba: ' + err.message);
            return null;
        }
    }

    async function deleteSite(siteId) {
        try {
            console.log(`Deleting site, URL: /api/Sites/${siteId}?partnerId=${currentPartnerId}`);
            const response = await fetch(`/api/Sites/${siteId}?partnerId=${currentPartnerId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' }
            });
            if (!response.ok) {
                const errorText = await response.text();
                console.error(`Delete site failed, Status: ${response.status}, Error: ${errorText || 'No response body'}`);
                throw new Error(`Failed to delete site: ${errorText || response.statusText}`);
            }
            console.log('Site deleted:', siteId);
            window.c92.showToast('success', 'Telephely sikeresen törölve!');
            return true;
        } catch (err) {
            console.error('Error deleting site:', err);
            window.c92.showToast('error', 'Hiba: ' + err.message);
        }
    }

    async function deleteContact(contactId) {
        try {
            console.log(`Deleting contact, URL: /api/partners/${currentPartnerId}/contacts/${contactId}`);
            const response = await fetch(`/api/partners/${currentPartnerId}/contacts/${contactId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' }
            });
            if (!response.ok) {
                const errorText = await response.text();
                console.error(`Delete contact failed, Status: ${response.status}, Error: ${errorText || 'No response body'}`);
                throw new Error(`Failed to delete contact: ${errorText || response.statusText}`);
            }
            console.log('Contact deleted:', contactId);
            window.c92.showToast('success', 'Kapcsolattartó sikeresen törölve!');
            return true;
        } catch (err) {
            console.error('Error deleting contact:', err);
            window.c92.showToast('error', 'Hiba: ' + err.message);
        }
    }

    async function deleteDocument(documentId) {
        try {
            console.log(`Deleting document, URL: /api/Documents/${documentId}`);
            const response = await fetch(`/api/Documents/${documentId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' }
            });
            if (!response.ok) {
                const errorText = await response.text();
                console.error(`Delete document failed, Status: ${response.status}, Error: ${errorText || 'No response body'}`);
                throw new Error(`Failed to delete document: ${errorText || response.statusText}`);
            }
            console.log('Document deleted:', documentId);
            window.c92.showToast('success', 'Dokumentum sikeresen törölve!');
            return true;
        } catch (err) {
            console.error('Error deleting document:', err);
            window.c92.showToast('error', 'Hiba: ' + err.message);
        }
    }

    document.getElementById('editPartnerForm').addEventListener('submit', async function (event) {
        event.preventDefault();
        const formData = new FormData(this);
        const data = {
            partnerId: parseInt(formData.get('partnerId')),
            Name: formData.get('Name'),
            Email: formData.get('Email') || null,
            PhoneNumber: formData.get('PhoneNumber') || null,
            AlternatePhone: formData.get('AlternatePhone') || null,
            Website: formData.get('Website') || null,
            CompanyName: formData.get('CompanyName') || null,
            TaxId: formData.get('TaxId') || null,
            IntTaxId: formData.get('IntTaxId') || null,
            Industry: formData.get('Industry') || null,
            AddressLine1: formData.get('AddressLine1') || null,
            AddressLine2: formData.get('AddressLine2') || null,
            City: formData.get('City') || null,
            State: formData.get('State') || null,
            PostalCode: formData.get('PostalCode') || null,
            Country: formData.get('Country') || null,
            StatusId: formData.get('StatusId') ? parseInt(formData.get('StatusId')) : null,
            LastContacted: formData.get('LastContacted') || null,
            Notes: formData.get('Notes') || null,
            AssignedTo: formData.get('AssignedTo') || null,
            BillingContactName: formData.get('BillingContactName') || null,
            BillingEmail: formData.get('BillingEmail') || null,
            PaymentTerms: formData.get('PaymentTerms') || null,
            CreditLimit: formData.get('CreditLimit') ? parseFloat(formData.get('CreditLimit')) : null,
            PreferredCurrency: formData.get('PreferredCurrency') || null,
            IsTaxExempt: formData.get('IsTaxExempt') === 'true',
            PartnerGroupId: formData.get('PartnerGroupId') ? parseInt(formData.get('PartnerGroupId')) : null,
            UpdatedBy: "System",
            UpdatedDate: new Date().toISOString()
        };

        try {
            console.log('Submitting edit partner form:', data);
            const response = await fetch(`/api/Partners/${currentPartnerId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Update response error:', errorText);
                throw new Error(`Failed to update partner: ${errorText || response.statusText}`);
            }
            window.c92.showToast('success', 'Partner sikeresen frissítve!');
            const modal = bootstrap.Modal.getInstance(document.getElementById('editPartnerModal'));
            modal.hide();
            await loadPartner(currentPartnerId);
            window.location.reload();
        } catch (err) {
            console.error('Error updating partner:', err);
            window.c92.showToast('error', 'Hiba: ' + err.message);
        }
    });
});