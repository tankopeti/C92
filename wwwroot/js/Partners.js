// // /js/partner.js – Teljes View minden kapcsolódó adattal + linkekkel
// document.addEventListener('DOMContentLoaded', function () {
//     console.log('partner.js BETÖLTÖDÖTT – kész a teljes megtekintésre');

//     document.addEventListener('click', async function (e) {
//         const viewBtn = e.target.closest('.view-partner-btn');
//         if (!viewBtn) return;

//         const partnerId = viewBtn.dataset.partnerId;
//         if (!partnerId) {
//             window.c92.showToast('error', 'Hiba: Partner ID hiányzik');
//             return;
//         }

//         console.log(`Megtekintés – Partner ID: ${partnerId}`);

//         const modalEl = document.getElementById('viewPartnerModal');
//         const content = document.getElementById('viewPartnerContent');
//         if (!modalEl || !content) return;

//         const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
//         modal.show();

//         content.innerHTML = `
//             <div class="text-center py-5">
//                 <div class="spinner-border text-primary" role="status">
//                     <span class="visually-hidden">Betöltés...</span>
//                 </div>
//                 <p class="mt-3">Adatok betöltése...</p>
//             </div>
//         `;

//         try {
//             const response = await fetch(`/api/Partners/${partnerId}`);
//             if (!response.ok) throw new Error('Partner nem található vagy inaktív');

//             const data = await response.json();

//             content.innerHTML = `
//                 <div class="container-fluid">
//                     <div class="text-center mb-4">
//                         <h4 class="mb-3">${data.name || 'Névtelen partner'}</h4>
//                         <p class="text-muted">Partner ID: ${data.partnerId}</p>
//                     </div>

//                     <div class="row g-3 mb-4">
//                         <div class="col-md-6"><strong>Cégnév:</strong> ${data.companyName || '—'}</div>
//                         <div class="col-md-6"><strong>E-mail:</strong> ${data.email || '—'}</div>
//                         <div class="col-md-6"><strong>Telefonszám:</strong> ${data.phoneNumber || '—'}</div>
//                         <div class="col-md-6"><strong>Másodlagos telefon:</strong> ${data.alternatePhone || '—'}</div>
//                         <div class="col-md-6"><strong>Weboldal:</strong> ${data.website ? `<a href="${data.website}" target="_blank">${data.website}</a>` : '—'}</div>
//                         <div class="col-md-6"><strong>Adószám:</strong> ${data.taxId || '—'}</div>
//                         <div class="col-md-6"><strong>Nemzetközi adószám:</strong> ${data.intTaxId || '—'}</div>
//                         <div class="col-md-6"><strong>Iparág:</strong> ${data.industry || '—'}</div>
//                     </div>

//                     <hr class="my-4">

//                     <h5>Cím adatok</h5>
//                     <div class="row g-3 mb-4">
//                         <div class="col-md-6"><strong>Utca, házszám:</strong> ${data.addressLine1 || '—'}</div>
//                         <div class="col-md-6"><strong>Kiegészítő cím:</strong> ${data.addressLine2 || '—'}</div>
//                         <div class="col-md-4"><strong>Város:</strong> ${data.city || '—'}</div>
//                         <div class="col-md-4"><strong>Megye:</strong> ${data.state || '—'}</div>
//                         <div class="col-md-4"><strong>Irányítószám:</strong> ${data.postalCode || '—'}</div>
//                         <div class="col-md-6"><strong>Ország:</strong> ${data.country || '—'}</div>
//                     </div>

//                     <hr class="my-4">

//                     <h5>Számlázási adatok</h5>
//                     <div class="row g-3 mb-4">
//                         <div class="col-md-6"><strong>Számlázási kapcsolattartó:</strong> ${data.billingContactName || '—'}</div>
//                         <div class="col-md-6"><strong>Számlázási e-mail:</strong> ${data.billingEmail || '—'}</div>
//                         <div class="col-md-6"><strong>Fizetési feltételek:</strong> ${data.paymentTerms || '—'}</div>
//                         <div class="col-md-6"><strong>Kredit limit:</strong> ${data.creditLimit ? data.creditLimit + ' ' + (data.preferredCurrency || '') : '—'}</div>
//                         <div class="col-md-6"><strong>Előnyben részesített valuta:</strong> ${data.preferredCurrency || '—'}</div>
//                         <div class="col-md-6"><strong>Adómentesség:</strong> ${data.isTaxExempt ? 'Igen' : 'Nem'}</div>
//                     </div>

//                     <hr class="my-4">

//                     <h5>További adatok</h5>
//                     <div class="row g-3 mb-4">
//                         <div class="col-md-6"><strong>Értékesítő:</strong> ${data.assignedTo || '—'}</div>
//                         <div class="col-md-6"><strong>Utolsó kapcsolatfelvétel:</strong> ${data.lastContacted ? new Date(data.lastContacted).toLocaleDateString('hu-HU') : '—'}</div>
//                         <div class="col-md-6"><strong>Státusz:</strong> <span class="badge bg-secondary">${data.status?.name || 'N/A'}</span></div>
//                     </div>

//                     <hr class="my-4">

//                     <h5>Jegyzetek</h5>
//                     <p>${data.notes || 'Nincsenek jegyzetek.'}</p>

//                     <!-- Telephelyek -->
//                     <hr class="my-4">
//                     <h5>Telephelyek (${data.sites?.length || 0})</h5>
//                     <div class="row g-3">
//                         ${data.sites?.length > 0 ? data.sites.map(s => `
//                             <div class="col-md-6">
//                                 <div class="card mb-3">
//                                     <div class="card-body">
//                                         <h6 class="card-title">${s.siteName || 'Névtelen telephely'} ${s.isPrimary ? '<span class="badge bg-success">Elsődleges</span>' : ''}</h6>
//                                         <p class="card-text mb-1">${s.addressLine1 || ''} ${s.addressLine2 || ''}</p>
//                                         <p class="card-text mb-0">${s.postalCode || ''} ${s.city || ''}, ${s.state || ''}, ${s.country || ''}</p>
//                                     </div>
//                                 </div>
//                             </div>
//                         `).join('') : '<p class="text-muted">Nincsenek telephelyek.</p>'}
//                     </div>

//                     <!-- Kapcsolattartók -->
//                     <hr class="my-4">
//                     <h5>Kapcsolattartók (${data.contacts?.length || 0})</h5>
//                     <div class="row g-3">
//                         ${data.contacts?.length > 0 ? data.contacts.map(c => `
//                             <div class="col-md-6">
//                                 <div class="card mb-3">
//                                     <div class="card-body">
//                                         <h6 class="card-title">${c.firstName} ${c.lastName || ''} ${c.isPrimary ? '<span class="badge bg-primary">Elsődleges</span>' : ''}</h6>
//                                         <p class="card-text mb-1"><strong>E-mail:</strong> ${c.email || '—'}</p>
//                                         <p class="card-text mb-1"><strong>Telefon:</strong> ${c.phoneNumber || '—'}</p>
//                                         <p class="card-text mb-0"><strong>Szerepkör:</strong> ${c.jobTitle || '—'}</p>
//                                     </div>
//                                 </div>
//                             </div>
//                         `).join('') : '<p class="text-muted">Nincsenek kapcsolattartók.</p>'}
//                     </div>

//                     <!-- Árajánlatok -->
//                     <hr class="my-4">
//                     <h5>Árajánlatok (${data.quotes?.length || 0})</h5>
//                     <div class="row g-3">
//                         ${data.quotes?.length > 0 ? data.quotes.map(q => `
//                             <div class="col-md-6">
//                                 <div class="card mb-3">
//                                     <div class="card-body">
//                                         <h6 class="card-title">Árajánlat #${q.quoteId || 'N/A'}</h6>
//                                         <p class="card-text mb-1"><strong>Dátum:</strong> ${q.quoteDate ? new Date(q.quoteDate).toLocaleDateString('hu-HU') : '—'}</p>
//                                         <p class="card-text mb-1"><strong>Összeg:</strong> ${q.totalAmount || '—'} ${q.currency || ''}</p>
//                                         <p class="card-text mb-0"><strong>Státusz:</strong> ${q.status || '—'}</p>
//                                     </div>
//                                 </div>
//                             </div>
//                         `).join('') : '<p class="text-muted">Nincsenek árajánlatok.</p>'}
//                     </div>

//                     <!-- Megrendelések -->
//                     <hr class="my-4">
//                     <h5>Megrendelések (${data.orders?.length || 0})</h5>
//                     <div class="row g-3">
//                         ${data.orders?.length > 0 ? data.orders.map(o => `
//                             <div class="col-md-6">
//                                 <div class="card mb-3">
//                                     <div class="card-body">
//                                         <h6 class="card-title">Megrendelés #${o.orderId || 'N/A'}</h6>
//                                         <p class="card-text mb-1"><strong>Dátum:</strong> ${o.orderDate ? new Date(o.orderDate).toLocaleDateString('hu-HU') : '—'}</p>
//                                         <p class="card-text mb-1"><strong>Összeg:</strong> ${o.totalAmount || '—'} ${o.currency || ''}</p>
//                                         <p class="card-text mb-0"><strong>Státusz:</strong> ${o.status || '—'}</p>
//                                     </div>
//                                 </div>
//                             </div>
//                         `).join('') : '<p class="text-muted">Nincsenek megrendelések.</p>'}
//                     </div>

//                     <!-- Dokumentumok -->
//                     <hr class="my-4">
//                     <h5>Dokumentumok (${data.documents?.length || 0})</h5>
//                     <div class="row g-3">
//                         ${data.documents?.length > 0 ? data.documents.map(d => `
//                             <div class="col-md-6">
//                                 <div class="card mb-3">
//                                     <div class="card-body d-flex justify-content-between align-items-center">
//                                         <div>
//                                             <strong>${d.fileName}</strong><br>
//                                             <small class="text-muted">Feltöltve: ${new Date(d.uploadDate).toLocaleDateString('hu-HU')}</small>
//                                         </div>
//                                         <a href="${d.filePath}" target="_blank" class="btn btn-sm btn-outline-primary">Megnyitás</a>
//                                     </div>
//                                 </div>
//                             </div>
//                         `).join('') : '<p class="text-muted">Nincsenek dokumentumok.</p>'}
//                     </div>
//                 </div>
//             `;

//         } catch (err) {
//             console.error('Hiba:', err);
//             content.innerHTML = `<div class="alert alert-danger m-4"><strong>Hiba:</strong> ${err.message}</div>`;
//             window.c92.showToast('error', 'Hiba a betöltéskor');
//         }
//     });
// });