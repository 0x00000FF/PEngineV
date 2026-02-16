// Error page interactive functionality
(function () {
    'use strict';

    // Go back button handler
    const goBackBtn = document.getElementById('error-go-back');
    if (goBackBtn) {
        goBackBtn.addEventListener('click', function () {
            history.back();
        });
    }

    // Retry button handler
    const retryBtn = document.getElementById('error-retry');
    if (retryBtn) {
        retryBtn.addEventListener('click', function () {
            location.reload();
        });
    }
})();
