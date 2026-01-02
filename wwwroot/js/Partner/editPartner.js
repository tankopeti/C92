// /js/Partner/editPartner.js – Szerkesztés (AJAX PUT, reload nélkül)
document.addEventListener('DOMContentLoaded', function () {
  console.log('editPartner.js BETÖLTÖDÖTT – AJAX mentés (reload nélkül)');

  const modalEl = document.getElementById('editPartnerModal');
  if (!modalEl) {
    console.error('editPartnerModal nem található');
    return;
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

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();

    try {
      const response = await fetch(`/api/Partners/${encodeURIComponent(partnerId)}`, {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) throw new Error(response.status === 404 ? 'Partner nem található' : `HTTP ${response.status}`);

      const data = await response.json();

      // CSAK a létező mezőket töltjük ki (a te modalod alapján)
      const setValue = (id, value) => {
        const el = document.getElementById(id);
        if (el) el.value = value ?? '';
      };

      setValue('editPartnerId', data.partnerId);
      setValue('editPartnerName', data.name);
      setValue('editPartnerCompanyName', data.companyName);
      setValue('editPartnerEmail', data.email);
      setValue('editPartnerPhone', data.phoneNumber);
      setValue('editPartnerAlternatePhone', data.alternatePhone);
      setValue('editPartnerWebsite', data.website);
      setValue('editPartnerAddressLine1', data.addressLine1);
      setValue('editPartnerAddressLine2', data.addressLine2);
      setValue('editPartnerCity', data.city);
      setValue('editPartnerState', data.state);
      setValue('editPartnerPostalCode', data.postalCode);
      setValue('editPartnerCountry', data.country);
      setValue('editPartnerTaxId', data.taxId);
      setValue('editPartnerStatus', data.statusId);
      setValue('editPartnerNotes', data.notes);

      // Listák readonly megjelenítése (ha vannak konténerek)
      const setContent = (id, html) => {
        const el = document.getElementById(id);
        if (el) el.innerHTML = html;
      };

      setContent('sites-edit-content',
        data.sites?.length > 0
          ? data.sites.map(s => `<div class="alert alert-info mb-2">${escapeHtml(s.siteName)} – ${escapeHtml(s.city)}</div>`).join('')
          : '<p class="text-muted">Nincsenek telephelyek.</p>'
      );

      setContent('contacts-edit-content',
        data.contacts?.length > 0
          ? data.contacts.map(c => `<div class="alert alert-secondary mb-2">${escapeHtml((c.firstName || '') + ' ' + (c.lastName || ''))} – ${escapeHtml(c.email || 'nincs email')}</div>`).join('')
          : '<p class="text-muted">Nincsenek kapcsolattartók.</p>'
      );

      setContent('documents-edit-content',
        data.documents?.length > 0
          ? data.documents.map(d => `<div class="alert alert-light mb-2"><a href="${escapeAttr(d.filePath)}" target="_blank" rel="noopener">${escapeHtml(d.fileName)}</a></div>`).join('')
          : '<p class="text-muted">Nincsenek dokumentumok.</p>'
      );

    } catch (err) {
      console.error('Edit betöltési hiba:', err);
      window.c92?.showToast?.('error', 'Nem sikerült betölteni a partnert szerkesztésre');
    }
  });

  /* ================== SAVE (AJAX PUT) ================== */

  const editForm = document.getElementById('editPartnerForm');
  if (editForm) {
    editForm.addEventListener('submit', async function (e) {
      e.preventDefault();

      const formData = new FormData(this);

      // Fontos: a form field name-eket meghagyjuk, ahogy nálad vannak
      const partnerIdRaw = formData.get('PartnerId');
      const partnerId = partnerIdRaw ? parseInt(String(partnerIdRaw), 10) : null;

      const partnerDto = {
        partnerId: partnerId,
        name: formData.get('Name')?.trim() || null,
        companyName: formData.get('CompanyName')?.trim() || null,
        email: formData.get('Email')?.trim() || null,
        phoneNumber: formData.get('PhoneNumber')?.trim() || null,
        alternatePhone: formData.get('AlternatePhone')?.trim() || null,
        website: formData.get('Website')?.trim() || null,
        taxId: formData.get('TaxId')?.trim() || null,
        addressLine1: formData.get('AddressLine1')?.trim() || null,
        addressLine2: formData.get('AddressLine2')?.trim() || null,
        city: formData.get('City')?.trim() || null,
        state: formData.get('State')?.trim() || null,
        postalCode: formData.get('PostalCode')?.trim() || null,
        country: formData.get('Country')?.trim() || null,
        notes: formData.get('Notes')?.trim() || null,
        statusId: formData.get('StatusId') ? parseInt(String(formData.get('StatusId')), 10) : null
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
          window.c92?.showToast?.('error', payload.errors?.General?.[0] || payload.title || payload.message || 'Hiba a mentéskor');
          return;
        }

        // Kezeljük mindkét esetet:
        // A) API sima DTO-t ad vissza -> payload = { partnerId, name, ... }
        // B) API wrapper -> payload = { success, message, data: { ... } }
        const updated = payload.data ?? payload;

        patchRow(updated);

        // window.c92?.showToast?.('success', payload.message || 'Partner sikeresen frissítve!');
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

    // PartnerFilter addRow() szerinti oszlopok:
    // 0: name, 1: email, 2: phone, 3: taxId, 4..9 address, 10 status badge, 11 currency, 12 assignedTo, 13 actions
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

    // status badge oszlop (nálad a 9. / 10. környéke – a mintában 9 volt, de a konkrét index a táblától függhet)
    // A te addRow() alapján: status a 9-es indexen van (0..12-ig)
    // Nálad: <td>...postalCode</td> (index 8), <td><span class="badge"...>status</span></td> (index 9)
    if (tds[9]) {
      tds[9].innerHTML = `
        <span class="badge" style="background:${escapeAttr(statusColor)};color:${escapeAttr(statusTextColor)}">
          ${escapeHtml(statusName)}
        </span>
      `;
    }

    // currency (index 10 a te addRow alapján)
    if (tds[10]) tds[10].textContent = preferredCurrency;

    // assignedTo (index 11)
    if (tds[11]) tds[11].textContent = assignedTo;
  }

  /* ================== UTILS ================== */

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
