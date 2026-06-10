(function () {
    const form = document.getElementById('filterForm');
    const searchBox = document.getElementById('searchBox');
    const fieldsHidden = document.getElementById('fieldsHidden');
    const pageHidden = document.getElementById('pageHidden');
    const pageSizeHidden = document.getElementById('pageSizeHidden');

    if (!form || !searchBox || !fieldsHidden || !pageHidden) return;

    const btnApplyFilter = document.getElementById('btnApplyFilter');
    if (btnApplyFilter) {
        btnApplyFilter.addEventListener('click', function () {
            const cols = Array.from(document.querySelectorAll('.filter-col:checked')).map(c => c.value);
            fieldsHidden.value = cols.join(',');
            pageHidden.value = 1;
            form.submit();
        });
    }

    searchBox.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            pageHidden.value = 1;
            form.submit();
        }
    });

    const btnRefresh = document.getElementById('btnRefresh');
    if (btnRefresh) {
        btnRefresh.addEventListener('click', function () {
            searchBox.value = '';
            fieldsHidden.value = ''; // pakai default di Controller kalau kosong
            pageHidden.value = 1;
            form.submit();
        });
    }
})();
