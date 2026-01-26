// /js/Partner/createPartner.js – Új partner létrehozása (AJAX, reload nélkül) + GFO dropdown betöltés
document.addEventListener('DOMContentLoaded', () => {
  console.log('createPartner.js BETÖLTÖDÖTT – kész a létrehozásra');

  const btn = document.getElementById('saveNewPartnerBtn');
  if (!btn) return;

  const modalEl = document.getElementById('createPartnerModal');
  const form = document.getElementById('createPartnerForm');

    // --- PartnerType dropdown betöltés (PartnersController: GET /api/Partners/partnerTypes) ---
  const partnerTypeSelect = document.getElementById('createPartnerTypeId'); // <select id="createPartnerTypeId" name="PartnerTypeId">
  if (partnerTypeSelect) {
    (async () => {
      try {
        const res = await fetch('/api/Partners/partnerTypes', {
          credentials: 'same-origin',
          headers: { 'Accept': 'application/json' }
        });

        if (!res.ok) {
          const t = await res.text().catch(() => '');
          throw new Error(`HTTP ${res.status} ${t}`);
        }

        const data = await res.json();
        partnerTypeSelect.innerHTML =
          `<option value="">Válasszon...</option>` +
          (Array.isArray(data)
            ? data.map(x => `<option value="${Number(x.id)}">${(x.name ?? '').toString()}</option>`).join('')
            : '');

      } catch (e) {
        console.error('[createPartner] PartnerTypes load failed', e);
        window.c92?.showToast?.('error', 'Partner típus lista nem tölthető be');
      }
    })();
  }


  // --- GFO dropdown betöltés (PartnersController: GET /api/Partners/gfos) ---
  const gfoSelect = document.getElementById('createGfoId'); // <select id="createGfoId" name="GFOId">
  if (gfoSelect) {
    (async () => {
      try {
        const res = await fetch('/api/Partners/gfos', {
          credentials: 'same-origin',
          headers: { 'Accept': 'application/json' }
        });

        if (!res.ok) {
          const t = await res.text().catch(() => '');
          throw new Error(`HTTP ${res.status} ${t}`);
        }

        const data = await res.json();
        gfoSelect.innerHTML =
          `<option value="">Válasszon...</option>` +
          (Array.isArray(data)
            ? data.map(x => `<option value="${Number(x.id)}">${(x.name ?? '').toString()}</option>`).join('')
            : '');

      } catch (e) {
        console.error('[createPartner] GFO load failed', e);
        window.c92?.showToast?.('error', 'GFO lista nem tölthető be');
      }
    })();
  }

  // --- CREATE ---
  btn.addEventListener('click', async (e) => {
    e.preventDefault();

    if (!form) {
      console.error('createPartnerForm nem található');
      window.c92?.showToast?.('error', 'Hiba: Űrlap nem található');
      return;
    }

    if (!form.checkValidity()) {
      form.reportValidity();
      return;
    }

    const fd = new FormData(form);

    // datetime-local -> ISO (vagy null)
    const lastContactedRaw = (fd.get('LastContacted') || '').toString().trim();
    const lastContactedIso = lastContactedRaw ? new Date(lastContactedRaw).toISOString() : null;

    // checkboxok: FormData csak akkor küldi, ha checked
    const isTaxExempt = document.getElementById('createIsTaxExempt')?.checked ?? false;
    const isActive = document.getElementById('createIsActive')?.checked ?? true;

    // NOTE: GFO dropdown name="GFOId" legyen, különben fd.get('GFOId') null!
    const partnerDto = {
      partnerId: 0, // create-nél 0

      // alap
      name: (fd.get('Name') || '').toString().trim(),
      companyName: (fd.get('CompanyName') || '').toString().trim() || null,
      shortName: (fd.get('ShortName') || '').toString().trim() || null,
      partnerCode: (fd.get('PartnerCode') || '').toString().trim() || null,
      ownId: (fd.get('OwnId') || '').toString().trim() || null,

      // FK-k
      gfoId: fd.get('GFOId') ? Number(fd.get('GFOId')) : null,
      partnerGroupId: fd.get('PartnerGroupId') ? Number(fd.get('PartnerGroupId')) : null,
      partnerTypeId: fd.get('PartnerTypeId') ? Number(fd.get('PartnerTypeId')) : null,
      statusId: fd.get('StatusId') ? Number(fd.get('StatusId')) : null,

      // kontakt
      email: (fd.get('Email') || '').toString().trim() || null,
      phoneNumber: (fd.get('PhoneNumber') || '').toString().trim() || null,
      alternatePhone: (fd.get('AlternatePhone') || '').toString().trim() || null,
      website: (fd.get('Website') || '').toString().trim() || null,
      assignedTo: (fd.get('AssignedTo') || '').toString().trim() || null,
      lastContacted: lastContactedIso,

      // adó/üzlet
      taxId: (fd.get('TaxId') || '').toString().trim() || null,
      intTaxId: (fd.get('IntTaxId') || '').toString().trim() || null,
      individualTaxId: (fd.get('IndividualTaxId') || '').toString().trim() || null,
      industry: (fd.get('Industry') || '').toString().trim() || null,

      // cím
      addressLine1: (fd.get('AddressLine1') || '').toString().trim() || null,
      addressLine2: (fd.get('AddressLine2') || '').toString().trim() || null,
      city: (fd.get('City') || '').toString().trim() || null,
      state: (fd.get('State') || '').toString().trim() || null,
      postalCode: (fd.get('PostalCode') || '').toString().trim() || null,
      country: (fd.get('Country') || '').toString().trim() || 'Magyarország',

      // számlázás
      preferredCurrency: (fd.get('PreferredCurrency') || '').toString().trim() || null,
      paymentTerms: (fd.get('PaymentTerms') || '').toString().trim() || null,
      creditLimit: fd.get('CreditLimit') ? Number(fd.get('CreditLimit')) : null,
      billingContactName: (fd.get('BillingContactName') || '').toString().trim() || null,
      billingEmail: (fd.get('BillingEmail') || '').toString().trim() || null,

      // egyéb
      comment1: (fd.get('Comment1') || '').toString().trim() || null,
      comment2: (fd.get('Comment2') || '').toString().trim() || null,
      notes: (fd.get('Notes') || '').toString().trim() || null,

      isTaxExempt: isTaxExempt,
      isActive: isActive,

      // create-nél üresen
      sites: [],
      contacts: [],
      documents: []
    };

    if (!partnerDto.name) {
      window.c92?.showToast?.('error', 'A partner neve kötelező!');
      return;
    }

    btn.disabled = true;

    try {
      // --- POST helper: mindig logolható raw body-val ---
      const tryPost = async (url) => {
        const res = await fetch(url, {
          method: 'POST',
          credentials: 'same-origin',
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
          },
          body: JSON.stringify(partnerDto)
        });

        const contentType = res.headers.get('content-type') || '';
        const rawText = await res.text().catch(() => '');

        if (!res.ok) {
          let err = {};
          if (contentType.includes('application/json')) {
            try { err = JSON.parse(rawText || '{}'); } catch { /* ignore */ }
          } else {
            err = { message: rawText?.slice(0, 800) };
          }
          return { ok: false, status: res.status, err, rawText };
        }

        let created = null;
        if (contentType.includes('application/json')) {
          try { created = JSON.parse(rawText); } catch { created = null; }
        }
        return { ok: true, status: res.status, created, rawText };
      };

      // ✅ nálad már REST: POST /api/Partners
      const result = await tryPost('/api/Partners');

      if (!result.ok) {
        console.error('[createPartner] HTTP ERROR', result.status, result.err);
        if (result.rawText) console.error('[createPartner] RAW RESPONSE', result.rawText);

        const msg =
          result.err?.errors?.General?.[0] ||
          result.err?.title ||
          result.err?.message ||
          `Hiba a mentéskor (${result.status})`;

        window.c92?.showToast?.('error', msg);
        return;
      }

      window.c92?.showToast?.('success', 'Partner sikeresen létrehozva!');

      // modal bezár + form reset
      bootstrap.Modal.getInstance(modalEl)?.hide();
      form.reset();

      // listázó JS-nek szólunk (partnersIndex.js figyelheti)
      document.dispatchEvent(new CustomEvent('partners:changed', {
        detail: { action: 'created', partner: result.created }
      }));
    }
    catch (err) {
      console.error('[createPartner] Network/JS error:', err);
      window.c92?.showToast?.('error', 'Hálózati hiba');
    }
    finally {
      btn.disabled = false;
    }
  });
});
