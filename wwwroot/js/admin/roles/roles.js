document.addEventListener('DOMContentLoaded', function() {
    const roleModal = new bootstrap.Modal('#roleModal');
    let currentAction = '';
    
    // Edit button handler
    document.querySelectorAll('.edit-role').forEach(btn => {
        btn.addEventListener('click', function() {
            currentAction = 'update';
            document.getElementById('modalTitle').textContent = 'Edit Role';
            document.getElementById('roleId').value = this.dataset.roleId;
            document.getElementById('roleName').value = this.dataset.roleName;
            document.getElementById('confirmAction').className = 'btn btn-primary';
            document.getElementById('confirmAction').textContent = 'Update';
            roleModal.show();
        });
    });
    
    // Delete button handler
    document.querySelectorAll('.delete-role').forEach(btn => {
        btn.addEventListener('click', function() {
            currentAction = 'delete';
            document.getElementById('modalTitle').textContent = 'Delete Role';
            document.getElementById('roleId').value = this.dataset.roleId;
            document.getElementById('confirmAction').className = 'btn btn-danger';
            document.getElementById('confirmAction').textContent = 'Delete';
            roleModal.show();
        });
    });
    
    // Confirm action handler
    document.getElementById('confirmAction').addEventListener('click', async function() {
        const roleId = document.getElementById('roleId').value;
        const roleName = document.getElementById('roleName').value;
        
        try {
            let url, data;
            
            if (currentAction === 'update') {
                url = '/Admin/Roles?handler=UpdateRole';
                data = { id: roleId, name: roleName };
            } else {
                url = '/Admin/Roles?handler=DeleteRole';
                data = { id: roleId };
            }
            
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(data)
            });
            
            if (!response.ok) throw new Error('Action failed');
            
            window.location.reload();
            
        } catch (error) {
            console.error('Error:', error);
            alert('Action failed: ' + error.message);
        } finally {
            roleModal.hide();
        }
    });
});