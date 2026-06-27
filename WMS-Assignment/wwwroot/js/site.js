$(document).ready(function () {
    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        $('.alert').alert('close');
    }, 5000);

    // Confirm delete
    $('.confirm-delete').click(function (e) {
        if (!confirm('Are you sure you want to delete this item?')) {
            e.preventDefault();
        }
    });

    // Table row hover effect
    $('.table tbody tr').hover(
        function () {
            $(this).addClass('table-hover');
        },
        function () {
            $(this).removeClass('table-hover');
        }
    );

    // Tooltip initialization
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Popover initialization
    $('[data-bs-toggle="popover"]').popover();

    // Smooth scroll to top
    $('#back-to-top').click(function () {
        $('html, body').animate({ scrollTop: 0 }, 'slow');
        return false;
    });

    // Show/hide password toggle
    $('.toggle-password').click(function () {
        var input = $(this).closest('.input-group').find('input');
        var icon = $(this).find('i');

        if (input.attr('type') === 'password') {
            input.attr('type', 'text');
            icon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            input.attr('type', 'password');
            icon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    });

    // Print functionality
    $('.print-btn').click(function () {
        window.print();
    });

    // Export table to CSV
    $('.export-csv').click(function () {
        var table = $(this).closest('.table-responsive').find('table');
        if (table.length) {
            var csv = tableToCSV(table);
            downloadCSV(csv, 'export.csv');
        }
    });

    // Helper function: table to CSV
    function tableToCSV(table) {
        var rows = table.find('tr');
        var csv = [];

        rows.each(function () {
            var cols = $(this).find('td, th');
            var rowData = [];

            cols.each(function () {
                var text = $(this).text().trim();
                // Escape quotes and handle commas
                text = text.replace(/"/g, '""');
                if (text.includes(',') || text.includes('"') || text.includes('\n')) {
                    text = '"' + text + '"';
                }
                rowData.push(text);
            });

            csv.push(rowData.join(','));
        });

        return csv.join('\n');
    }

    // Helper function: download CSV
    function downloadCSV(csv, filename) {
        var blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
        var link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        link.click();
        URL.revokeObjectURL(link.href);
    }

    // Dark mode toggle (example)
    $('#dark-mode-toggle').click(function () {
        $('body').toggleClass('dark-mode');
        var icon = $(this).find('i');
        if ($('body').hasClass('dark-mode')) {
            icon.removeClass('fa-moon').addClass('fa-sun');
            localStorage.setItem('darkMode', 'true');
        } else {
            icon.removeClass('fa-sun').addClass('fa-moon');
            localStorage.setItem('darkMode', 'false');
        }
    });

    // Check saved dark mode preference
    if (localStorage.getItem('darkMode') === 'true') {
        $('body').addClass('dark-mode');
        $('#dark-mode-toggle i').removeClass('fa-moon').addClass('fa-sun');
    }

    // Handle AJAX errors globally
    $(document).ajaxError(function (event, xhr, settings, error) {
        console.error('AJAX Error:', error);
        showNotification('An error occurred. Please try again.', 'danger');
    });
});

// Custom notification function
function showNotification(message, type) {
    var alertClass = 'alert-' + (type || 'info');
    var icon = type === 'success' ? 'fa-check-circle' :
        type === 'warning' ? 'fa-exclamation-triangle' :
            type === 'danger' ? 'fa-exclamation-circle' : 'fa-info-circle';

    var notification = $(`
        <div class="alert ${alertClass} alert-dismissible fade show position-fixed top-0 end-0 m-3" 
             style="z-index: 9999; max-width: 400px;" role="alert">
            <i class="fas ${icon}"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `);

    $('body').append(notification);

    setTimeout(function () {
        notification.alert('close');
    }, 5000);
}