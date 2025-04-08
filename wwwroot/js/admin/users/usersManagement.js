document.addEventListener('DOMContentLoaded', function() {
    console.log('Users management script loaded');

    // Add User Modal
    const addUserModalElement = document.getElementById('addUserModal');
    if (!addUserModalElement) {
        console.error('Add user modal element not found');
        return;
    }
    const addUserModal = new bootstrap.Modal(addUserModalElement);
    const addUserForm = document.getElementById('addUserForm');
    if (!addUserForm) {
        console.error('Add user form not found');
        return;
    }

    addUserModalElement.addEventListener('hidden.bs.modal', function() {
        addUserForm.reset();
        addUserForm.classList.remove('was-validated');
    });

    addUserForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        console.log('Add user form submitted');

        addUserForm.classList.add('was-validated');
        if (!addUserForm.checkValidity()) {
            console.log('Add user form validation failed');
            return;
        }

        const saveUserBtn = document.getElementById('saveUserBtn');
        const getValue = (id) => {
            const element = document.getElementById(id);
            if (!element) {
                console.error(`Element with ID '${id}' not found`);
                return '';
            }
            console.log(`Value for ${id}:`, element.value);
            return element.value && typeof element.value === 'string' ? element.value.trim() : '';
        };

        const formData = {
            UserName: getValue('userName'),
            Email: getValue('modalEmail'),
            Password: getValue('modalPassword'),
            PhoneNumber: getValue('phoneNumber') || null
        };

        console.log('Add user form data:', formData);

        if (!formData.UserName || !formData.Email || !formData.Password) {
            console.log('Required fields missing:', formData);
            return;
        }

        try {
            saveUserBtn.disabled = true;
            saveUserBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';

            const response = await fetch('/Admin/Users?handler=AddUser', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ userDto: formData })
            });

            const result = await response.json();
            if (!response.ok) {
                const errorMessage = result.errors 
                    ? result.errors.map(e => `${e.Field || ''}: ${e.Message}`).join('\n') 
                    : result.error || 'Failed to create user';
                throw new Error(errorMessage);
            }

            addUserModal.hide();
            setTimeout(() => window.location.reload(), 500);
        } catch (error) {
            console.error('Error creating user:', error);
            alert('Error creating user: ' + error.message);
        } finally {
            saveUserBtn.disabled = false;
            saveUserBtn.innerHTML = 'Save';
        }
    });

    // Force Password Change
    document.querySelectorAll('.change-password').forEach(button => {
        button.addEventListener('click', async function() {
            const userId = this.getAttribute('data-user-id');
            console.log(`Forcing password change for user ID: ${userId}`);

            try {
                const response = await fetch(`/Admin/Users?handler=ForcePasswordChange&id=${userId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
                    }
                });

                const result = await response.json();
                if (!response.ok) {
                    throw new Error(result.error || 'Failed to force password change');
                }

                alert(result.message);
            } catch (error) {
                console.error('Error forcing password change:', error);
                alert('Error: ' + error.message);
            }
        });
    });

    // Edit User Modal
    const editUserModalElement = document.getElementById('editUserModal');
    if (!editUserModalElement) {
        console.error('Edit user modal element not found');
        return;
    }
    const editUserModal = new bootstrap.Modal(editUserModalElement);
    const editUserForm = document.getElementById('editUserForm');
    if (!editUserForm) {
        console.error('Edit user form not found');
        return;
    }

    editUserModalElement.addEventListener('hidden.bs.modal', function() {
        console.log('Edit modal hidden');
        editUserForm.reset();
        editUserForm.classList.remove('was-validated');
    });

    document.querySelectorAll('.edit-user').forEach(button => {
        button.addEventListener('click', function() {
            console.log('Edit button clicked for user ID:', this.getAttribute('data-user-id'));
            const userId = this.getAttribute('data-user-id');
            const row = this.closest('tr');
            const userName = row.cells[1].textContent;
            const email = row.cells[2].textContent;
            const phoneNumber = row.cells[3].textContent;
            const accessFailedCount = row.cells[4].textContent;
            const disabled = row.cells[5].textContent === 'Yes';

            document.getElementById('editUserId').value = userId;
            document.getElementById('editUserName').value = userName;
            document.getElementById('editEmail').value = email;
            document.getElementById('editPhoneNumber').value = phoneNumber;
            document.getElementById('editAccessFailedCount').value = accessFailedCount;
            document.getElementById('editDisabled').checked = disabled;

            console.log('Showing edit modal');
            editUserModal.show();
        });
    });

    document.getElementById('saveEditUserBtn').addEventListener('click', async function() {
        console.log('Save edit button clicked');
        editUserForm.classList.add('was-validated');
        if (!editUserForm.checkValidity()) {
            console.log('Edit form validation failed');
            return;
        }

        const formData = {
            Id: document.getElementById('editUserId').value,
            UserName: document.getElementById('editUserName').value.trim(),
            Email: document.getElementById('editEmail').value.trim(),
            PhoneNumber: document.getElementById('editPhoneNumber').value.trim() || null,
            AccessFailedCount: parseInt(document.getElementById('editAccessFailedCount').value) || 0,
            Disabled: document.getElementById('editDisabled').checked
        };

        console.log('Edit form data:', formData);

        try {
            this.disabled = true;
            this.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';

            const response = await fetch('/Admin/Users?handler=EditUser', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();
            if (!response.ok) {
                const errorMessage = result.errors 
                    ? result.errors.map(e => `${e.Field}: ${e.Message}`).join('\n') 
                    : result.error || 'Failed to save user';
                throw new Error(errorMessage);
            }

            console.log('Edit successful, updating row');
            const row = document.querySelector(`tr[data-user-id="${formData.Id}"]`);
            row.cells[1].textContent = result.user.userName;
            row.cells[2].textContent = result.user.email;
            row.cells[3].textContent = result.user.phoneNumber || '';
            row.cells[4].textContent = result.user.accessFailedCount;
            row.cells[5].textContent = result.user.disabled ? 'Yes' : 'No';

            editUserModal.hide();
        } catch (error) {
            console.error('Error saving user:', error);
            alert('Error: ' + error.message);
        } finally {
            this.disabled = false;
            this.innerHTML = 'Save changes';
        }
    });

    // Real-time input validation for add user form
    addUserForm.querySelectorAll('input').forEach(input => {
        input.addEventListener('input', function() {
            if (this.checkValidity()) {
                this.classList.remove('is-invalid');
                this.classList.add('is-valid');
            } else {
                this.classList.remove('is-valid');
                this.classList.add('is-invalid');
            }
        });
    });
});