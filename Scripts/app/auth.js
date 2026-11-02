(function ($) {
    'use strict';

    $(function () {
        $('.password-toggle').on('click', function () {
            var $button = $(this);
            var targetId = $button.data('target');
            var $input = $('#' + targetId);
            var show = $input.attr('type') === 'password';

            $input.attr('type', show ? 'text' : 'password');
            $button.attr('aria-pressed', show ? 'true' : 'false')
                .attr('aria-label', show ? 'Hide password' : 'Show password');
        });

        $('form.auth-form').on('submit', function () {
            var $form = $(this);

            if ($form.data('submitting')) {
                return false;
            }

            if (typeof $form.valid === 'function' && !$form.valid()) {
                return false;
            }

            $form.data('submitting', true);
            TodoApp.setBusy($form.find('button[type="submit"]'), true);
        });
    });
})(jQuery);
