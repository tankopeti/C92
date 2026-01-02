// /wwwroot/js/Partner/advancedFilterTomSelect.js
// Advanced filter modal: TomSelect init + modal layer fix (dropdownParent: body)

document.addEventListener('DOMContentLoaded', () => {
    const modalEl = document.getElementById('advancedFilterModal');
    if (!modalEl) return;

    const statusSelect = document.getElementById('filterStatus');
    const typeSelect = document.getElementById('filterType');

    function initTomSelect(selectEl, placeholder) {
        if (!selectEl) return;

        // Ha nincs TomSelect core, nem tudunk mit csinálni
        if (!window.TomSelect) {
            console.warn('TomSelect core nincs betöltve. (tom-select.complete.min.js hiányzik?)');
            return;
        }

        // már initelve?
        if (selectEl.tomselect) return;

        new TomSelect(selectEl, {
            create: false,
            allowEmptyOption: true,
            closeAfterSelect: true,

            // ✅ a dropdown a body-ba kerül -> nem vágja le a modal, nem kerül mögé
            dropdownParent: 'body',

            placeholder: placeholder || '— Mindegy —'
        });
    }

    function refreshTomSelect(selectEl) {
        if (!selectEl?.tomselect) return;
        // ha loadStatuses.js később tölt be optionöket
        selectEl.tomselect.sync();
        selectEl.tomselect.refreshOptions(false);
    }

    // Modal látható -> ekkor initelj!
    modalEl.addEventListener('shown.bs.modal', () => {
        initTomSelect(statusSelect, '-- Mindegy --');
        initTomSelect(typeSelect, '-- Mindegy --');

        // ha opciók fetch után jönnek, frissítsünk késleltetve
        setTimeout(() => {
            refreshTomSelect(statusSelect);
            refreshTomSelect(typeSelect);
        }, 120);
    });

    // (opcionális) bezáráskor lehet destroy, de nem kötelező
    // modalEl.addEventListener('hidden.bs.modal', () => {
    //     [statusSelect, typeSelect].forEach(sel => sel?.tomselect?.destroy());
    // });
});
