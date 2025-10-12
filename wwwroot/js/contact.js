
document.addEventListener('DOMContentLoaded', function() {
    console.log('contacts.js loaded');

    // Initialize TomSelect for partner selects in modals
    function initTomSelect(selectId) {
        const select = document.getElementById(selectId);
        if (!select) {
            console.warn(`Select element not found: ${selectId}`);
            return;
        }
        if (select.tomselect) {
            console.log(`TomSelect already initialized for ${selectId}`);
            return;
        }
        if (typeof TomSelect === 'undefined') {
            console.error('TomSelect not loaded - check CDN');
            return;
        }

        try {
            new TomSelect(select, {
                plugins: ['clear_button', 'dropdown_header'],
                maxOptions: 100,
                searchField: ['text'],
                placeholder: 'Partner keresése...',
                render: {
                    option: function(data, escape) {
                        return `<div>${escape(data.text)}</div>`;
                    },
                    item: function(data, escape) {
                        return `<div>${escape(data.text)}</div>`;
                    }
                },
                onInitialize: function() {
                    console.log(`TomSelect initialized for ${selectId}`);
                }
            });
        } catch (err) {
            console.error(`TomSelect init failed for ${selectId}:`, err);
        }
    }

    // For create modal
    var createModal = document.getElementById('createContactModal');
    if (createModal) {
        console.log('Create modal found');
        createModal.addEventListener('shown.bs.modal', function () {
            console.log('Create modal shown, initializing TomSelect');
            initTomSelect('createPartnerId');
        });
    } else {
        console.warn('Create modal not found in DOM');
    }

    // For edit modal
    var editModal = document.getElementById('editContactModal');
    if (editModal) {
        console.log('Edit modal found');
        editModal.addEventListener('shown.bs.modal', function () {
            console.log('Edit modal shown, initializing TomSelect');
            initTomSelect('editPartnerId');
        });

        editModal.addEventListener('show.bs.modal', function (event) {
            var button = event.relatedTarget;
            if (!button) return;
            var id = button.getAttribute('data-contact-id');
            document.getElementById('editContactId').value = id || '';
            document.getElementById('editFirstName').value = button.getAttribute('data-first-name') || '';
            document.getElementById('editLastName').value = button.getAttribute('data-last-name') || '';
            document.getElementById('editEmail').value = button.getAttribute('data-email') || '';
            document.getElementById('editPhoneNumber').value = button.getAttribute('data-phone-number') || '';
            document.getElementById('editPhoneNumber2').value = button.getAttribute('data-phone-number2') || '';
            document.getElementById('editJobTitle').value = button.getAttribute('data-job-title') || '';
            document.getElementById('editComment').value = button.getAttribute('data-comment') || '';
            document.getElementById('editComment2').value = button.getAttribute('data-comment2') || '';
            document.getElementById('editStatusId').value = button.getAttribute('data-status-id') || '';
            document.getElementById('editPartnerId').value = button.getAttribute('data-partner-id') || '';
            document.getElementById('editIsPrimary').checked = button.getAttribute('data-is-primary') === 'True';
            document.getElementById('editPartnerId').dispatchEvent(new Event('change'));
        });
    } else {
        console.warn('Edit modal not found in DOM');
    }

    // For delete modal
    var deleteModal = document.getElementById('deleteContactModal');
    if (deleteModal) {
        console.log('Delete modal found');
        deleteModal.addEventListener('show.bs.modal', function (event) {
            var button = event.relatedTarget;
            if (!button) {
                console.error('No related target for delete modal');
                return;
            }
            var id = button.getAttribute('data-contact-id');
            if (!id || isNaN(id)) {
                console.error('Invalid or missing contact ID for delete modal:', id);
                return;
            }
            document.getElementById('deleteContactId').value = id;
            console.log('Delete modal opened with contact ID:', id);
        });
    } else {
        console.warn('Delete modal not found in DOM');
    }

    // Handle create form submit via AJAX to API
    var createForm = document.getElementById('createContactModal').querySelector('form');
    if (createForm) {
        console.log('Create form found - adding submit listener');
        createForm.addEventListener('submit', async function (event) {
            console.log('Create form submit triggered');
            event.preventDefault();

            const formData = new FormData(this);
            const data = {
                firstName: formData.get('firstName') || null,
                lastName: formData.get('lastName') || null,
                email: formData.get('email') || null,
                phoneNumber: formData.get('phoneNumber') || null,
                phoneNumber2: formData.get('phoneNumber2') || null,
                jobTitle: formData.get('jobTitle') || null,
                comment: formData.get('comment') || null,
                comment2: formData.get('comment2') || null,
                isPrimary: formData.get('isPrimary') === 'true' || false,
                statusId: formData.get('statusId') ? parseInt(formData.get('statusId')) : null,
                partnerId: formData.get('partnerId') ? parseInt(formData.get('partnerId')) : null,
                createdDate: new Date().toISOString(),
                updatedDate: null
            };

            console.log('JSON data:', data);

            const submitBtn = this.querySelector('button[type="submit"], .btn-primary[type="submit"]');
            if (!submitBtn) {
                console.error('Submit button not found in form');
                alert('Hiba: Submit gomb nem található.');
                return;
            }
            submitBtn.disabled = true;
            submitBtn.textContent = 'Mentés...';

            try {
                console.log('Sending POST request to /api/Contact');
                const response = await fetch('/api/Contact', {
                    method: 'POST',
                    body: JSON.stringify(data),
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    credentials: 'include' // Send authentication cookie
                });
                console.log('Response received:', response.status, response.statusText);

                if (!response.ok) {
                    const contentType = response.headers.get('content-type');
                    if (contentType && contentType.includes('application/json')) {
                        const result = await response.json();
                        console.error('API error:', result);
                        alert('Hiba: ' + (result.message || 'Kontakt létrehozása sikertelen.'));
                    } else {
                        const text = await response.text();
                        console.error('Non-JSON response:', text);
                        alert('Hiba: A szerver nem JSON választ küldött. Status: ' + response.status);
                    }
                    return;
                }

                const result = await response.json();
                console.log('JSON result:', result);

                if (response.ok) {
                    console.log('Success - closing modal and reloading');
                    const modal = bootstrap.Modal.getInstance(document.getElementById('createContactModal'));
                    modal.hide();
                    location.reload();
                }
            } catch (err) {
                console.error('Fetch error:', err);
                alert('Hálózati hiba: ' + err.message);
            } finally {
                submitBtn.disabled = false;
                submitBtn.textContent = 'Mentés';
            }
        });
    } else {
        console.warn('Create form not found');
    }

    // Handle edit form submit via AJAX to API
    var editForm = document.getElementById('editContactModal').querySelector('form');
    if (editForm) {
        console.log('Edit form found - adding submit listener');
        editForm.addEventListener('submit', async function (event) {
            console.log('Edit form submit triggered');
            event.preventDefault();

            const formData = new FormData(this);
            const id = document.getElementById('editContactId').value;
            if (!id || isNaN(id)) {
                console.error('Invalid contact ID:', id);
                alert('Hiba: Érvénytelen kontakt azonosító.');
                submitBtn.disabled = false;
                submitBtn.textContent = 'Frissítés';
                return;
            }

            const data = {
                firstName: formData.get('firstName') || null,
                lastName: formData.get('lastName') || null,
                email: formData.get('email') || null,
                phoneNumber: formData.get('phoneNumber') || null,
                phoneNumber2: formData.get('phoneNumber2') || null,
                jobTitle: formData.get('jobTitle') || null,
                comment: formData.get('comment') || null,
                comment2: formData.get('comment2') || null,
                isPrimary: formData.get('isPrimary') === 'true' || false,
                statusId: formData.get('statusId') ? parseInt(formData.get('statusId')) : null,
                partnerId: formData.get('partnerId') ? parseInt(formData.get('partnerId')) : null,
                updatedDate: new Date().toISOString()
            };

            console.log('JSON data:', data);

            const submitBtn = this.querySelector('button[type="submit"], .btn-primary[type="submit"]');
            if (!submitBtn) {
                console.error('Submit button not found in edit form');
                alert('Hiba: Submit gomb nem található.');
                return;
            }
            submitBtn.disabled = true;
            submitBtn.textContent = 'Frissítés...';

            try {
                console.log(`Sending PUT request to /api/Contact/${id}`);
                const response = await fetch(`/api/Contact/${id}`, {
                    method: 'PUT',
                    body: JSON.stringify(data),
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    credentials: 'include' // Send authentication cookie
                });
                console.log('Edit response:', response.status, response.statusText);

                if (!response.ok) {
                    const contentType = response.headers.get('content-type');
                    if (contentType && contentType.includes('application/json')) {
                        const result = await response.json();
                        console.error('API error:', result);
                        alert('Hiba: ' + (result.message || 'Kontakt frissítése sikertelen.'));
                    } else {
                        const text = await response.text();
                        console.error('Non-JSON response:', text);
                        alert('Hiba: A szerver nem JSON választ küldött. Status: ' + response.status);
                    }
                    return;
                }

                const result = await response.json();
                console.log('Edit JSON result:', result);

                if (response.ok) {
                    const modal = bootstrap.Modal.getInstance(document.getElementById('editContactModal'));
                    modal.hide();
                    location.reload();
                }
            } catch (err) {
                console.error('Fetch error:', err);
                alert('Hálózati hiba: ' + err.message);
            } finally {
                submitBtn.disabled = false;
                submitBtn.textContent = 'Frissítés';
            }
        });
    } else {
        console.warn('Edit form not found');
    }

    // Handle delete form submit via AJAX to API
    var deleteForm = document.getElementById('deleteContactModal').querySelector('form');
    if (deleteForm) {
        console.log('Delete form found - adding submit listener');
        deleteForm.addEventListener('submit', async function (event) {
            console.log('Delete form submit triggered');
            event.preventDefault();

            const id = document.getElementById('deleteContactId').value;
            if (!id || isNaN(id)) {
                console.error('Invalid contact ID:', id);
                alert('Hiba: Érvénytelen kontakt azonosító.');
                submitBtn.disabled = false;
                submitBtn.textContent = 'Törlés';
                return;
            }

            const submitBtn = this.querySelector('button[type="submit"], .btn-danger[type="submit"]');
            if (!submitBtn) {
                console.error('Submit button not found in delete form');
                alert('Hiba: Submit gomb nem található.');
                return;
            }
            submitBtn.disabled = true;
            submitBtn.textContent = 'Törlés...';

            try {
                console.log(`Sending DELETE request to /api/Contact/${id}`);
                const response = await fetch(`/api/Contact/${id}`, {
                    method: 'DELETE',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    credentials: 'include' // Send authentication cookie
                });
                console.log('Delete response:', response.status, response.statusText);

                if (!response.ok) {
                    const contentType = response.headers.get('content-type');
                    if (contentType && contentType.includes('application/json')) {
                        const result = await response.json();
                        console.error('API error:', result);
                        alert('Hiba: ' + (result.message || 'Kontakt törlése sikertelen.'));
                    } else {
                        const text = await response.text();
                        console.error('Non-JSON response:', text);
                        alert('Hiba: A szerver nem JSON választ küldött. Status: ' + response.status);
                    }
                    return;
                }

                const result = await response.json();
                console.log('Delete JSON result:', result);

                if (response.ok) {
                    const modal = bootstrap.Modal.getInstance(document.getElementById('deleteContactModal'));
                    modal.hide();
                    location.reload();
                }
            } catch (err) {
                console.error('Fetch error:', err);
                alert('Hálózati hiba: ' + err.message);
            } finally {
                submitBtn.disabled = false;
                submitBtn.textContent = 'Törlés';
            }
        });
    } else {
        console.warn('Delete form not found');
    }

    // Handle view modal population
    var viewModal = document.getElementById('viewContactModal');
    if (viewModal) {
        console.log('View modal found');
        viewModal.addEventListener('show.bs.modal', function (event) {
            var button = event.relatedTarget;
            if (!button) return;

            var id = button.getAttribute('data-contact-id');
            if (!id) {
                console.warn('No contact ID found for view modal');
                return;
            }

            document.getElementById('viewFirstName').textContent = button.getAttribute('data-first-name') || '';
            document.getElementById('viewLastName').textContent = button.getAttribute('data-last-name') || '';
            document.getElementById('viewEmail').textContent = button.getAttribute('data-email') || '';
            document.getElementById('viewPhoneNumber').textContent = button.getAttribute('data-phone-number') || '';
            document.getElementById('viewPhoneNumber2').textContent = button.getAttribute('data-phone-number2') || '';
            document.getElementById('viewJobTitle').textContent = button.getAttribute('data-job-title') || '';
            document.getElementById('viewComment').textContent = button.getAttribute('data-comment') || '';
            document.getElementById('viewComment2').textContent = button.getAttribute('data-comment2') || '';
            document.getElementById('viewStatusName').textContent = button.getAttribute('data-status-name') || 'N/A';
            document.getElementById('viewPartnerName').textContent = button.getAttribute('data-partner-name') || 'Nincs hozzárendelve';
            var isPrimary = button.getAttribute('data-is-primary') === 'True';
            document.getElementById('viewIsPrimary').textContent = isPrimary ? 'Igen' : 'Nem';
            document.getElementById('viewDetailsLink').href = `/CRM/Contacts/Details?id=${id}`;
            document.getElementById('viewContactLabel').textContent = `Kontakt részletei: ${button.getAttribute('data-first-name') || ''} ${button.getAttribute('data-last-name') || ''}`;
        });
    } else {
        console.warn('View modal not found in DOM');
    }

    // Form validation (client-side)
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function (event) {
            if (!this.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            this.classList.add('was-validated');
        }, false);
    });

    // Global functions for history (if needed)
    window.loadHistory = function(id) {
        console.log('Loading history for ' + id);
    };
    window.showHistoryTab = function(id) {
        // Implement tab switch
    };
});