$(document).ready(function () {
    let debounceTimer;

    $('#searchBox').on('input', function () {
        clearTimeout(debounceTimer);
        const term = $(this).val().trim();

        debounceTimer = setTimeout(function () {
            if (term.length === 0) {
                $('#dropdownResults').empty().hide();
                return;
            }

            fetch(`/DocManagement/Search/LiveResults?searchTerm=${encodeURIComponent(term)}`)
                .then(response => response.text())
                .then(html => {
                    $('#dropdownResults').html(html).show();
                });
        }, 300);
    });

    // Optional: hide dropdown when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.search-wrapper').length) {
            $('#dropdownResults').hide();
        }
    });
});
