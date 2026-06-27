$(document).ready(function () {
    var orderItems = [];
    var subtotal = 0;
    var tax = 0;
    var serviceCharge = 0;
    var total = 0;

    // Add item to order
    $('.add-item-btn').click(function () {
        var itemId = parseInt($(this).data('item-id'));
        var itemName = $(this).data('item-name');
        var itemPrice = parseFloat($(this).data('item-price'));
        var quantity = parseInt($(this).closest('.d-flex').find('.quantity-input').val()) || 1;

        if (quantity <= 0) {
            showToast('Please select a quantity greater than 0', 'warning');
            return;
        }

        // Check if item already in order
        var existingItem = orderItems.find(function (item) {
            return item.menuItemId === itemId;
        });

        if (existingItem) {
            existingItem.quantity += quantity;
        } else {
            orderItems.push({
                menuItemId: itemId,
                name: itemName,
                price: itemPrice,
                quantity: quantity
            });
        }

        // Reset quantity input
        $(this).closest('.d-flex').find('.quantity-input').val(0);

        updateOrderSummary();
        updateOrderItemsList();
        showToast('Item added to order!', 'success');
    });

    // Remove item from order
    $(document).on('click', '.btn-remove-item', function () {
        var itemId = parseInt($(this).data('item-id'));
        orderItems = orderItems.filter(function (item) {
            return item.menuItemId !== itemId;
        });
        updateOrderSummary();
        updateOrderItemsList();
        showToast('Item removed from order', 'info');
    });

    // Update quantity
    $(document).on('change', '.item-quantity', function () {
        var itemId = parseInt($(this).data('item-id'));
        var newQuantity = parseInt($(this).val()) || 0;

        if (newQuantity <= 0) {
            // Remove item if quantity is 0 or negative
            orderItems = orderItems.filter(function (item) {
                return item.menuItemId !== itemId;
            });
        } else {
            var item = orderItems.find(function (item) {
                return item.menuItemId === itemId;
            });
            if (item) {
                item.quantity = newQuantity;
            }
        }

        updateOrderSummary();
        updateOrderItemsList();
    });

    // Update order summary
    function updateOrderSummary() {
        subtotal = 0;
        orderItems.forEach(function (item) {
            subtotal += item.price * item.quantity;
        });

        tax = subtotal * 0.06;
        serviceCharge = subtotal * 0.10;
        total = subtotal + tax + serviceCharge;

        $('#subtotal').text('RM ' + subtotal.toFixed(2));
        $('#tax').text('RM ' + tax.toFixed(2));
        $('#service-charge').text('RM ' + serviceCharge.toFixed(2));
        $('#total').text('RM ' + total.toFixed(2));

        // Enable/disable place order button
        $('#place-order-btn').prop('disabled', orderItems.length === 0);

        // Update hidden inputs
        updateHiddenInputs();
    }

    // Update order items list
    function updateOrderItemsList() {
        var container = $('#order-items-container');
        container.empty();

        if (orderItems.length === 0) {
            container.html('<p class="text-muted text-center">No items added yet</p>');
            return;
        }

        orderItems.forEach(function (item) {
            var html = `
                <div class="order-item">
                    <div class="item-info">
                        <div class="item-name">${item.name}</div>
                        <div class="item-details">
                            RM ${item.price.toFixed(2)} x ${item.quantity} = RM ${(item.price * item.quantity).toFixed(2)}
                        </div>
                    </div>
                    <div class="item-actions">
                        <input type="number" class="form-control form-control-sm item-quantity" 
                               data-item-id="${item.menuItemId}" value="${item.quantity}" min="0" max="20" style="width: 60px;" />
                        <button type="button" class="btn-remove-item" data-item-id="${item.menuItemId}">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                </div>
            `;
            container.append(html);
        });
    }

    // Update hidden inputs for form submission
    function updateHiddenInputs() {
        var dataContainer = $('#order-items-data');
        dataContainer.empty();

        orderItems.forEach(function (item, index) {
            dataContainer.append(`
                <input type="hidden" name="OrderItems[${index}].MenuItemId" value="${item.menuItemId}" />
                <input type="hidden" name="OrderItems[${index}].Quantity" value="${item.quantity}" />
                <input type="hidden" name="OrderItems[${index}].SpecialInstructions" value="" />
            `);
        });
    }

    // Toast notification
    function showToast(message, type) {
        var alertClass = 'alert-' + (type || 'info');
        var icon = type === 'success' ? 'fa-check-circle' :
            type === 'warning' ? 'fa-exclamation-triangle' :
                type === 'danger' ? 'fa-exclamation-circle' : 'fa-info-circle';

        var toast = $(`
            <div class="alert ${alertClass} alert-dismissible fade show position-fixed top-0 end-0 m-3" 
                 style="z-index: 9999; max-width: 400px;" role="alert">
                <i class="fas ${icon}"></i> ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `);

        $('body').append(toast);

        setTimeout(function () {
            toast.alert('close');
        }, 3000);
    }

    // Table selection change - check availability
    $('#TableId').change(function () {
        var tableId = $(this).val();
        if (tableId) {
            $.ajax({
                url: '/Order/GetTableStatus',
                type: 'GET',
                success: function (response) {
                    if (response.success) {
                        var table = response.data.find(function (t) {
                            return t.tableId == tableId;
                        });
                        if (table && table.isOccupied) {
                            showToast('Table is currently occupied. Please select another table.', 'warning');
                            $('#TableId').val('');
                        }
                    }
                }
            });
        }
    });

    // Keyboard shortcuts
    $(document).keydown(function (e) {
        // Ctrl+N: New order
        if (e.ctrlKey && e.key === 'n') {
            e.preventDefault();
            window.location.href = '/Order/Create';
        }
        // Ctrl+M: View menu
        if (e.ctrlKey && e.key === 'm') {
            e.preventDefault();
            window.location.href = '/Menu';
        }
    });
});