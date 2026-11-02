(function ($) {
    'use strict';

    var TodoApp = window.TodoApp || {};

    TodoApp.token = function () {
        return $('input[name="__RequestVerificationToken"]').first().val() || '';
    };

    TodoApp.escapeHtml = function (value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (character) {
            return {
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#39;'
            }[character];
        });
    };

    TodoApp.showToast = function (message, type) {
        var $stack = $('#toastStack');

        if (!$stack.length) {
            return;
        }

        var icon = type === 'error'
            ? '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="M12 8v4M12 16h.01" /></svg>'
            : '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m5 12 4 4L19 6" /></svg>';

        var $toast = $('<div class="toast-item"></div>')
            .addClass(type === 'error' ? 'is-error' : 'is-success');

        $('<span class="toast-icon" aria-hidden="true"></span>').html(icon).appendTo($toast);
        $('<span class="toast-message"></span>').text(message || '').appendTo($toast);
        $stack.append($toast);

        window.setTimeout(function () {
            $toast.fadeOut(180, function () {
                $toast.remove();
            });
        }, 4200);
    };

    TodoApp.post = function (url, data) {
        return $.ajax({
            url: url,
            type: 'POST',
            data: $.extend({}, data || {}, {
                __RequestVerificationToken: TodoApp.token()
            }),
            dataType: 'json',
            cache: false
        });
    };

    TodoApp.get = function (url, data) {
        return $.ajax({
            url: url,
            type: 'GET',
            data: data || {},
            dataType: 'json',
            cache: false
        });
    };

    TodoApp.setBusy = function ($button, busy) {
        if (!$button || !$button.length) {
            return;
        }

        var $label = $button.find('.btn-label');

        if (busy) {
            if (!$button.data('original-label') && $label.length) {
                $button.data('original-label', $label.text());
            }

            var loadingLabel = $button.data('loading-label');

            if (loadingLabel && $label.length) {
                $label.text(loadingLabel);
            }

            $button.prop('disabled', true).find('.spinner').prop('hidden', false);
            return;
        }

        $button.prop('disabled', false).find('.spinner').prop('hidden', true);

        var originalLabel = $button.data('original-label');

        if (originalLabel && $label.length) {
            $label.text(originalLabel);
        }
    };

    TodoApp.handleError = function (xhr, fallbackMessage) {
        if (xhr && xhr.status === 401) {
            return;
        }

        var message = fallbackMessage || 'Something went wrong. Please try again.';

        if (xhr && xhr.responseJSON && xhr.responseJSON.message) {
            message = xhr.responseJSON.message;
        }

        TodoApp.showToast(message, 'error');
    };

    TodoApp.confirm = function (options) {
        var $modal = $('#confirmModal');

        if (!$modal.length || typeof bootstrap === 'undefined') {
            if (window.confirm(options.message || options.title || 'Are you sure?')) {
                options.onConfirm();
            }

            return;
        }

        $modal.find('#confirmTitle').text(options.title || 'Are you sure?');
        $modal.find('#confirmMessage').text(options.message || '');

        var $ok = $modal.find('#confirmOk');
        $ok.text(options.confirmLabel || 'Confirm')
            .removeClass('btn-primary btn-danger')
            .addClass(options.destructive ? 'btn-danger' : 'btn-primary');

        $ok.off('click.todoConfirm').on('click.todoConfirm', function () {
            var instance = bootstrap.Modal.getInstance($modal[0]);

            if (instance) {
                instance.hide();
            }

            if (options.onConfirm) {
                options.onConfirm();
            }
        });

        bootstrap.Modal.getOrCreateInstance($modal[0]).show();
    };

    $(document).ajaxError(function (event, xhr) {
        if (xhr.status === 401) {
            var loginUrl = $('meta[name="login-url"]').attr('content') || '/Account/Login';
            window.location.href = loginUrl;
            return;
        }

        if (xhr.status === 403) {
            TodoApp.showToast('You do not have permission to do that.', 'error');
        }
    });

    window.TodoApp = TodoApp;
})(jQuery);
