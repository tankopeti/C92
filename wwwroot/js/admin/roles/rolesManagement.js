document.addEventListener('DOMContentLoaded', function() {
    // Initialize modals
    const addRoleModal = new bootstrap.Modal('#addRoleModal');
    const editRoleModal = new bootstrap.Modal('#editRoleModal');

    // Add Role Handler
    document.getElementById('saveRoleBtn').addEventListener('click', async function() {
        const roleName = document.getElementById('roleName').value.trim();
        const form = document.getElementById('addRoleForm');
        
        if (!roleName) {
            form.classList.add('was-validated');
            return;
        }

        try {
            const response = await fetch('/Admin/Roles?handler=AddRole', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ roleName })
            });
            
            if (response.ok) {
                const newRole = await response.json();
                addRoleToTable(newRole);
                addRoleModal.hide();
                form.reset();
            } else {
                throw new Error('Failed to add role');
            }
        } catch (error) {
            console.error('Error:', error);
            showAlert('addRoleModal', error.message);
        }
    });

    // Edit Role - Setup Modal
    document.addEventListener('click', function(e) {
        const editBtn = e.target.closest('.edit-role');
        if (editBtn) {
            const row = editBtn.closest('tr');
            document.getElementById('editRoleId').value = editBtn.dataset.roleId;
            document.getElementById('editRoleName').value = row.cells[1].textContent;
            editRoleModal.show();
        }
    });

    // Update Role Handler
    document.getElementById('updateRoleBtn').addEventListener('click', async function() {
        const roleId = document.getElementById('editRoleId').value;
        const roleName = document.getElementById('editRoleName').value.trim();
        
        if (!roleName) {
            document.getElementById('editRoleForm').classList.add('was-validated');
            return;
        }
    
        try {
            // 1. Corrected URL format
            const response = await fetch('/Admin/Roles/Index?handler=UpdateRole', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                // 2. Properly formatted body
                body: JSON.stringify({
                    id: roleId,
                    name: roleName
                })
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            // 3. Update the UI
            const row = document.querySelector(`tr[data-role-id="${roleId}"]`);
            if (row) {
                row.cells[1].textContent = roleName;
                row.cells[2].textContent = roleName.toUpperCase();
            }
            
            // 4. Close modal
            bootstrap.Modal.getInstance(document.getElementById('editRoleModal')).hide();
            
        } catch (error) {
            console.error('Update failed:', error);
            // 5. Show error to user
            const errorAlert = document.getElementById('editRoleModal').querySelector('.alert');
            errorAlert.textContent = `Update failed: ${error.message}`;
            errorAlert.classList.remove('d-none');
        }
    });
    

    // Delete handler with event delegation
    document.addEventListener('click', function(e) {
        const deleteBtn = e.target.closest('.delete-role');
        if (deleteBtn) {
            const roleId = deleteBtn.dataset.roleId;
            if (confirm('Are you sure you want to delete this role?')) {
                fetch(`/Admin/Roles/Index?handler=DeleteRole&id=${roleId}`, {
                    method: 'DELETE',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                })
                .then(response => {
                    if (response.ok) {
                        document.querySelector(`tr[data-role-id="${roleId}"]`)?.remove();
                    } else {
                        throw new Error('Failed to delete role');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert(error.message);
                });
            }
        }
    });

    // Helper Functions
    function addRoleToTable(role) {
        const tbody = document.querySelector('#rolesTable tbody');
        const row = tbody.insertRow();
        row.dataset.roleId = role.id;
        row.innerHTML = `
            <td class="ps-3">${role.id}</td>
            <td>${role.name}</td>
            <td>${role.normalizedName}</td>
            <td class="pe-3 text-end">
                <div class="btn-group btn-group-sm">
                    <button class="btn btn-outline-primary edit-role" data-role-id="${role.id}">
                        <i class="bi bi-pencil-square"></i>
                    </button>
                    <button class="btn btn-outline-danger delete-role" data-role-id="${role.id}">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </td>
        `;
    }

    function showAlert(modalId, message) {
        const modal = document.getElementById(modalId);
        const alert = modal.querySelector('.alert');
        alert.textContent = message;
        alert.classList.remove('d-none');
        setTimeout(() => alert.classList.add('d-none'), 5000);
    }
});