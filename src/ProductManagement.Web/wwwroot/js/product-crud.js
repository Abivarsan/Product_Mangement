/**
 * Product Management CRUD Module
 */

$(document).ready(function () {
    // Cache common DOM elements
    const $productModal = new bootstrap.Modal(document.getElementById('productModal'));
    const $deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
    const $productForm = $('#productForm');
    const $modalAlert = $('#modalAlert');
    const $searchInput = $('#searchInput');
    const $categoryFilter = $('#categoryFilter');
    const $productsTableBody = $('#productsTableBody');
    const $btnSaveProduct = $('#btnSaveProduct');
    const $saveSpinner = $('#saveSpinner');
    const $saveIcon = $('#saveIcon');

    // Retrieve CSRF token from page meta tag
    const antiForgeryToken = $('meta[name="request-verification-token"]').attr('content');

    // Helper: Toast Notifications
    function showToast(message, isSuccess = true, headerTitle = 'System Notification') {
        const $toast = $('#liveToast');
        const $toastMessage = $('#toastMessage');

        $toast.removeClass('bg-success bg-danger bg-warning text-white');
        if (isSuccess) {
            $toast.addClass('bg-success text-white');
            $toastMessage.html(`<i class="bi bi-check-circle-fill fs-5"></i> <div><strong>${headerTitle}</strong><div class="small">${message}</div></div>`);
        } else {
            $toast.addClass('bg-danger text-white');
            $toastMessage.html(`<i class="bi bi-exclamation-triangle-fill fs-5"></i> <div><strong>${headerTitle}</strong><div class="small">${message}</div></div>`);
        }

        const toast = new bootstrap.Toast($toast[0], { delay: 4000 });
        toast.show();
    }

    // Helper: Reset Modal Form
    function resetForm() {
        $productForm[0].reset();
        $productForm.removeClass('was-validated');
        $('#productId').val('0');
        $modalAlert.addClass('d-none').empty();
        $('#productModalLabel').html('<i class="bi bi-box-seam me-2 text-primary"></i>Add New Product');
    }

    // 1. Open "Add Product" Modal
    $('#btnOpenAddModal').on('click', function () {
        resetForm();
        $productModal.show();
    });

    // 2. Open "Edit Product" Modal via AJAX
    $(document).on('click', '.btn-edit', function () {
        const productId = $(this).data('id');
        resetForm();

        // Fetch product details via AJAX
        $.ajax({
            url: `/Product/GetById?id=${productId}`,
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.IsSuccess && response.Data) {
                    const item = response.Data;
                    $('#productId').val(item.Id);
                    $('#productName').val(item.Name);
                    $('#productCategory').val(item.Category);
                    $('#productPrice').val(item.Price);
                    $('#productStock').val(item.Stock);

                    $('#productModalLabel').html(`<i class="bi bi-pencil-square me-2 text-primary"></i>Edit Product #${item.Id}`);
                    $productModal.show();
                } else {
                    showToast(response.Message || 'Failed to load product.', false, 'Error');
                }
            },
            error: function () {
                showToast('Unable to connect to server to fetch product details.', false, 'Network Error');
            }
        });
    });

    // 3. Save Product (Create or Update) via AJAX
    $productForm.on('submit', function (e) {
        e.preventDefault();

        const form = this;
        if (!form.checkValidity()) {
            e.stopPropagation();
            $(form).addClass('was-validated');
            return;
        }

        const id = parseInt($('#productId').val()) || 0;
        const isEditing = id > 0;
        const targetUrl = isEditing ? '/Product/Update' : '/Product/Create';

        const payload = {
            Id: id,
            Name: $('#productName').val().trim(),
            Category: $('#productCategory').val().trim(),
            Price: parseFloat($('#productPrice').val()),
            Stock: parseInt($('#productStock').val())
        };

        // UI Loading State
        $btnSaveProduct.prop('disabled', true);
        $saveSpinner.removeClass('d-none');
        $saveIcon.addClass('d-none');
        $modalAlert.addClass('d-none').empty();

        $.ajax({
            url: targetUrl,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            headers: {
                'RequestVerificationToken': antiForgeryToken
            },
            success: function (response) {
                $btnSaveProduct.prop('disabled', false);
                $saveSpinner.addClass('d-none');
                $saveIcon.removeClass('d-none');

                $productModal.hide();

                // Display success toast with audit notice
                showToast(response.Message || 'Operation completed successfully.', true, 'Product Saved');

                // If low stock delegate triggered an alert, display an additional warning
                if (response.LowStockWarning) {
                    setTimeout(function () {
                        showToast(response.LowStockWarning, false, 'Stock Threshold Alert');
                    }, 1200);
                }

                // Refresh table to reflect fresh data and evicted cache
                setTimeout(function () {
                    window.location.reload();
                }, 800);
            },
            error: function (xhr) {
                $btnSaveProduct.prop('disabled', false);
                $saveSpinner.addClass('d-none');
                $saveIcon.removeClass('d-none');

                let errorText = 'An unexpected error occurred while saving.';
                if (xhr.responseJSON && xhr.responseJSON.Errors && xhr.responseJSON.Errors.length > 0) {
                    errorText = xhr.responseJSON.Errors.join('<br>');
                } else if (xhr.responseJSON && xhr.responseJSON.Message) {
                    errorText = xhr.responseJSON.Message;
                }

                $modalAlert.removeClass('d-none').html(errorText);
            }
        });
    });

    // 4. Open "Delete Product" Confirmation Modal
    $(document).on('click', '.btn-delete', function () {
        const id = $(this).data('id');
        const name = $(this).data('name');

        $('#deleteProductId').val(id);
        $('#deleteProductName').text(`"${name}" (ID #${id})`);
        $deleteModal.show();
    });

    // 5. Confirm Delete via AJAX
    $('#btnConfirmDelete').on('click', function () {
        const id = $('#deleteProductId').val();
        const $btn = $(this);
        const $spinner = $('#deleteSpinner');

        $btn.prop('disabled', true);
        $spinner.removeClass('d-none');

        $.ajax({
            url: `/Product/Delete?id=${id}`,
            type: 'POST',
            headers: {
                'RequestVerificationToken': antiForgeryToken
            },
            success: function (response) {
                $btn.prop('disabled', false);
                $spinner.addClass('d-none');
                $deleteModal.hide();

                showToast(response.Message || 'Product deleted.', true, 'Deleted');

                // Animate row removal smoothly
                $(`tr[data-id="${id}"]`).fadeOut(400, function () {
                    $(this).remove();
                    // Reload page briefly to update KPI counters
                    setTimeout(function () {
                        window.location.reload();
                    }, 500);
                });
            },
            error: function (xhr) {
                $btn.prop('disabled', false);
                $spinner.addClass('d-none');
                showToast('Failed to delete product.', false, 'Delete Error');
            }
        });
    });

    // 6. Live Search and Category Filtering (jQuery instant DOM filter)
    function applyFilter() {
        const searchTerm = $searchInput.val().toLowerCase().trim();
        const selectedCategory = $categoryFilter.val().toLowerCase().trim();

        let visibleCount = 0;

        $productsTableBody.find('tr[data-id]').each(function () {
            const $row = $(this);
            const name = $row.find('.product-name').text().toLowerCase();
            const category = $row.find('.product-category').text().toLowerCase();

            const matchesSearch = !searchTerm || name.includes(searchTerm) || category.includes(searchTerm);
            const matchesCategory = !selectedCategory || category === selectedCategory;

            if (matchesSearch && matchesCategory) {
                $row.show();
                visibleCount++;
            } else {
                $row.hide();
            }
        });

        // Show/hide empty state
        if (visibleCount === 0) {
            if ($('#noMatchRow').length === 0) {
                $productsTableBody.append(`
                    <tr id="noMatchRow">
                        <td colspan="7" class="text-center py-4 text-muted">
                            <i class="bi bi-search fs-3 d-block mb-1"></i>
                            No products match your current filter.
                        </td>
                    </tr>
                `);
            }
            $('#noMatchRow').show();
        } else {
            $('#noMatchRow').hide();
        }
    }

    $searchInput.on('keyup input', applyFilter);
    $categoryFilter.on('change', applyFilter);

    // Refresh Table Button
    $('#btnRefreshTable').on('click', function () {
        const $btn = $(this);
        $btn.addClass('rotate-animation');
        window.location.reload();
    });
});
