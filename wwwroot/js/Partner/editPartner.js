// /js/Partner/editPartner.js – Szerkesztés (AJAX PUT, reload nélkül) + új mezők + GFO select + PartnerType select
document.addEventListener('DOMContentLoaded', function () {
  console.log('editPartner.js BETÖLTÖDÖTT – AJAX mentés (reload nélkül)');

  const modalEl = document.getElementById('editPartnerModal');
  if (!modalEl) {
    console.error('editPartnerModal nem található');
    return;
  }

  const editForm = document.getElementById('editPartnerForm');

  // --- mező segéd ---
  const setValue = (id, value) => {
    const el = document.getElementById(id);
    if (!el) return;
    // checkbox
    if (el.type === 'checkbox') {
      el.checked = !!value;
      return;
    }
    el.value = value ?? '';
  };

  const setContent = (id, html) => {
    const el = document.getElementById(id);
    if (el) el.innerHTML = html;
  };

  /* ================== SELECTS: GFO + PartnerTypes ================== */

  // --- GFO select ---
  const gfoSelect = document.getElementById('editGfoId');
  let gfoLoaded = false;

  async function loadGfosOnce() {
    if (!gfoSelect || gfoLoaded) return;
    try {
      const res = await fetch('/api/Partners/gfos', {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const data = await res.json();
      gfoSelect.innerHTML =
        `<option value="">Válasszon...</option>` +
        (Array.isArray(data) ? data : []).map(x =>
          `<option value="${escapeAttr(x.id)}">${escapeHtml(x.name || '')}</option>`
        ).join('');

      gfoLoaded = true;
    } catch (e) {
      console.error('[editPartner] GFO load failed', e);
      window.c92?.showToast?.('error', 'GFO lista nem tölthető be');
    }
  }

  // --- PartnerType select ---
  // HTML-ben ez legyen: <select id="editPartnerTypeId" name="PartnerTypeId">...</select>
  const partnerTypeSelect = document.getElementById('editPartnerTypeId');
  let partnerTypeLoaded = false;

  async function loadPartnerTypesOnce() {
    if (!partnerTypeSelect || partnerTypeLoaded) return;
    try {
      const res = await fetch('/api/Partners/partnerTypes', {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const data = await res.json();
      partnerTypeSelect.innerHTML =
        `<option value="">Válasszon...</option>` +
        (Array.isArray(data) ? data : []).map(x =>
          `<option value="${escapeAttr(x.id)}">${escapeHtml(x.name || '')}</option>`
        ).join('');

      partnerTypeLoaded = true;
    } catch (e) {
      console.error('[editPartner] PartnerTypes load failed', e);
      window.c92?.showToast?.('error', 'Partner típus lista nem tölthető be');
    }
  }

  /* ================== OPEN + LOAD ================== */

  document.addEventListener('click', async function (e) {
    const editBtn = e.target.closest('.edit-partner-btn');
    if (!editBtn) return;

    const partnerId = editBtn.dataset.partnerId;
    if (!partnerId) {
      window.c92?.showToast?.('error', 'Hiba: Partner ID hiányzik');
      return;
    }

    // dropdownok előtöltés
    await loadGfosOnce();
    await loadPartnerTypesOnce();

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();

    try {
      const response = await fetch(`/api/Partners/${encodeURIComponent(partnerId)}`, {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        throw new Error(response.status === 404 ? 'Partner nem található' : `HTTP ${response.status}`);
      }

      const data = await response.json();

      // --- BASIC ---
      setValue('editPartnerId', data.partnerId);
      setValue('editPartnerName', data.name);
      setValue('editPartnerCompanyName', data.companyName);

      // --- új mezők ---
      setValue('editShortName', data.shortName);
      setValue('editPartnerCode', data.partnerCode);
      setValue('editOwnId', data.ownId);
      setValue('editAssignedTo', data.assignedTo);
      setValue('editLastContacted', toDateTimeLocal(data.lastContacted));
      setValue('editIsTaxExempt', data.isTaxExempt);
      setValue('editIsActive', data.isActive);

      // GFO
      const gfoId =
        data.gfoId ??
        data.GFOId ??
        data.gfo?.gfoId ??
        data.GFO?.GFOId ??
        null;

      if (gfoSelect) {
        gfoSelect.value = gfoId != null ? String(gfoId) : '';
      }

      // partnerGroup
      setValue('editPartnerGroupId', data.partnerGroupId);

      // PartnerType (select) – többféle property névre is toleráns
      const partnerTypeId =
        data.partnerTypeId ??
        data.PartnerTypeId ??
        data.partnerType?.partnerTypeId ??
        data.PartnerType?.PartnerTypeId ??
        null;

      if (partnerTypeSelect) {
        partnerTypeSelect.value = partnerTypeId != null ? String(partnerTypeId) : '';
      } else {
        // ha valamiért még input maradt, akkor fallback
        setValue('editPartnerTypeId', partnerTypeId);
      }

      // --- CONTACT ---
      setValue('editPartnerEmail', data.email);
      setValue('editPartnerPhone', data.phoneNumber);
      setValue('editPartnerAlternatePhone', data.alternatePhone);
      setValue('editPartnerWebsite', data.website);

      // --- ADDRESS ---
      setValue('editPartnerAddressLine1', data.addressLine1);
      setValue('editPartnerAddressLine2', data.addressLine2);
      setValue('editPartnerCity', data.city);
      setValue('editPartnerState', data.state);
      setValue('editPartnerPostalCode', data.postalCode);
      setValue('editPartnerCountry', data.country);

      // --- BUSINESS ---
      setValue('editPartnerTaxId', data.taxId);
      setValue('editIntTaxId', data.intTaxId);
      setValue('editIndividualTaxId', data.individualTaxId);
      setValue('editIndustry', data.industry);
      setValue('editPartnerStatus', data.statusId);

      // --- BILLING ---
      setValue('editPreferredCurrency', data.preferredCurrency);
      setValue('editPaymentTerms', data.paymentTerms);
      setValue('editCreditLimit', data.creditLimit);
      setValue('editBillingContactName', data.billingContactName);
      setValue('editBillingEmail', data.billingEmail);

      // --- NOTES / COMMENTS ---
      setValue('editPartnerNotes', data.notes);
      setValue('editComment1', data.comment1);
      setValue('editComment2', data.comment2);

      // --- READONLY LISTS (ha vannak konténerek) ---
      setContent('sites-edit-content',
        Array.isArray(data.sites) && data.sites.length
          ? data.sites.map(s => `<div class="alert alert-info mb-2">${escapeHtml(s.siteName || '–')} – ${escapeHtml(s.city || '')}</div>`).join('')
          : '<p class="text-muted">Nincsenek telephelyek.</p>'
      );

      setContent('contacts-edit-content',
        Array.isArray(data.contacts) && data.contacts.length
          ? data.contacts.map(c => `<div class="alert alert-secondary mb-2">${escapeHtml(((c.firstName || '') + ' ' + (c.lastName || '')).trim() || '–')} – ${escapeHtml(c.email || 'nincs email')}</div>`).join('')
          : '<p class="text-muted">Nincsenek kapcsolattartók.</p>'
      );

      setContent('documents-edit-content',
        Array.isArray(data.documents) && data.documents.length
          ? data.documents.map(d => {
              const href = d.filePath ? escapeAttr(d.filePath) : '#';
              const name = d.fileName ? escapeHtml(d.fileName) : 'Dokumentum';
              return `<div class="alert alert-light mb-2"><a href="${href}" target="_blank" rel="noopener">${name}</a></div>`;
            }).join('')
          : '<p class="text-muted">Nincsenek dokumentumok.</p>'
      );

    } catch (err) {
      console.error('Edit betöltési hiba:', err);
      window.c92?.showToast?.('error', 'Nem sikerült betölteni a partnert szerkesztésre');
    }
  });

  /* ================== SAVE (AJAX PUT) ================== */

  if (editForm) {
    editForm.addEventListener('submit', async function (e) {
      e.preventDefault();

      const formData = new FormData(this);

      const partnerIdRaw = formData.get('PartnerId');
      const partnerId = partnerIdRaw ? parseInt(String(partnerIdRaw), 10) : null;

      // checkboxok
      const isTaxExempt = document.getElementById('editIsTaxExempt')?.checked ?? false;
      const isActive = document.getElementById('editIsActive')?.checked ?? true;

      // datetime-local -> ISO (vagy null)
      const lastContactedRaw = (formData.get('LastContacted') || '').toString().trim();
      const lastContactedIso = lastContactedRaw ? new Date(lastContactedRaw).toISOString() : null;

      const partnerDto = {
        partnerId: partnerId,

        // REQUIRED
        name: (formData.get('Name') || '').toString().trim() || null,

        // BASIC
        companyName: (formData.get('CompanyName') || '').toString().trim() || null,
        shortName: (formData.get('ShortName') || '').toString().trim() || null,
        partnerCode: (formData.get('PartnerCode') || '').toString().trim() || null,
        ownId: (formData.get('OwnId') || '').toString().trim() || null,
        assignedTo: (formData.get('AssignedTo') || '').toString().trim() || null,
        lastContacted: lastContactedIso,
        isTaxExempt: isTaxExempt,
        isActive: isActive,

        // FK-k
        gfoId: formData.get('GFOId') ? Number(formData.get('GFOId')) : null,
        partnerGroupId: formData.get('PartnerGroupId') ? Number(formData.get('PartnerGroupId')) : null,
        partnerTypeId: formData.get('PartnerTypeId') ? Number(formData.get('PartnerTypeId')) : null,

        // CONTACT
        email: (formData.get('Email') || '').toString().trim() || null,
        phoneNumber: (formData.get('PhoneNumber') || '').toString().trim() || null,
        alternatePhone: (formData.get('AlternatePhone') || '').toString().trim() || null,
        website: (formData.get('Website') || '').toString().trim() || null,

        // TAX / BUSINESS
        taxId: (formData.get('TaxId') || '').toString().trim() || null,
        intTaxId: (formData.get('IntTaxId') || '').toString().trim() || null,
        individualTaxId: (formData.get('IndividualTaxId') || '').toString().trim() || null,
        industry: (formData.get('Industry') || '').toString().trim() || null,

        // ADDRESS
        addressLine1: (formData.get('AddressLine1') || '').toString().trim() || null,
        addressLine2: (formData.get('AddressLine2') || '').toString().trim() || null,
        city: (formData.get('City') || '').toString().trim() || null,
        state: (formData.get('State') || '').toString().trim() || null,
        postalCode: (formData.get('PostalCode') || '').toString().trim() || null,
        country: (formData.get('Country') || '').toString().trim() || null,

        // STATUS
        statusId: formData.get('StatusId') ? parseInt(String(formData.get('StatusId')), 10) : null,

        // BILLING
        preferredCurrency: (formData.get('PreferredCurrency') || '').toString().trim() || null,
        paymentTerms: (formData.get('PaymentTerms') || '').toString().trim() || null,
        creditLimit: formData.get('CreditLimit') ? Number(formData.get('CreditLimit')) : null,
        billingContactName: (formData.get('BillingContactName') || '').toString().trim() || null,
        billingEmail: (formData.get('BillingEmail') || '').toString().trim() || null,

        // NOTES / COMMENTS
        notes: (formData.get('Notes') || '').toString().trim() || null,
        comment1: (formData.get('Comment1') || '').toString().trim() || null,
        comment2: (formData.get('Comment2') || '').toString().trim() || null,

        // editnél ezt nem piszkáljuk
        sites: [],
        contacts: [],
        documents: []
      };

      if (!partnerDto.partnerId) {
        window.c92?.showToast?.('error', 'Hiba: Partner ID hiányzik');
        return;
      }

      if (!partnerDto.name) {
        window.c92?.showToast?.('error', 'A partner neve kötelező!');
        return;
      }

      try {
        const response = await fetch(`/api/Partners/${encodeURIComponent(String(partnerDto.partnerId))}`, {
          method: 'PUT',
          credentials: 'same-origin',
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
          },
          body: JSON.stringify(partnerDto)
        });

        const payload = await response.json().catch(() => ({}));

        if (!response.ok) {
          window.c92?.showToast?.('error',
            payload.errors?.General?.[0] ||
            payload.title ||
            payload.message ||
            'Hiba a mentéskor'
          );
          return;
        }

        const updated = payload.data ?? payload;

        // 1) táblázat sor frissítése
        patchRow(updated);

        // 2) partnersIndex “központi” API, ha létezik
        window.c92?.partners?.patchRow?.(updated);

        // 3) szólunk más scriptnek is
        document.dispatchEvent(new CustomEvent('partners:changed', {
          detail: { action: 'updated', partner: updated }
        }));

        bootstrap.Modal.getInstance(modalEl)?.hide();
      } catch (err) {
        console.error('Edit mentési hiba:', err);
        window.c92?.showToast?.('error', 'Hálózati hiba');
      }
    });
  }

  /* ================== TABLE ROW PATCH ================== */

  function patchRow(p) {
    const id = p.partnerId ?? p.PartnerId;
    if (!id) return;

    const tr = document.querySelector(`tr[data-partner-id="${CSS.escape(String(id))}"]`);
    if (!tr) return;

    const tds = tr.querySelectorAll('td');
    if (tds.length < 13) return;

    const name = p.name ?? p.Name ?? '—';
    const email = p.email ?? p.Email ?? '—';
    const phone = p.phoneNumber ?? p.PhoneNumber ?? '—';
    const taxId = p.taxId ?? p.TaxId ?? '—';

    const addressLine1 = p.addressLine1 ?? p.AddressLine1 ?? '';
    const addressLine2 = p.addressLine2 ?? p.AddressLine2 ?? '';
    const city = p.city ?? p.City ?? '';
    const state = p.state ?? p.State ?? '';
    const postalCode = p.postalCode ?? p.PostalCode ?? '';

    const statusObj = p.status ?? p.Status;
    const statusName = (typeof statusObj === 'string'
      ? statusObj
      : statusObj?.name ?? statusObj?.Name) ?? 'N/A';

    const statusColor = (statusObj?.color ?? statusObj?.Color) ?? '#6c757d';
    const statusTextColor = normalizeTextColor(statusColor);

    const preferredCurrency = p.preferredCurrency ?? p.PreferredCurrency ?? '';
    const assignedTo = p.assignedTo ?? p.AssignedTo ?? '';

    tds[0].textContent = name;
    tds[1].textContent = email;
    tds[2].textContent = phone;
    tds[3].textContent = taxId;

    tds[4].textContent = addressLine1;
    tds[5].textContent = addressLine2;
    tds[6].textContent = city;
    tds[7].textContent = state;
    tds[8].textContent = postalCode;

    if (tds[9]) {
      tds[9].innerHTML = `
        <span class="badge" style="background:${escapeAttr(statusColor)};color:${escapeAttr(statusTextColor)}">
          ${escapeHtml(statusName)}
        </span>
      `;
    }

    if (tds[10]) tds[10].textContent = preferredCurrency;
    if (tds[11]) tds[11].textContent = assignedTo;
  }

  /* ================== UTILS ================== */

  function toDateTimeLocal(v) {
    if (!v) return '';
    const d = new Date(v);
    if (isNaN(d.getTime())) return '';
    const pad = (n) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  function normalizeTextColor(bgHex) {
    const c = String(bgHex || '').toLowerCase();
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
});
