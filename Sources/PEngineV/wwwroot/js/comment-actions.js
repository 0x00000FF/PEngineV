// Comment delete confirmation
(function () {
    'use strict';

    const deleteForms = document.querySelectorAll('.pe-delete-comment-form');
    const deleteOverlay = document.getElementById('comment-delete-overlay');
    const deleteClose = document.getElementById('comment-delete-close');
    const deleteCancel = document.getElementById('comment-delete-cancel');
    const deleteConfirm = document.getElementById('comment-delete-confirm');
    const overlayBackdrop = deleteOverlay ? deleteOverlay.querySelector('.pe-overlay-backdrop') : null;

    let currentForm = null;

    function showDeleteOverlay(form) {
        currentForm = form;
        deleteOverlay.hidden = false;
    }

    function hideDeleteOverlay() {
        deleteOverlay.hidden = true;
        currentForm = null;
    }

    function handleConfirm() {
        if (currentForm) {
            currentForm.removeEventListener('submit', handleSubmit);
            currentForm.submit();
        }
    }

    function handleSubmit(e) {
        e.preventDefault();
        showDeleteOverlay(e.target);
    }

    if (deleteOverlay && deleteClose && deleteCancel && deleteConfirm) {
        deleteForms.forEach(function (form) {
            form.addEventListener('submit', handleSubmit);
        });

        deleteClose.addEventListener('click', hideDeleteOverlay);
        deleteCancel.addEventListener('click', hideDeleteOverlay);
        deleteConfirm.addEventListener('click', handleConfirm);
        if (overlayBackdrop) {
            overlayBackdrop.addEventListener('click', hideDeleteOverlay);
        }
    }
})();
