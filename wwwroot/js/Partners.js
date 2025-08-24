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
            Website: formData.get('Website') || null,
            TaxId: formData.get('TaxId') || null,
            AddressLine1: formData.get('AddressLine1') || null,
            City: formData.get('City') || null,
            PostalCode: formData.get('PostalCode') || null,
            Country: formData.get('Country') || null
        };

        try {
            console.log('Sending partnerDto:', partnerDto); // Log the payload
            const response = await fetch('/api/Partners/CreatePartner', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(partnerDto)
            });

            console.log('Response status:', response.status, 'Content-Type:', response.headers.get('content-type')); // Log response details

            let errorMessage = 'Failed to create partner';
            if (!response.ok) {
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    const error = await response.json();
                    errorMessage = error.message || errorMessage;
                } else {
                    const text = await response.text();
                    errorMessage = text || 'Server returned an invalid response';
                    console.log('Raw response text:', text); // Log raw response for debugging
                }
                console.error('Error:', errorMessage);
                window.c92.showToast('error', 'Failed to create partner: ' + errorMessage);
                return;
            }

            const result = await response.json();
            window.c92.showToast('success', `Partner created successfully with ID: ${result.partnerId}`);
            form.reset(); // Clear the form fields
            bootstrap.Modal.getInstance(document.getElementById('createPartnerModal')).hide();
        } catch (error) {
            console.error('Error:', error);
            window.c92.showToast('error', 'An error occurred while creating the partner: ' + error.message);
        }
    });

console.log('partnerModal.js loaded');

let currentPartnerId = null;

async function loadPartner(partnerId) {
    try {
        console.log('Fetching partner with ID:', partnerId);
        const response = await fetch(`/api/Partners/${partnerId}`, {
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
        document.getElementById('viewPartnerStatus').textContent = data.status || '';
        document.getElementById('viewPartnerBillingContactName').textContent = data.billingContactName || '';
        document.getElementById('viewPartnerBillingEmail').textContent = data.billingEmail || '';
        document.getElementById('viewPartnerPaymentTerms').textContent = data.paymentTerms || '';
        document.getElementById('viewPartnerCreditLimit').textContent = data.creditLimit?.toString() || '';
        document.getElementById('viewPartnerPreferredCurrency').textContent = data.preferredCurrency || '';
        document.getElementById('viewPartnerIsTaxExempt').textContent = data.isTaxExempt ? 'Igen' : 'Nem';
        document.getElementById('viewPartnerAssignedTo').textContent = data.assignedTo || '';
        document.getElementById('viewPartnerPartnerGroupId').textContent = data.partnerGroupId || '';
        document.getElementById('viewPartnerLastContacted').textContent = data.lastContacted || '';
        document.getElementById('viewPartnerNotes').textContent = data.notes || '';

        const sitesContainer = document.getElementById('sites-content');
        sitesContainer.innerHTML = '';
        (data.sites || []).forEach((site) => {
            sitesContainer.innerHTML += `
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
            `;
        });
        if (!(data.sites || []).length) {
            sitesContainer.innerHTML = '<p>Nincsenek telephelyek.</p>';
        }

        const contactsContainer = document.getElementById('contacts-content');
        contactsContainer.innerHTML = '';
        (data.contacts || []).forEach((contact) => {
            contactsContainer.innerHTML += `
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
            `;
        });
        if (!(data.contacts || []).length) {
            contactsContainer.innerHTML = '<p>Nincsenek kapcsolattartók.</p>';
        }

        const documentsContainer = document.getElementById('documents-content');
        documentsContainer.innerHTML = '';
        (data.documents || []).forEach((doc) => {
            documentsContainer.innerHTML += `
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Név</label>
                        <p class="form-control-static">${doc.name || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Típus</label>
                        <p class="form-control-static">${doc.type || ''}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Feltöltés dátuma</label>
                        <p class="form-control-static">${doc.uploadDate || ''}</p>
                    </div>
                </div>
                <hr class="my-4">
            `;
        });
        if (!(data.documents || []).length) {
            documentsContainer.innerHTML = '<p>Nincsenek dokumentumok.</p>';
        }

        const viewModal = new bootstrap.Modal(document.getElementById('viewPartnerModal'));
        viewModal.show();
    } catch (err) {
        console.error('Error loading partner:', err);
        alert('Hiba: ' + err.message);
    }
}

async function loadEditPartner(partnerId) {
    try {
        console.log('Loading edit data for partner with ID:', partnerId);
        const response = await fetch(`/api/Partners/${partnerId}`, {
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
        document.getElementById('editPartnerId').value = data.partnerId;
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
        document.getElementById('editPartnerStatus').value = data.status || '';
        document.getElementById('editPartnerBillingContactName').value = data.billingContactName || '';
        document.getElementById('editPartnerBillingEmail').value = data.billingEmail || '';
        document.getElementById('editPartnerPaymentTerms').value = data.paymentTerms || '';
        document.getElementById('editPartnerCreditLimit').value = data.creditLimit || '';
        document.getElementById('editPartnerPreferredCurrency').value = data.preferredCurrency || '';
        document.getElementById('editPartnerIsTaxExempt').value = data.isTaxExempt.toString();
        document.getElementById('editPartnerAssignedTo').value = data.assignedTo || '';
        document.getElementById('editPartnerPartnerGroupId').value = data.partnerGroupId || '';
        document.getElementById('editPartnerLastContacted').value = data.lastContacted ? data.lastContacted.split('T')[0] : '';
        document.getElementById('editPartnerNotes').value = data.notes || '';

        // Populate Sites
        const sitesContainer = document.getElementById('sites-edit-content');
        sitesContainer.innerHTML = '';
        console.log('Populating sites:', data.sites);
        (data.sites || []).forEach((site) => {
            sitesContainer.innerHTML += `
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
            `;
        });
        if (!(data.sites || []).length) {
            sitesContainer.innerHTML = '<p>Nincsenek telephelyek.</p>';
        }

        // Populate Contacts
        const contactsContainer = document.getElementById('contacts-edit-content');
        contactsContainer.innerHTML = '';
        console.log('Populating contacts:', data.contacts);
        (data.contacts || []).forEach((contact) => {
            contactsContainer.innerHTML += `
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
            `;
        });
        if (!(data.contacts || []).length) {
            contactsContainer.innerHTML = '<p>Nincsenek kapcsolattartók.</p>';
        }

        // Populate Documents
        const documentsContainer = document.getElementById('documents-edit-content');
        documentsContainer.innerHTML = '';
        console.log('Populating documents:', data.documents);
        (data.documents || []).forEach((doc) => {
            documentsContainer.innerHTML += `
                <div class="row" data-document-id="${doc.documentId || doc.id || ''}">
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Név</label>
                        <input type="text" class="form-control document-name" value="${doc.name || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Típus</label>
                        <input type="text" class="form-control document-type" value="${doc.type || ''}" readonly>
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label fw-bold">Feltöltés dátuma</label>
                        <input type="text" class="form-control document-upload-date" value="${doc.uploadDate || ''}" readonly>
                    </div>
                    <div class="col-12 text-end">
                        <button type="button" class="btn btn-warning edit-document-btn">Szerkesztés</button>
                        <button type="button" class="btn btn-danger remove-document-btn">Eltávolítás</button>
                    </div>
                </div>
                <hr class="my-4">
            `;
        });
        if (!(data.documents || []).length) {
            documentsContainer.innerHTML = '<p>Nincsenek dokumentumok.</p>';
        }

        const editModal = new bootstrap.Modal(document.getElementById('editPartnerModal'));
        editModal.show();

        // Initialize tabs after modal is shown
        setTimeout(() => {
            console.log('Initializing tabs');
            const tabTriggers = document.querySelectorAll('.nav-link');
            tabTriggers.forEach(tab => {
                tab.addEventListener('click', () => {
                    new bootstrap.Tab(tab).show();
                    console.log(`Switched to tab: ${tab.getAttribute('data-bs-target')}`);
                });
            });
            // Ensure the active tab is shown
            new bootstrap.Tab(document.querySelector('.nav-link.active')).show();
        }, 100);
    } catch (err) {
        console.error('Error loading edit partner:', err);
        alert('Hiba: ' + err.message);
    }
}

function addSite() {
    const sitesContainer = document.getElementById('sites-edit-content');
    if (!sitesContainer) {
        console.error('Sites container not found');
        return;
    }
    const siteId = `new-${Date.now()}`; // Unique ID for new site
    const newRow = document.createElement('div');
    newRow.className = 'row';
    newRow.setAttribute('data-site-id', siteId); // Set attribute directly
    newRow.innerHTML = `
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
        <div class="col-12 text-end">
            <button type="button" class="btn btn-success save-site-btn">Mentés</button>
            <button type="button" class="btn btn-secondary cancel-site-btn">Mégse</button>
        </div>
        <hr class="my-4">
    `;
    sitesContainer.appendChild(newRow);
    console.log('Added new site with data-site-id:', siteId); // Debug
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
    `;
    contactsContainer.appendChild(newRow);
    newRow.querySelector('.contact-first-name').focus();
    console.log('Added new contact with data-contact-id:', contactId);
}

function addDocument() {
    const documentsContainer = document.getElementById('documents-edit-content');
    documentsContainer.innerHTML = ''; // Clear existing content
    documentsContainer.innerHTML += `
        <div class="row">
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Név</label>
                <input type="text" class="form-control document-name" required>
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Típus</label>
                <input type="text" class="form-control document-type" required>
            </div>
            <div class="col-md-6 mb-3">
                <label class="form-label fw-bold">Feltöltés dátuma</label>
                <input type="date" class="form-control document-upload-date">
            </div>
            <div class="col-12 text-end">
                <button type="button" class="btn btn-success save-document-btn">Mentés</button>
                <button type="button" class="btn btn-secondary cancel-document-btn">Mégse</button>
            </div>
        </div>
        <hr class="my-4">
    `;
}

document.addEventListener('click', async function (event) {
    console.log('Click event detected on:', event.target); // Debug the target element
    const viewBtn = event.target.closest('.view-partner-btn');
    if (viewBtn) {
        const partnerId = viewBtn.getAttribute('data-partner-id');
        console.log('View button clicked, partnerId:', partnerId);
        if (partnerId) {
            await loadPartner(partnerId);
        } else {
            console.error('No partnerId found on view button');
            alert('Hiba: Nincs megadva partner ID');
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
            alert('Hiba: Nincs megadva partner ID a szerkesztéshez');
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
            await loadEditPartner(currentPartnerId); // Refresh
        }
    }

    if (event.target.classList.contains('remove-contact-btn')) {
        console.log('Remove Contact button clicked');
        if (confirm('Biztosan eltávolítja ezt a kapcsolattartót?')) {
            const contactId = event.target.closest('.row').getAttribute('data-contact-id');
            await deleteContact(contactId);
            await loadEditPartner(currentPartnerId); // Refresh
        }
    }

    if (event.target.classList.contains('remove-document-btn')) {
        console.log('Remove Document button clicked');
        if (confirm('Biztosan eltávolítja ezt a dokumentumot?')) {
            const documentId = event.target.closest('.row').getAttribute('data-document-id');
            await deleteDocument(documentId);
            await loadEditPartner(currentPartnerId); // Refresh
        }
    }

    if (event.target.classList.contains('edit-site-btn')) {
        console.log('Edit Site button clicked');
        const row = event.target.closest('.row');
        if (!row) {
            console.error('No row found for edit-site-btn');
            alert('Hiba: Nem található a sor a szerkesztéshez.');
            return;
        }
        const inputs = row.querySelectorAll('input');
        if (inputs.length === 0) {
            console.error('No inputs found in row for edit-site-btn');
            alert('Hiba: Nem találhatóak beviteli mezők a szerkesztéshez.');
            return;
        }
        inputs.forEach(input => {
            input.removeAttribute('readonly');
            console.log('Removed readonly from input:', input.className);
        });
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
        alert('Hiba: Nem található a sor a szerkesztéshez.');
        return;
    }
    const inputs = row.querySelectorAll('input');
    if (inputs.length === 0) {
        console.error('No inputs found in row for edit-contact-btn');
        alert('Hiba: Nem találhatóak beviteli mezők a szerkesztéshez.');
        return;
    }
    inputs.forEach(input => {
        input.removeAttribute('readonly');
        console.log('Removed readonly from input:', input.className);
        if (input.classList.contains('contact-first-name')) {
            input.focus(); // Focus on first name
        }
    });
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
            alert('Hiba: Nem található a sor a szerkesztéshez.');
            return;
        }
        const inputs = row.querySelectorAll('input');
        if (inputs.length === 0) {
            console.error('No inputs found in row for edit-document-btn');
            alert('Hiba: Nem találhatóak beviteli mezők a szerkesztéshez.');
            return;
        }
        inputs.forEach(input => {
            input.removeAttribute('readonly');
            console.log('Removed readonly from input:', input.className);
        });
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
        alert('Hiba: Nem található a sor a mentéshez.');
        return;
    }
    const siteId = row.getAttribute('data-site-id');
    if (!siteId) {
        console.error('No data-site-id attribute found on row');
        alert('Hiba: Hiányzik a site azonosító.');
        return;
    }
    const siteData = {
        partnerId: currentPartnerId,
        siteName: row.querySelector('.site-name')?.value.trim() || '',
        addressLine1: row.querySelector('.site-address')?.value.trim() || '',
        addressLine2: row.querySelector('.site-address-line-2')?.value.trim() || null,
        city: row.querySelector('.site-city')?.value.trim() || null,
        state: row.querySelector('.site-state')?.value.trim() || null,
        postalCode: row.querySelector('.site-postal-code')?.value.trim() || null,
        country: row.querySelector('.site-country')?.value.trim() || '',
        isPrimary: false
    };
    // Client-side validation
    if (!siteData.siteName || !siteData.addressLine1 || !siteData.country) {
        console.error('Required fields missing:', siteData);
        alert('Hiba: Név, Cím és Ország mezők kitöltése kötelező!');
        return;
    }
    console.log('Saving site data:', siteData);
    const url = siteId.startsWith('new-') ? `/api/Sites?partnerId=${currentPartnerId}` : `/api/Sites/${parseInt(siteId)}?partnerId=${currentPartnerId}`;
    const method = siteId.startsWith('new-') ? 'POST' : 'PUT';
    const result = await saveSite({ url, method, data: siteData });
    if (result && siteId.startsWith('new-')) {
        row.setAttribute('data-site-id', result.siteId);
    }
    setTimeout(() => {
        row.querySelectorAll('input').forEach(input => input.setAttribute('readonly', true));
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
        alert('Hiba: Nem található a sor a mentéshez.');
        return;
    }
    const contactId = row.getAttribute('data-contact-id');
    if (!contactId) {
        console.error('No data-contact-id attribute found on row');
        alert('Hiba: Hiányzik a contact azonosító.');
        return;
    }
    const contactData = {
        contactId: contactId.startsWith('new-') ? 0 : parseInt(contactId),
        firstName: row.querySelector('.contact-first-name')?.value.trim() || null,
        lastName: row.querySelector('.contact-last-name')?.value.trim() || null,
        email: row.querySelector('.contact-email')?.value.trim() || null,
        phoneNumber: row.querySelector('.contact-phone')?.value.trim() || null,
        jobTitle: row.querySelector('.contact-job-title')?.value.trim() || null,
        comment: row.querySelector('.contact-comment')?.value.trim() || null,
        isPrimary: false
    };
    console.log('Contact data before validation:', contactData);
    // Validation: Warn if firstName or lastName is empty, but allow save
    if (!contactData.firstName || !contactData.lastName) {
        console.warn('FirstName or LastName is empty, proceeding as optional');
    }
    console.log('Saving contact data:', contactData);
    const url = contactId.startsWith('new-') ? `/api/partners/${currentPartnerId}/contacts` : `/api/partners/${currentPartnerId}/contacts/${parseInt(contactId)}`;
    const method = contactId.startsWith('new-') ? 'POST' : 'PUT';
    const result = await saveContact({ url, method, data: contactData });
    if (result && contactId.startsWith('new-')) {
        row.setAttribute('data-contact-id', result.contactId);
    } else if (!result) {
        console.error('Save operation failed, no result returned');
        return;
    }
    setTimeout(() => {
        row.querySelectorAll('input').forEach(input => input.setAttribute('readonly', true));
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
            alert('Hiba: Nem található a sor a mentéshez.');
            return;
        }
        const documentId = row.getAttribute('data-document-id');
        if (!documentId) {
            console.error('No data-document-id attribute found on row');
            alert('Hiba: Hiányzik a document azonosító.');
            return;
        }
        const documentData = {
            documentId: documentId ? (documentId.startsWith('new-') ? null : parseInt(documentId)) : null,
            partnerId: currentPartnerId,
            name: row.querySelector('.document-name')?.value || '',
            type: row.querySelector('.document-type')?.value || '',
            uploadDate: row.querySelector('.document-upload-date')?.value || ''
        };
        console.log('Saving document data:', documentData);
        const result = await saveDocument(documentData);
        if (result && documentId && documentId.startsWith('new-')) {
            row.setAttribute('data-document-id', result.documentId);
        }
        setTimeout(() => {
            row.querySelectorAll('input').forEach(input => input.setAttribute('readonly', true));
            event.target.classList.remove('save-document-btn');
            event.target.classList.add('edit-document-btn');
            event.target.textContent = 'Szerkesztés';
            loadEditPartner(currentPartnerId); // Refresh after save
        }, 100); // 100ms delay
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
        return result;
    } catch (err) {
        console.error('Error saving site:', err);
        alert('Hiba: ' + err.message);
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
        console.log(`Saving document, URL: ${url}, Method: ${method}, Data:`, documentData); // Debug
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
        return result;
    } catch (err) {
        console.error('Error saving document:', err);
        alert('Hiba: ' + err.message);
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
        return true;
    } catch (err) {
        console.error('Error deleting site:', err);
        alert('Hiba: ' + err.message);
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
        return true;
    } catch (err) {
        console.error('Error deleting contact:', err);
        alert('Hiba: ' + err.message);
    }
}

async function deleteDocument(documentId) {
    try {
        const response = await fetch(`/api/Documents/${documentId}`, { method: 'DELETE' });
        if (!response.ok) throw new Error('Failed to delete document');
        console.log('Document deleted:', documentId);
    } catch (err) {
        console.error('Error deleting document:', err);
        alert('Hiba: ' + err.message);
    }
}

document.getElementById('editPartnerForm').addEventListener('submit', async function (event) {
    event.preventDefault();
    const formData = new FormData(this);
    const data = Object.fromEntries(formData.entries());
    console.log('Form data before submission:', data); // Debug full form data
    // Ensure all nullable fields are included, even if empty
    data.isTaxExempt = data.isTaxExempt === 'true';
    data.creditLimit = parseFloat(data.creditLimit) || null;
    data.lastContacted = data.lastContacted ? new Date(data.lastContacted).toISOString() : null;
    data.partnerGroupId = data.partnerGroupId || null; // Explicitly handle nullable field
    data.UpdatedBy = "System";
    data.UpdatedDate = new Date().toISOString();

    try {
        const response = await fetch(`/api/Partners/${currentPartnerId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        console.log('Update response status:', response.status, 'Status text:', response.statusText);
        if (!response.ok) {
            const errorText = await response.text();
            console.error('Update response error:', errorText);
            throw new Error(`Failed to update partner: ${errorText || response.statusText}`);
        }
        alert('Partner sikeresen frissítve!');
        const modal = bootstrap.Modal.getInstance(document.getElementById('editPartnerModal'));
        modal.hide();
        await loadPartner(currentPartnerId); // Refresh view modal
    } catch (err) {
        console.error('Error updating partner:', err);
        alert('Hiba: ' + err.message);
    }
});
});