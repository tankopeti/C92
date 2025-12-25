// /js/Partner/viewPartner.js – Kizárólag a partner megtekintése (View modal)
document.addEventListener('DOMContentLoaded', function () {
    console.log('viewPartner.js BETÖLTÖDÖTT – kész a megtekintésre');

    document.addEventListener('click', async function (e) {
        const viewBtn = e.target.closest('.view-partner-btn');
        if (!viewBtn) return;

        const partnerId = viewBtn.dataset.partnerId;
        if (!partnerId) {
            window.c92.showToast('error', 'Hiba: Partner ID hiányzik');
            return;
        }

        console.log(`Megtekintés – Partner ID: ${partnerId}`);

        const modalEl = document.getElementById('viewPartnerModal');
        const content = document.getElementById('viewPartnerContent');
        if (!modalEl || !content) {
            console.error('viewPartnerModal vagy viewPartnerContent nem található');
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

        content.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Betöltés...</span>
                </div>
                <p class="mt-3">Adatok betöltése...</p>
            </div>
        `;

        try {
            const response = await fetch(`/api/Partners/${partnerId}`);
            if (!response.ok) {
                throw new Error(response.status === 404 ? 'Partner nem található vagy inaktív' : 'Szerver hiba');
            }

            const data = await response.json();

            content.innerHTML = `
                <div class="container-fluid">
                    <div class="text-center mb-4">
                        <h4 class="mb-3">${data.name || 'Névtelen partner'}</h4>
                        <p class="text-muted">Partner ID: ${data.partnerId}</p>
                    </div>

                    <div class="row g-3 mb-4 text-start">
                        <div class="col-md-6"><strong>Cégnév:</strong> ${data.companyName || '—'}</div>
                        <div class="col-md-6"><strong>E-mail:</strong> ${data.email || '—'}</div>
                        <div class="col-md-6"><strong>Telefonszám:</strong> ${data.phoneNumber || '—'}</div>
                        <div class="col-md-6"><strong>Másodlagos telefon:</strong> ${data.alternatePhone || '—'}</div>
                        <div class="col-md-6">
                            <strong>Weboldal:</strong> 
                            ${data.website ? `<a href="${data.website.startsWith('http') ? data.website : 'https://' + data.website}" target="_blank">${data.website}</a>` : '—'}
                        </div>
                        <div class="col-md-6"><strong>Adószám:</strong> ${data.taxId || '—'}</div>
                        <div class="col-md-6"><strong>Nemzetközi adószám:</strong> ${data.intTaxId || '—'}</div>
                        <div class="col-md-6"><strong>Iparág:</strong> ${data.industry || '—'}</div>
                    </div>

                    <hr class="my-4">

                    <h5>Cím adatok</h5>
                    <div class="row g-3 mb-4 text-start">
                        <div class="col-md-6"><strong>Utca, házszám:</strong> ${data.addressLine1 || '—'}</div>
                        <div class="col-md-6"><strong>Kiegészítő cím:</strong> ${data.addressLine2 || '—'}</div>
                        <div class="col-md-6"><strong>Város:</strong> ${data.city || '—'}</div>
                        <div class="col-md-6"><strong>Megye:</strong> ${data.state || '—'}</div>
                        <div class="col-md-6"><strong>Irányítószám:</strong> ${data.postalCode || '—'}</div>
                        <div class="col-md-6"><strong>Ország:</strong> ${data.country || '—'}</div>
                    </div>

                    <hr class="my-4">

                    <h5>Számlázási adatok</h5>
                    <div class="row g-3 mb-4 text-start">
                        <div class="col-md-6"><strong>Számlázási kapcsolattartó:</strong> ${data.billingContactName || '—'}</div>
                        <div class="col-md-6"><strong>Számlázási e-mail:</strong> ${data.billingEmail || '—'}</div>
                        <div class="col-md-6"><strong>Fizetési feltételek:</strong> ${data.paymentTerms || '—'}</div>
                        <div class="col-md-6"><strong>Kredit limit:</strong> ${data.creditLimit ? data.creditLimit + ' ' + (data.preferredCurrency || '') : '—'}</div>
                        <div class="col-md-6"><strong>Előnyben részesített valuta:</strong> ${data.preferredCurrency || '—'}</div>
                        <div class="col-md-6"><strong>Adómentesség:</strong> ${data.isTaxExempt ? 'Igen' : 'Nem'}</div>
                    </div>

                    <hr class="my-4">

                    <h5>További adatok</h5>
                    <div class="row g-3 mb-4 text-start">
                        <div class="col-md-6"><strong>Értékesítő:</strong> ${data.assignedTo || '—'}</div>
                        <div class="col-md-6"><strong>Utolsó kapcsolatfelvétel:</strong> ${data.lastContacted ? new Date(data.lastContacted).toLocaleDateString('hu-HU') : '—'}</div>
                        <div class="col-md-6"><strong>Státusz:</strong> <span class="badge bg-secondary">${data.status?.name || 'N/A'}</span></div>
                    </div>

                    <hr class="my-4">

                    <h5>Jegyzetek</h5>
                    <p class=" text-start">${data.notes || 'Nincsenek jegyzetek.'}</p>

                    <!-- Telephelyek – táblázat -->
                    <hr class="my-4">
                    <h5>Telephelyek (${data.sites?.length || 0})</h5>
                    ${data.sites?.length > 0 ? `
                        <div class="table-responsive mb-4">
                            <table class="table table-bordered table-sm">
                                <thead class="table-light">
                                    <tr>
                                        <th>Telephely neve</th>
                                        <th>Cím</th>
                                        <th>Város</th>
                                        <th>Irányítószám</th>
                                        <th>Elsődleges</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${data.sites.map(s => `
                                    <tr>
                                        <td><strong>${s.siteName || '—'}</strong></td>
                                        <td>
                                            ${s.addressLine1 || s.addressLine2 ? 
                                                `<a href="https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(`${s.addressLine1 || ''} ${s.addressLine2 || ''} ${s.city || ''} ${s.postalCode || ''}`)}" target="_blank">
                                                    ${s.addressLine1 || ''} ${s.addressLine2 || ''}
                                                </a>` : '—'}
                                        </td>
                                        <td>${s.city || '—'}</td>
                                        <td>${s.postalCode || '—'}</td>
                                        <td>${s.isPrimary ? '<span class="badge bg-success">Igen</span>' : 'Nem'}</td>
                                    </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                    ` : '<p class="text-muted mb-4">Nincsenek telephelyek.</p>'}

                    <!-- Kapcsolattartók – táblázat -->
                    <hr class="my-4">
                    <h5>Kapcsolattartók (${data.contacts?.length || 0})</h5>
                    ${data.contacts?.length > 0 ? `
                        <div class="table-responsive mb-4">
                            <table class="table table-bordered table-sm">
                                <thead class="table-light">
                                    <tr>
                                        <th>Név</th>
                                        <th>E-mail</th>
                                        <th>Telefon</th>
                                        <th>Másodlagos telefon</th>
                                        <th>Szerepkör</th>
                                        <th>Elsődleges</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${data.contacts.map(c => `
                                        <tr>
                                            <td><strong>${c.firstName || ''} ${c.lastName || ''}</strong></td>
                                            <td>
                                                ${c.email ? `<a href="mailto:${c.email}">${c.email}</a>` : '—'}
                                            </td>
                                            <td>${c.phoneNumber || '—'}</td>
                                            <td>${c.phoneNumber2 || '—'}</td>
                                            <td>${c.jobTitle || '—'}</td>
                                            <td>${c.isPrimary ? '<span class="badge bg-primary">Igen</span>' : 'Nem'}</td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                    ` : '<p class="text-muted mb-4">Nincsenek kapcsolattartók.</p>'}

                    <!-- Árajánlatok – táblázat -->
                    <hr class="my-4">
                    <h5>Árajánlatok (${data.quotes?.length || 0})</h5>
                    ${data.quotes?.length > 0 ? `
                        <div class="table-responsive mb-4">
                            <table class="table table-bordered table-sm">
                                <thead class="table-light">
                                    <tr>
                                        <th>ID</th>
                                        <th>Dátum</th>
                                        <th>Összeg</th>
                                        <th>Valuta</th>
                                        <th>Státusz</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${data.quotes.map(q => `
                                        <tr>
                                            <td>#${q.quoteId || '—'}</td>
                                            <td>${q.quoteDate ? new Date(q.quoteDate).toLocaleDateString('hu-HU') : '—'}</td>
                                            <td>${q.totalAmount || '—'}</td>
                                            <td>${q.currency || '—'}</td>
                                            <td>${q.status || '—'}</td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                    ` : '<p class="text-muted mb-4">Nincsenek árajánlatok.</p>'}

                    <!-- Megrendelések – táblázat -->
                    <hr class="my-4">
                    <h5>Megrendelések (${data.orders?.length || 0})</h5>
                    ${data.orders?.length > 0 ? `
                        <div class="table-responsive mb-4">
                            <table class="table table-bordered table-sm">
                                <thead class="table-light">
                                    <tr>
                                        <th>ID</th>
                                        <th>Dátum</th>
                                        <th>Összeg</th>
                                        <th>Valuta</th>
                                        <th>Státusz</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${data.orders.map(o => `
                                        <tr>
                                            <td>#${o.orderId || '—'}</td>
                                            <td>${o.orderDate ? new Date(o.orderDate).toLocaleDateString('hu-HU') : '—'}</td>
                                            <td>${o.totalAmount || '—'}</td>
                                            <td>${o.currency || '—'}</td>
                                            <td>${o.status || '—'}</td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                    ` : '<p class="text-muted mb-4">Nincsenek megrendelések.</p>'}

                    <!-- Dokumentumok – táblázat -->
                    <hr class="my-4">
                    <h5>Dokumentumok (${data.documents?.length || 0})</h5>
                    ${data.documents?.length > 0 ? `
                        <div class="table-responsive mb-4">
                            <table class="table table-bordered table-sm">
                                <thead class="table-light">
                                    <tr>
                                        <th>Fájlnév</th>
                                        <th>Feltöltve</th>
                                        <th>Művelet</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${data.documents.map(d => `
                                        <tr>
                                            <td><strong>${d.fileName}</strong></td>
                                            <td>${new Date(d.uploadDate).toLocaleDateString('hu-HU')}</td>
                                            <td><a href="${d.filePath}" target="_blank" class="btn btn-sm btn-outline-primary">Megnyitás</a></td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                    ` : '<p class="text-muted mb-4">Nincsenek dokumentumok.</p>'}

                </div>
            `;

        } catch (err) {
            console.error('Hiba a betöltéskor:', err);
            content.innerHTML = `<div class="alert alert-danger m-4"><strong>Hiba:</strong> ${err.message || 'Nem sikerült betölteni'}</div>`;
            window.c92.showToast('error', 'Hiba a betöltéskor');
        }
    });
});