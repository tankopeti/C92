// /js/Partner/viewPartner.js – Partner megtekintése (View modal)
document.addEventListener('DOMContentLoaded', () => {
    console.log('✅ viewPartner.js betöltve');

    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.view-partner-btn');
        if (!btn) return;

        const partnerId = btn.dataset.partnerId;
        if (!partnerId) {
            window.c92?.showToast?.('error', 'Hiányzó Partner ID');
            return;
        }

        const modalEl = document.getElementById('viewPartnerModal');
        const contentEl = document.getElementById('viewPartnerContent');
        if (!modalEl || !contentEl) {
            console.error('viewPartnerModal vagy viewPartnerContent nem található');
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

        contentEl.innerHTML = loadingHtml('Adatok betöltése...');

        try {
            const res = await fetch(`/api/Partners/${partnerId}`, {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' }
            });

            if (!res.ok) throw new Error(res.status === 404 ? 'Partner nem található' : `HTTP ${res.status}`);

            const d = await res.json();

            const statusName = d.status?.name ?? 'N/A';
            const statusColor = d.status?.color ?? '#6c757d';
            const statusTextColor = normalizeTextColor(statusColor);

            const headerTitle = escapeHtml(d.companyName?.trim())
                ? `${escapeHtml(d.companyName)} <span class="text-muted fw-normal">(${escapeHtml(d.name || '')})</span>`
                : escapeHtml(d.name ?? 'Névtelen partner');

            contentEl.innerHTML = `
                <div class="container-fluid">
                    <!-- HEADER -->
                    <div class="d-flex flex-column flex-md-row justify-content-between align-items-start gap-3 mb-3">
                        <div>
                            <h4 class="fw-bold mb-1">${headerTitle}</h4>
                            <div class="text-muted">
                                <span class="me-2">ID: <strong>${escapeHtml(String(d.partnerId ?? '—'))}</strong></span>
                            </div>
                        </div>

                        <div class="text-md-end">
                            <div class="mb-1">
                            <span class="me-2">Státusz </span>
                                <span class="badge"
                                      style="background:${escapeAttr(statusColor)};color:${escapeAttr(statusTextColor)}">
                                    ${escapeHtml(statusName)}
                                </span>
                            </div>
                            <div class="text-muted small">
                                Utolsó kapcsolat: ${formatDateHU(d.lastContacted)}
                            </div>
                        </div>
                    </div>

                    ${section('Kapcsolat', `
                        <div class="row g-3">
                            ${kv('E-mail', mailto(d.email))}
                            ${kv('Telefonszám', tel(d.phoneNumber))}
                            ${kv('Másodlagos telefonszám', tel(d.alternatePhone))}
                            ${kv('Weboldal', website(d.website))}
                        </div>
                    `)}

                    ${section('Cím', `
                        <div class="row g-3">
                            ${kv('Utca, házszám', d.addressLine1)}
                            ${kv('Kiegészítő cím', d.addressLine2)}
                            ${kv('Város', d.city)}
                            ${kv('Megye', d.state)}
                            ${kv('Irányítószám', d.postalCode)}
                            ${kv('Ország', d.country)}
                        </div>
                        <div class="mt-3">
                            ${mapsLink(d)}
                        </div>
                    `)}

                    ${section('Üzleti adatok', `
                        <div class="row g-3">
                            ${kv('Adószám', d.taxId)}
                            ${kv('Nemzetközi adószám', d.intTaxId)}
                            ${kv('Iparág', d.industry)}
                            ${kv('Értékesítő / felelős', d.assignedTo)}
                            ${kv('Preferált valuta', d.preferredCurrency)}
                            ${kv('Partner csoport', d.partnerGroupId != null ? String(d.partnerGroupId) : null)}
                        </div>
                    `)}

                    ${section('Számlázás', `
                        <div class="row g-3">
                            ${kv('Számlázási kapcsolattartó', d.billingContactName)}
                            ${kv('Számlázási e-mail', mailto(d.billingEmail))}
                            ${kv('Fizetési feltételek', d.paymentTerms)}
                            ${kv('Kredit limit', formatMoney(d.creditLimit, d.preferredCurrency))}
                            ${kv('Adómentesség', d.isTaxExempt === true ? 'Igen' : d.isTaxExempt === false ? 'Nem' : '—')}
                        </div>
                    `)}

                    ${section('Jegyzetek', `
                        <div class="p-3 bg-body-tertiary rounded-3">
                            ${d.notes ? nl2br(escapeHtml(d.notes)) : '<span class="text-muted">Nincs jegyzet.</span>'}
                        </div>
                    `)}

                    ${tableSection('Telephelyek', d.sites, siteHead(), siteRow)}
                    ${tableSection('Kapcsolattartók', d.contacts, contactHead(), contactRow)}
                    ${tableSection('Dokumentumok', d.documents, documentHead(), documentRow)}
                </div>
            `;
        } catch (err) {
            console.error(err);
            contentEl.innerHTML = `
                <div class="alert alert-danger m-3">
                    <strong>Hiba:</strong> ${escapeHtml(err.message || 'Nem sikerült betölteni a partner adatait.')}
                </div>
            `;
            window.c92?.showToast?.('error', 'Hiba a partner betöltésekor');
        }
    });

    /* ================== HELPERS ================== */

    function loadingHtml(text) {
        return `
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status"></div>
                <p class="mt-3 mb-0">${escapeHtml(text)}</p>
            </div>
        `;
    }

    function section(title, bodyHtml) {
        return `
            <hr class="my-4">
            <h5 class="mb-3">${escapeHtml(title)}</h5>
            ${bodyHtml}
        `;
    }

    function kv(label, value) {
        return `
            <div class="col-md-6">
                <div class="text-muted small">${escapeHtml(label)}</div>
                <div>${value == null || value === '' ? '—' : value}</div>
            </div>
        `;
    }

    function badge(text, type) {
        return `<span class="badge bg-${escapeAttr(type)}">${escapeHtml(text)}</span>`;
    }

    function mailto(email) {
        if (!email) return '—';
        const safe = escapeHtml(email);
        return `<a href="mailto:${escapeAttr(email)}">${safe}</a>`;
    }

    function tel(phone) {
        if (!phone) return '—';
        const safe = escapeHtml(phone);
        const telHref = phone.replace(/\s+/g, '');
        return `<a href="tel:${escapeAttr(telHref)}">${safe}</a>`;
    }

    function website(url) {
        if (!url) return '—';
        const safeText = escapeHtml(url);
        const href = url.startsWith('http://') || url.startsWith('https://')
            ? url
            : `https://${url}`;
        return `<a href="${escapeAttr(href)}" target="_blank" rel="noopener">${safeText}</a>`;
    }

    function formatDateHU(val) {
        if (!val) return '—';
        const d = new Date(val);
        if (isNaN(d.getTime())) return '—';
        return d.toLocaleDateString('hu-HU');
    }

    function formatMoney(amount, currency) {
        if (amount == null || amount === '') return '—';
        const num = Number(amount);
        if (Number.isNaN(num)) return '—';
        const cur = currency ? ` ${escapeHtml(currency)}` : '';
        return `${num.toLocaleString('hu-HU')}${cur}`;
    }

    function mapsLink(d) {
        const parts = [
            d.addressLine1,
            d.addressLine2,
            d.city,
            d.state,
            d.postalCode,
            d.country
        ].filter(Boolean);

        if (!parts.length) return `<span class="text-muted">Nincs megadott cím.</span>`;

        const q = parts.join(', ');
        const href = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(q)}`;
        return `<a class="btn btn-sm btn-outline-secondary" href="${escapeAttr(href)}" target="_blank" rel="noopener">
                    <i class="bi bi-geo-alt me-1"></i> Megnyitás térképen
                </a>`;
    }

    function tableSection(title, items, headHtml, rowFn) {
        const arr = Array.isArray(items) ? items : [];
        return `
            <hr class="my-4">
            <div class="d-flex justify-content-between align-items-center mb-2">
                <h5 class="mb-0">${escapeHtml(title)} (${arr.length})</h5>
            </div>
            ${arr.length ? `
                <div class="table-responsive">
                    <table class="table table-sm table-bordered align-middle">
                        <thead class="table-light">${headHtml}</thead>
                        <tbody>
                            ${arr.map(rowFn).join('')}
                        </tbody>
                    </table>
                </div>
            ` : `<p class="text-muted mb-0">Nincs adat.</p>`}
        `;
    }

    /* ---- Sites ---- */
    function siteHead() {
        return `
            <tr>
                <th>Név</th>
                <th>Cím</th>
                <th>Város</th>
                <th>Megye</th>
                <th>Irányítószám</th>
                <th>Ország</th>
                <th>Elsődleges</th>
            </tr>`;
    }

    function siteRow(s) {
        const addr = [s.addressLine1, s.addressLine2].filter(Boolean).join(' ');
        return `
            <tr>
                <td>${escapeHtml(s.siteName ?? '—')}</td>
                <td>${escapeHtml(addr || '—')}</td>
                <td>${escapeHtml(s.city ?? '—')}</td>
                <td>${escapeHtml(s.state ?? '—')}</td>
                <td>${escapeHtml(s.postalCode ?? '—')}</td>
                <td>${escapeHtml(s.country ?? '—')}</td>
                <td>${s.isPrimary ? badge('Igen', 'success') : 'Nem'}</td>
            </tr>`;
    }

    /* ---- Contacts ---- */
    function contactHead() {
        return `
            <tr>
                <th>Név</th>
                <th>E-mail</th>
                <th>Telefon</th>
                <th>Másodlagos telefon</th>
                <th>Szerepkör</th>
                <th>Elsődleges</th>
            </tr>`;
    }

    function contactRow(c) {
        const fullName = [c.firstName, c.lastName].filter(Boolean).join(' ') || '—';
        return `
            <tr>
                <td>${escapeHtml(fullName)}</td>
                <td>${c.email ? `<a href="mailto:${escapeAttr(c.email)}">${escapeHtml(c.email)}</a>` : '—'}</td>
                <td>${c.phoneNumber ? `<a href="tel:${escapeAttr(String(c.phoneNumber).replace(/\s+/g,''))}">${escapeHtml(c.phoneNumber)}</a>` : '—'}</td>
                <td>${c.phoneNumber2 ? `<a href="tel:${escapeAttr(String(c.phoneNumber2).replace(/\s+/g,''))}">${escapeHtml(c.phoneNumber2)}</a>` : '—'}</td>
                <td>${escapeHtml(c.jobTitle ?? '—')}</td>
                <td>${c.isPrimary ? badge('Igen', 'primary') : 'Nem'}</td>
            </tr>`;
    }

    /* ---- Documents ---- */
    function documentHead() {
        return `
            <tr>
                <th>Fájlnév</th>
                <th>Típus</th>
                <th>Feltöltve</th>
                <th>Művelet</th>
            </tr>`;
    }

    function documentRow(d) {
        const fileName = d.fileName ?? d.name ?? '—';
        const uploaded = d.uploadDate ?? d.createdAt ?? d.createdDate ?? null;
        const filePath = d.filePath ?? d.url ?? null;

        return `
            <tr>
                <td>${escapeHtml(fileName)}</td>
                <td>${escapeHtml(d.contentType ?? d.type ?? '—')}</td>
                <td>${formatDateHU(uploaded)}</td>
                <td>
                    ${filePath
                        ? `<a class="btn btn-sm btn-outline-primary" href="${escapeAttr(filePath)}" target="_blank" rel="noopener">Megnyitás</a>`
                        : '—'}
                </td>
            </tr>`;
    }

    /* ---- Utils ---- */
    function normalizeTextColor(bgHex) {
        // egyszerű: ha sárga (#ffc107) vagy világos -> fekete, különben fehér
        const c = (bgHex || '').toLowerCase();
        if (c === '#ffc107' || c === '#ffe082' || c === '#ffeb3b') return 'black';
        return 'white';
    }

    function escapeHtml(str) {
        return String(str ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function escapeAttr(str) {
        return escapeHtml(str).replaceAll('`', '&#096;');
    }

    function nl2br(s) {
        return String(s).replace(/\n/g, '<br>');
    }
});
