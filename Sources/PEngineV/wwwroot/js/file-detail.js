"use strict";

(function () {
    function initFileDetailOverlay() {
        var overlay = document.getElementById("file-detail-overlay");
        var closeBtn = document.getElementById("file-detail-close");
        var cancelBtn = document.getElementById("file-detail-cancel");
        var downloadBtn = document.getElementById("file-detail-download");
        var backdrop = overlay ? overlay.querySelector(".pe-overlay-backdrop") : null;

        if (!overlay) return;

        var nameEl = document.getElementById("file-detail-name");
        var typeEl = document.getElementById("file-detail-type");
        var sizeEl = document.getElementById("file-detail-size");
        var hashEl = document.getElementById("file-detail-hash");

        function showOverlay(data) {
            if (nameEl) nameEl.textContent = data.filename;
            if (typeEl) typeEl.textContent = data.contenttype;
            if (sizeEl) sizeEl.textContent = formatFileSize(data.filesize);
            if (hashEl) hashEl.textContent = data.hash;
            if (downloadBtn) {
                downloadBtn.href = data.downloadUrl;
                downloadBtn.download = data.filename;
            }

            overlay.hidden = false;
            document.body.style.overflow = "hidden";
        }

        function hideOverlay() {
            overlay.hidden = true;
            document.body.style.overflow = "";
        }

        document.addEventListener("click", function (e) {
            var btn = e.target.closest(".pe-attachment-detail-btn");
            if (!btn) return;

            e.preventDefault();
            var data = {
                filename: btn.getAttribute("data-filename"),
                contenttype: btn.getAttribute("data-contenttype"),
                filesize: parseInt(btn.getAttribute("data-filesize"), 10),
                hash: btn.getAttribute("data-hash"),
                downloadUrl: btn.getAttribute("data-download-url")
            };
            showOverlay(data);
        });

        if (closeBtn) {
            closeBtn.addEventListener("click", hideOverlay);
        }

        if (cancelBtn) {
            cancelBtn.addEventListener("click", hideOverlay);
        }

        if (backdrop) {
            backdrop.addEventListener("click", hideOverlay);
        }

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !overlay.hidden) {
                hideOverlay();
            }
        });
    }

    function formatFileSize(bytes) {
        var sizes = ["B", "KB", "MB", "GB"];
        var len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.length - 1) {
            order++;
            len /= 1024;
        }
        return len.toFixed(2) + " " + sizes[order];
    }

    document.addEventListener("DOMContentLoaded", initFileDetailOverlay);
})();
