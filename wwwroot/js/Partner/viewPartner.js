(function () {
  'use strict';

  document.addEventListener('DOMContentLoaded', function () {
    console.log('[viewPartner] DOM loaded');

    const modalEl = document.getElementById('viewPartnerModal');
    const bodyEl = document.getElementById('viewPartnerModalBody');
    const titleEl = document.getElementById('viewPartnerModalTitle');
    const editBtn = document.getElementById('editPartnerBtn');

    if (!modalEl || !bodyEl) {
      console.warn('[viewPartner] missing modal elements');
      return;
    }

    // --- CSRF (ha kell) ---
    const csrf =
      document.querySelector('meta[name="csrf-token"]')?.content ||
      document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
      (document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1]
        ? decodeURIComponent(document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1])
        : '') ||
      '';

    /* -------------------------------------------------- */
    /* GFO cache (id -> name)                              */
    /* -------------------------------------------------- */
    let gfoMap = null;

    async function loadGfoMap() {
      if (gfoMap) return gfoMap;

      const res = await fetch('/api/Partners/gfos', {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!res.ok) throw new Error(`GFO HTTP ${res.status}`);

      const arr = await res.json();
      gfoMap = new Map((arr || []).map(x => [Number(x.id), x.name || '']));
      return gfoMap;
    }

    function getGfoNameById(id) {
      if (!gfoMap) return null;
      const n = gfoMap.get(Number(id));
      return n && String(n).trim() ? n : null;
    }

    /* -------------------------------------------------- */
    /* PartnerType cache (id -> name)                      */
    /* -------------------------------------------------- */
    let partnerTypeMap = null;

    async function loadPartnerTypeMap() {
      if (partnerTypeMap) return partnerTypeMap;

      const res = await fetch('/api/Partners/partnerTypes', {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!res.ok) throw new Error(`PartnerTypes HTTP ${res.status}`);

      const arr = await res.json();
      partnerTypeMap = new Map((arr || []).map(x => [Number(x.id), x.name || '']));
      return partnerTypeMap;
    }

    function getPartnerTypeNameById(id) {
      if (!partnerTypeMap) return null;
      const n = partnerTypeMap.get(Number(id));
      return n && String(n).trim() ? n : null;
    }

    /* -------------------------------------------------- */
    /* Helpers                                            */
    /* -------------------------------------------------- */

    function esc(v) {
      const d = document.createElement('div');
      d.textContent = v == null ? '' : String(v);
      return d.innerHTML;
    }

    function fmtDate(v) {
      if (!v) return '–';
      const d = new Date(v);
      if (isNaN(d.getTime())) return '–';
      return d.toLocaleString('hu-HU', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      }).replace(',', '');
    }

    function fmtDateOnly(v) {
      if (!v) return '–';
      const d = new Date(v);
      if (isNaN(d.getTime())) return '–';
      return d.toLocaleDateString('hu-HU');
    }

    function isLight(hex) {
      const h = String(hex || '').replace('#', '').trim();
      if (h.length !== 6) return false;
      const r = parseInt(h.slice(0, 2), 16);
      const g = parseInt(h.slice(2, 4), 16);
      const b = parseInt(h.slice(4, 6), 16);
      if ([r, g, b].some(x => Number.isNaN(x))) return false;
      const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
      return lum > 170;
    }

    function badge(text, color) {
      const bg = color && String(color).trim() ? color : '#6c757d';
      const txt = isLight(bg) ? '#212529' : '#fff';
      return `<span class="badge" style="background:${esc(bg)};color:${esc(txt)}">${esc(text || '–')}</span>`;
    }

    function kvRow(label, valueHtml) {
      return `
        <div class="d-flex justify-content-between gap-3 py-1">
          <div class="text-muted">${esc(label)}</div>
          <div class="text-end">${valueHtml == null || valueHtml === '' ? '–' : valueHtml}</div>
        </div>`;
    }

    function mailto(v) {
      if (!v) return '–';
      return `<a href="mailto:${esc(v)}">${esc(v)}</a>`;
    }

    function tel(v) {
      if (!v) return '–';
      const href = String(v).replace(/\s+/g, '');
      return `<a href="tel:${esc(href)}">${esc(v)}</a>`;
    }

    function website(v) {
      if (!v) return '–';
      const u = String(v);
      const href = u.startsWith('http://') || u.startsWith('https://') ? u : `https://${u}`;
      return `<a href="${esc(href)}" target="_blank" rel="noopener">${esc(u)}</a>`;
    }

    function formatMoney(amount, currency) {
      if (amount == null || amount === '') return '–';
      const num = Number(amount);
      if (Number.isNaN(num)) return '–';
      const cur = currency ? ` ${esc(currency)}` : '';
      return `${num.toLocaleString('hu-HU')}${cur}`;
    }

    function listTable(title, items, headHtml, rowFn) {
      const arr = Array.isArray(items) ? items : [];
      return `
        <div class="p-3 border rounded bg-light mt-3">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <h6 class="mb-0 text-success">${esc(title)}</h6>
            <span class="badge bg-light text-dark">${esc(arr.length)}</span>
          </div>
          ${arr.length ? `
            <div class="table-responsive">
              <table class="table table-sm table-bordered align-middle mb-0">
                <thead class="table-light">${headHtml}</thead>
                <tbody>${arr.map(rowFn).join('')}</tbody>
              </table>
            </div>
          ` : `<div class="text-muted small">Nincs adat.</div>`}
        </div>
      `;
    }

    /* -------------------------------------------------- */
    /* Documents                                          */
    /* -------------------------------------------------- */

    function renderDocuments(d) {
      const list = d.documents ?? d.Documents ?? [];
      if (!Array.isArray(list) || list.length === 0) {
        return `<div class="text-muted small">Nincs csatolt dokumentum.</div>`;
      }

      return list.map(doc => {
        const id = doc.documentId ?? doc.DocumentId ?? doc.id ?? doc.Id;
        const name = doc.fileName ?? doc.FileName ?? doc.name ?? doc.Name ?? (id ? `Dokumentum #${id}` : 'Dokumentum');
        const uploaded = doc.uploadDate ?? doc.UploadDate ?? doc.createdAt ?? doc.CreatedAt ?? doc.createdDate ?? doc.CreatedDate;
        const uploadedBy = doc.uploadedByName ?? doc.UploadedByName ?? doc.createdByName ?? doc.CreatedByName;
        const note = doc.note ?? doc.Note;

        const filePath = doc.filePath ?? doc.FilePath ?? doc.url ?? doc.Url;
        const href = filePath ? String(filePath) : (id ? `/documents/download/${encodeURIComponent(id)}` : '');

        return `
          <div class="d-flex justify-content-between align-items-start p-2 mb-2 bg-white border rounded">
            <div class="me-2">
              <div>
                <i class="bi bi-paperclip me-2 text-success"></i>
                ${href
                  ? `<a href="${esc(href)}" target="_blank" rel="noopener"><strong>${esc(name)}</strong></a>`
                  : `<strong>${esc(name)}</strong>`
                }
              </div>
              <div class="text-muted small mt-1">
                ${uploaded ? esc(fmtDate(uploaded)) : '–'}
                ${uploadedBy ? ` • ${esc(uploadedBy)}` : ''}
                ${note ? ` • ${esc(note)}` : ''}
              </div>
            </div>
            ${id ? `<span class="badge bg-light text-dark">#${esc(id)}</span>` : ''}
          </div>
        `;
      }).join('');
    }

    /* -------------------------------------------------- */
    /* Sites / Contacts tables                            */
    /* -------------------------------------------------- */

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
      const addr = [
        s.addressLine1 ?? s.AddressLine1,
        s.addressLine2 ?? s.AddressLine2
      ].filter(Boolean).join(' ');
      const isPrimary = s.isPrimary ?? s.IsPrimary;
      return `
        <tr>
          <td>${esc(s.siteName ?? s.SiteName ?? '–')}</td>
          <td>${esc(addr || '–')}</td>
          <td>${esc(s.city ?? s.City ?? '–')}</td>
          <td>${esc(s.state ?? s.State ?? '–')}</td>
          <td>${esc(s.postalCode ?? s.PostalCode ?? '–')}</td>
          <td>${esc(s.country ?? s.Country ?? '–')}</td>
          <td>${isPrimary ? `<span class="badge bg-success">Igen</span>` : 'Nem'}</td>
        </tr>`;
    }

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
      const first = c.firstName ?? c.FirstName;
      const last = c.lastName ?? c.LastName;
      const fullName = [first, last].filter(Boolean).join(' ') || '–';

      const email = c.email ?? c.Email;
      const phone1 = c.phoneNumber ?? c.PhoneNumber;
      const phone2 = c.phoneNumber2 ?? c.PhoneNumber2;
      const isPrimary = c.isPrimary ?? c.IsPrimary;

      return `
        <tr>
          <td>${esc(fullName)}</td>
          <td>${email ? `<a href="mailto:${esc(email)}">${esc(email)}</a>` : '–'}</td>
          <td>${phone1 ? `<a href="tel:${esc(String(phone1).replace(/\s+/g, ''))}">${esc(phone1)}</a>` : '–'}</td>
          <td>${phone2 ? `<a href="tel:${esc(String(phone2).replace(/\s+/g, ''))}">${esc(phone2)}</a>` : '–'}</td>
          <td>${esc(c.jobTitle ?? c.JobTitle ?? '–')}</td>
          <td>${isPrimary ? `<span class="badge bg-primary">Igen</span>` : 'Nem'}</td>
        </tr>`;
    }

    /* -------------------------------------------------- */
    /* Render main                                        */
    /* -------------------------------------------------- */

    function renderPartner(d) {
      const id = d.partnerId ?? d.PartnerId ?? d.id ?? d.Id;

      const companyName = (d.companyName ?? d.CompanyName ?? '').trim();
      const name = (d.name ?? d.Name ?? '').trim();

      const headerTitle = companyName
        ? `${esc(companyName)} <span class="text-muted fw-normal">(${esc(name || '')})</span>`
        : esc(name || 'Névtelen partner');

      const statusName = d.status?.name ?? d.Status?.Name ?? d.statusName ?? d.StatusName ?? '–';
      const statusColor = d.status?.color ?? d.Status?.Color ?? d.statusColor ?? d.StatusColor ?? '#6c757d';

      const lastContacted = d.lastContacted ?? d.LastContacted;
      const assignedTo = d.assignedTo ?? d.AssignedTo ?? '–';

      // GFO: prefer backend name; fallback map
      const gfoId = d.gfoId ?? d.GFOId;
      const gfoName =
        d.gfoName ?? d.GFOName ?? (gfoId != null ? getGfoNameById(gfoId) : null);
      const gfoText = gfoName ? esc(gfoName) : '–';

      // PartnerType: prefer backend name; fallback map
      const partnerTypeId = d.partnerTypeId ?? d.PartnerTypeId;
      const partnerTypeName =
        d.partnerTypeName ?? d.PartnerTypeName ?? (partnerTypeId != null ? getPartnerTypeNameById(partnerTypeId) : null);
      const partnerTypeText = partnerTypeName ? esc(partnerTypeName) : '–';

      if (titleEl) {
        titleEl.innerHTML = `<i class="bi bi-eye me-2"></i> Partner #${esc(id)} – ${headerTitle}`;
      }

      bodyEl.innerHTML = `
        <div class="row g-4">
          <div class="col-lg-6">
            <div class="p-3 border rounded bg-light">
              <h6 class="mb-3 text-success">Alapadatok</h6>

              ${kvRow('Név', headerTitle)}
              ${kvRow('Rövid név', esc(d.shortName ?? d.ShortName ?? '–'))}
              ${kvRow('Státusz', badge(statusName, statusColor))}
              ${kvRow('Aktív', (d.isActive ?? d.IsActive) === false ? 'Nem' : 'Igen')}
              ${kvRow('Utolsó kapcsolat', lastContacted ? esc(fmtDateOnly(lastContacted)) : '–')}

              <hr/>

              ${kvRow('Partner kód', esc(d.partnerCode ?? d.PartnerCode ?? '–'))}
              ${kvRow('Saját azonosító', esc(d.ownId ?? d.OwnId ?? '–'))}
              ${kvRow('GFO', gfoText)}
              ${kvRow('PartnerGroupId', esc((d.partnerGroupId ?? d.PartnerGroupId) != null ? String(d.partnerGroupId ?? d.PartnerGroupId) : '–'))}
              ${kvRow('Partner típus', partnerTypeText)}

              <hr/>

              ${kvRow('Értékesítő / felelős', esc(assignedTo))}
              ${kvRow('Iparág', esc(d.industry ?? d.Industry ?? '–'))}
              ${kvRow('Preferált valuta', esc(d.preferredCurrency ?? d.PreferredCurrency ?? '–'))}
            </div>

            <div class="p-3 border rounded bg-light mt-3">
              <h6 class="mb-3 text-success">Kapcsolat</h6>
              ${kvRow('E-mail', mailto(d.email ?? d.Email))}
              ${kvRow('Telefonszám', tel(d.phoneNumber ?? d.PhoneNumber))}
              ${kvRow('Másodlagos telefonszám', tel(d.alternatePhone ?? d.AlternatePhone))}
              ${kvRow('Weboldal', website(d.website ?? d.Website))}
            </div>

            <div class="p-3 border rounded bg-light mt-3">
              <h6 class="mb-3 text-success">Adó / azonosítók</h6>
              ${kvRow('Adószám', esc(d.taxId ?? d.TaxId ?? '–'))}
              ${kvRow('Nemzetközi adószám', esc(d.intTaxId ?? d.IntTaxId ?? '–'))}
              ${kvRow('Magánszemély adóazonosító', esc(d.individualTaxId ?? d.IndividualTaxId ?? '–'))}
            </div>

            <div class="p-3 border rounded bg-light mt-3">
              <h6 class="mb-3 text-success">Jegyzetek</h6>
              <div class="bg-white border rounded p-2">
                ${(d.notes ?? d.Notes)
                  ? esc(String(d.notes ?? d.Notes)).replace(/\n/g, '<br>')
                  : '<span class="text-muted small">Nincs jegyzet.</span>'}
              </div>
              <div class="mt-2">
                ${kvRow('Komment 1', esc(d.comment1 ?? d.Comment1 ?? '–'))}
                ${kvRow('Komment 2', esc(d.comment2 ?? d.Comment2 ?? '–'))}
              </div>
            </div>
          </div>

          <div class="col-lg-6">
            <div class="p-3 border rounded bg-light">
              <h6 class="mb-3 text-success">Cím</h6>
              ${kvRow('Utca, házszám', esc(d.addressLine1 ?? d.AddressLine1 ?? '–'))}
              ${kvRow('Kiegészítő cím', esc(d.addressLine2 ?? d.AddressLine2 ?? '–'))}
              ${kvRow('Város', esc(d.city ?? d.City ?? '–'))}
              ${kvRow('Megye', esc(d.state ?? d.State ?? '–'))}
              ${kvRow('Irányítószám', esc(d.postalCode ?? d.PostalCode ?? '–'))}
              ${kvRow('Ország', esc(d.country ?? d.Country ?? '–'))}
            </div>

            <div class="p-3 border rounded bg-light mt-3">
              <h6 class="mb-3 text-success">Számlázás</h6>
              ${kvRow('Számlázási kapcsolattartó', esc(d.billingContactName ?? d.BillingContactName ?? '–'))}
              ${kvRow('Számlázási e-mail', mailto(d.billingEmail ?? d.BillingEmail))}
              ${kvRow('Fizetési feltételek', esc(d.paymentTerms ?? d.PaymentTerms ?? '–'))}
              ${kvRow('Kredit limit', formatMoney(d.creditLimit ?? d.CreditLimit, d.preferredCurrency ?? d.PreferredCurrency))}
              ${kvRow('Adómentesség', (d.isTaxExempt ?? d.IsTaxExempt) === true ? 'Igen' : (d.isTaxExempt ?? d.IsTaxExempt) === false ? 'Nem' : '–')}
            </div>

            <div class="p-3 border rounded bg-light mt-3">
              <h6 class="mb-3 text-success">Audit</h6>
              ${kvRow('Létrehozva', esc(fmtDate(d.createdDate ?? d.CreatedDate)))}
              ${kvRow('Létrehozó', esc(d.createdBy ?? d.CreatedBy ?? '–'))}
              ${kvRow('Módosítva', esc(fmtDate(d.updatedDate ?? d.UpdatedDate)))}
              ${kvRow('Módosította', esc(d.updatedBy ?? d.UpdatedBy ?? '–'))}
            </div>

            <div class="p-3 border rounded bg-light mt-3">
              <h6 class="mb-3 text-success">
                <i class="bi bi-paperclip me-2"></i> Dokumentumok
              </h6>
              ${renderDocuments(d)}
            </div>

            ${listTable('Telephelyek', d.sites ?? d.Sites, siteHead(), siteRow)}
            ${listTable('Kapcsolattartók', d.contacts ?? d.Contacts, contactHead(), contactRow)}
          </div>
        </div>
      `;
    }

    /* -------------------------------------------------- */
    /* Open                                               */
    /* -------------------------------------------------- */

    async function openPartnerView(partnerId) {
      const idNum = parseInt(String(partnerId || ''), 10);
      if (!Number.isFinite(idNum) || idNum <= 0) return;

      bodyEl.innerHTML = `
        <div class="text-center py-5">
          <div class="spinner-border text-success"></div>
          <p class="mt-3 text-muted">Adatok betöltése...</p>
        </div>`;

      bootstrap.Modal.getOrCreateInstance(modalEl).show();

      try {
        const data = await fetchPartner(idNum);

        // map-ek (ha nem mennek, attól még renderelünk)
        try { await loadGfoMap(); } catch (e) { console.warn('[viewPartner] gfoMap load failed', e); }
        try { await loadPartnerTypeMap(); } catch (e) { console.warn('[viewPartner] partnerTypeMap load failed', e); }

        renderPartner(data);
      } catch (e) {
        console.error('[viewPartner] load failed', e);
        bodyEl.innerHTML = `<div class="alert alert-danger m-0">Nem sikerült betölteni a partnert.</div>`;
        window.c92?.showToast?.('error', 'Hiba a partner betöltésekor');
      }
    }

    async function fetchPartner(id) {
      const res = await fetch(`/api/Partners/${encodeURIComponent(id)}`, {
        method: 'GET',
        headers: {
          'Accept': 'application/json',
          ...(csrf ? { 'RequestVerificationToken': csrf } : {})
        },
        credentials: 'same-origin'
      });

      if (!res.ok) {
        const t = await res.text().catch(() => '');
        throw new Error(`HTTP ${res.status} ${t}`);
      }

      return res.json();
    }

    /* -------------------------------------------------- */
    /* Events                                             */
    /* -------------------------------------------------- */

    // ✅ EZT hagyjuk érintetlenül (nálad ez működik)
    document.addEventListener('click', function (e) {
      const btn = e.target.closest('.view-partner-btn,[data-view-partner],.js-view-partner-btn');
      if (!btn) return;

      const id =
        btn.dataset.partnerId ||
        btn.dataset.viewPartner ||
        btn.getAttribute('data-partner-id') ||
        btn.getAttribute('data-view-partner');

      if (id) openPartnerView(id);
    });

    window.Partners = window.Partners || {};
    window.Partners.openViewModal = openPartnerView;

    modalEl.addEventListener('hidden.bs.modal', function () {
      bodyEl.innerHTML = '';
      if (titleEl) titleEl.innerHTML = '';
      if (editBtn) {
        editBtn.style.display = 'none';
        editBtn.onclick = null;
      }
    });
  });
})();
