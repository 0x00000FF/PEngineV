"use strict";

(function () {
    var currentFileData = null;

    function initFileDetailOverlay() {
        var overlay = document.getElementById("file-detail-overlay");
        var closeBtn = document.getElementById("file-detail-close");
        var cancelBtn = document.getElementById("file-detail-cancel");
        var downloadBtn = document.getElementById("file-detail-download");
        var verifyBtn = document.getElementById("file-detail-verify");
        var backdrop = overlay ? overlay.querySelector(".pe-overlay-backdrop") : null;

        if (!overlay) return;

        var nameEl = document.getElementById("file-detail-name");
        var typeEl = document.getElementById("file-detail-type");
        var sizeEl = document.getElementById("file-detail-size");
        var hashEl = document.getElementById("file-detail-hash");

        function showOverlay(data) {
            currentFileData = data;
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
            var btn = e.target.closest(".pe-attachment-info-btn");
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

        if (verifyBtn) {
            verifyBtn.addEventListener("click", function () {
                hideOverlay();
                showVerifyOverlay(currentFileData);
            });
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

    function initFileVerifyOverlay() {
        var overlay = document.getElementById("file-verify-overlay");
        var closeBtn = document.getElementById("file-verify-close");
        var doneBtn = document.getElementById("file-verify-done");
        var backBtn = document.getElementById("file-verify-back");
        var backdrop = overlay ? overlay.querySelector(".pe-overlay-backdrop") : null;
        var fileInput = document.getElementById("verify-file-input");

        if (!overlay) return;

        var currentExpectedHash = null;

        function hideOverlay() {
            overlay.hidden = true;
            document.body.style.overflow = "";
            if (fileInput) fileInput.value = "";
        }

        function goBackToFileDetail() {
            hideOverlay();
            var fileDetailOverlay = document.getElementById("file-detail-overlay");
            if (fileDetailOverlay) {
                fileDetailOverlay.hidden = false;
                document.body.style.overflow = "hidden";
            }
        }

        if (closeBtn) {
            closeBtn.addEventListener("click", hideOverlay);
        }

        if (doneBtn) {
            doneBtn.addEventListener("click", hideOverlay);
        }

        if (backBtn) {
            backBtn.addEventListener("click", goBackToFileDetail);
        }

        if (backdrop) {
            backdrop.addEventListener("click", hideOverlay);
        }

        if (fileInput) {
            fileInput.addEventListener("change", function () {
                if (fileInput.files.length > 0) {
                    verifySelectedFile(fileInput.files[0], currentExpectedHash);
                }
            });
        }

        window.setExpectedHash = function(hash) {
            currentExpectedHash = hash;
        };
    }

    function showVerifyOverlay(fileData) {
        var overlay = document.getElementById("file-verify-overlay");
        var progressSection = document.getElementById("verify-progress");
        var resultSection = document.getElementById("verify-result");
        var expectedHashEl = document.getElementById("verify-expected-hash");
        var computedHashEl = document.getElementById("verify-computed-hash");
        var successBox = document.getElementById("verify-success");
        var failureBox = document.getElementById("verify-failure");
        var fileInput = document.getElementById("verify-file-input");

        if (!overlay) return;

        // Reset state
        progressSection.hidden = true;
        resultSection.hidden = true;
        successBox.hidden = true;
        failureBox.hidden = true;
        if (fileInput) fileInput.value = "";
        if (computedHashEl) computedHashEl.textContent = "";

        expectedHashEl.textContent = fileData.hash;
        window.setExpectedHash(fileData.hash);

        overlay.hidden = false;
        document.body.style.overflow = "hidden";
    }

    function verifySelectedFile(file, expectedHash) {
        var progressSection = document.getElementById("verify-progress");
        var resultSection = document.getElementById("verify-result");
        var progressBar = document.getElementById("verify-progress-bar");
        var progressText = document.getElementById("verify-progress-text");
        var statusText = document.getElementById("verify-status-text");
        var computedHashEl = document.getElementById("verify-computed-hash");
        var successBox = document.getElementById("verify-success");
        var failureBox = document.getElementById("verify-failure");

        progressSection.hidden = false;
        resultSection.hidden = true;
        successBox.hidden = true;
        failureBox.hidden = true;
        progressBar.style.width = "0%";
        progressText.textContent = "0%";
        statusText.textContent = "Computing hash...";

        computeFileHash(file, function(progress) {
            progressBar.style.width = progress + "%";
            progressText.textContent = progress + "%";
        })
        .then(function(computedHash) {
            computedHashEl.textContent = computedHash;
            progressSection.hidden = true;
            resultSection.hidden = false;

            var match = computedHash.toLowerCase() === expectedHash.toLowerCase();
            if (match) {
                successBox.hidden = false;
                failureBox.hidden = true;
            } else {
                successBox.hidden = true;
                failureBox.hidden = false;
            }
        })
        .catch(function(error) {
            progressSection.hidden = true;
            resultSection.hidden = false;
            successBox.hidden = true;
            failureBox.hidden = false;
            computedHashEl.textContent = "Error: " + error.message;
        });
    }

    async function computeFileHash(file, onProgress) {
        var chunkSize = 1024 * 1024; // 1MB chunks
        var offset = 0;
        var chunks = [];

        while (offset < file.size) {
            var end = Math.min(offset + chunkSize, file.size);
            var chunk = file.slice(offset, end);
            var arrayBuffer = await chunk.arrayBuffer();
            chunks.push(new Uint8Array(arrayBuffer));

            offset = end;
            var progress = Math.round((offset / file.size) * 100);
            if (onProgress) onProgress(progress);

            await new Promise(function(resolve) { setTimeout(resolve, 0); });
        }

        // Combine all chunks
        var totalLength = chunks.reduce(function(sum, chunk) { return sum + chunk.length; }, 0);
        var combined = new Uint8Array(totalLength);
        var position = 0;
        for (var i = 0; i < chunks.length; i++) {
            combined.set(chunks[i], position);
            position += chunks[i].length;
        }

        var hashBuffer = await crypto.subtle.digest("SHA-256", combined.buffer);
        var hashArray = Array.from(new Uint8Array(hashBuffer));
        var computedHash = hashArray.map(function (b) {
            return b.toString(16).padStart(2, "0");
        }).join("");

        return computedHash;
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

    document.addEventListener("DOMContentLoaded", function () {
        initFileDetailOverlay();
        initFileVerifyOverlay();
    });
})();
