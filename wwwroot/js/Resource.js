// // wwwroot/js/Resources.js
// window.Resources = {
//     edit: function(id) {
//         new bootstrap.Modal(document.getElementById('editResourceModal_' + id)).show();
//     },
//     view: function(id) {
//         new bootstrap.Modal(document.getElementById('viewResourceModal_' + id)).show();
//         // Load history
//         fetch(`/api/resources/${id}/history`)
//             .then(r => r.json())
//             .then(data => {
//                 const container = document.getElementById('historyContainer_' + id);
//                 if (!data.length) {
//                     container.innerHTML = '<p class="text-muted">Nincs előzmény.</p>';
//                     return;
//                 }
//                 const rows = data.map(h => `
//                     <tr>
//                         <td>${new Date(h.ModifiedDate).toLocaleString('hu-HU')}</td>
//                         <td>${h.ModifiedByName}</td>
//                         <td>${h.ChangeDescription}</td>
//                         <td>${h.ServicePrice?.toFixed(2) || '-'}</td>
//                     </tr>
//                 `).join('');
//                 container.innerHTML = `
//                     <div class="table-responsive">
//                         <table class="table table-sm">
//                             <thead><tr><th>Dátum</th><th>Felhasználó</th><th>Leírás</th><th>Ár</th></tr></thead>
//                             <tbody>${rows}</tbody>
//                         </table>
//                     </div>
//                 `;
//             });
//     },
//     showHistory: function(id) {
//         this.view(id);
//     },
//     deactivate: function(form, id, name) {
//         return confirm(`Deaktiválod: ${name}?`);
//     }
// };