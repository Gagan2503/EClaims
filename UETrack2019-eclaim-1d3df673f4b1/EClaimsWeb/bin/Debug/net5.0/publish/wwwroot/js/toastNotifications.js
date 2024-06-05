(function ($) {
    'use strict';
    function SuccessToast(message) {
        $.toast({
            text: message,
            hideAfter: true,
            position: 'top-right',
            icon: 'success',
            allowToastClose: true,
            hideAfter: 5000,
        });
    }

    function FailureToast(message) {
        $.toast({
            text: message,
            hideAfter: true,
            position: 'top-right',
            icon: 'error',
            allowToastClose: true,
            hideAfter: 5000,
        });
    }
})(jQuery);