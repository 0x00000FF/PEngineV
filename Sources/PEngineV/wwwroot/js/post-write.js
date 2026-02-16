"use strict";

(function () {
    // Simple toast notification utility
    function showToast(message, type) {
        var container = document.querySelector('.pe-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'pe-toast-container';
            document.body.appendChild(container);
        }

        var toast = document.createElement('div');
        toast.className = 'pe-toast pe-toast-' + (type || 'error');
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(function () {
            toast.remove();
        }, 3000);
    }

    function initConditionalToggle() {
        var checkboxes = document.querySelectorAll("[data-pe-toggle]");
        checkboxes.forEach(function (cb) {
            var targetId = cb.getAttribute("data-pe-toggle");
            var target = document.getElementById(targetId);
            if (!target) return;

            function update() {
                target.hidden = !cb.checked;
            }

            update();
            cb.addEventListener("change", update);
        });
    }

    function initUploadZone() {
        var zone = document.getElementById("pe-upload-zone");
        var input = document.getElementById("pe-file-input");
        var list = document.getElementById("pe-upload-list");
        if (!zone || !input || !list) return;

        var allFiles = [];

        function addFiles(newFiles) {
            for (var i = 0; i < newFiles.length; i++) {
                allFiles.push(newFiles[i]);
            }
            updateInputFiles();
            renderFileList();
        }

        function updateInputFiles() {
            var dataTransfer = new DataTransfer();
            for (var i = 0; i < allFiles.length; i++) {
                dataTransfer.items.add(allFiles[i]);
            }
            input.files = dataTransfer.files;
        }

        zone.addEventListener("dragover", function (e) {
            e.preventDefault();
            zone.classList.add("pe-upload-zone-active");
        });

        zone.addEventListener("dragleave", function () {
            zone.classList.remove("pe-upload-zone-active");
        });

        zone.addEventListener("drop", function (e) {
            e.preventDefault();
            zone.classList.remove("pe-upload-zone-active");
            if (e.dataTransfer && e.dataTransfer.files.length > 0) {
                addFiles(e.dataTransfer.files);
            }
        });

        input.addEventListener("change", function () {
            addFiles(input.files);
        });

        function getFileIcon(fileName) {
            var ext = fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
            switch (ext) {
                case '.pdf': return 'fa-file-pdf';
                case '.doc': case '.docx': return 'fa-file-word';
                case '.xls': case '.xlsx': return 'fa-file-excel';
                case '.ppt': case '.pptx': return 'fa-file-powerpoint';
                case '.jpg': case '.jpeg': case '.png': case '.gif': case '.bmp': case '.svg': case '.webp': return 'fa-file-image';
                case '.mp4': case '.avi': case '.mov': case '.wmv': case '.flv': case '.webm': return 'fa-file-video';
                case '.mp3': case '.wav': case '.flac': case '.aac': case '.ogg': return 'fa-file-audio';
                case '.zip': case '.rar': case '.7z': case '.tar': case '.gz': return 'fa-file-zipper';
                case '.txt': case '.md': case '.log': return 'fa-file-lines';
                case '.js': case '.ts': case '.jsx': case '.tsx': case '.json': return 'fa-file-code';
                case '.cs': case '.java': case '.py': case '.cpp': case '.c': case '.h': case '.go': case '.rs': return 'fa-file-code';
                case '.html': case '.css': case '.xml': case '.yml': case '.yaml': return 'fa-file-code';
                default: return 'fa-file';
            }
        }

        function renderFileList() {
            list.innerHTML = "";
            allFiles.forEach(function (file, index) {
                var item = document.createElement("div");
                item.className = "pe-upload-file-item";

                var icon = document.createElement("i");
                icon.className = "fa-solid " + getFileIcon(file.name) + " pe-upload-file-icon";

                var name = document.createElement("span");
                name.className = "pe-upload-file-name";
                name.textContent = file.name;

                var size = document.createElement("span");
                size.className = "pe-upload-file-size";
                size.textContent = formatFileSize(file.size);

                var removeBtn = document.createElement("button");
                removeBtn.type = "button";
                removeBtn.className = "pe-upload-file-remove";
                removeBtn.innerHTML = '<i class="fa-solid fa-times"></i>';
                (function(idx) {
                    removeBtn.addEventListener("click", function () {
                        allFiles.splice(idx, 1);
                        updateInputFiles();
                        renderFileList();
                    });
                })(index);

                item.appendChild(icon);
                item.appendChild(name);
                item.appendChild(size);
                item.appendChild(removeBtn);
                list.appendChild(item);
            });
        }
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

    function initAttachmentDeletion() {
        var container = document.getElementById("existing-attachments");
        if (!container) return;

        var deleteOverlay = document.getElementById("attachment-delete-overlay");
        var deleteClose = document.getElementById("attachment-delete-close");
        var deleteCancel = document.getElementById("attachment-delete-cancel");
        var deleteConfirm = document.getElementById("attachment-delete-confirm");
        var overlayBackdrop = deleteOverlay ? deleteOverlay.querySelector('.pe-overlay-backdrop') : null;

        var currentAttachmentId = null;
        var currentPostId = null;
        var currentAttachmentItem = null;

        function showDeleteOverlay(attachmentId, postId, attachmentItem) {
            currentAttachmentId = attachmentId;
            currentPostId = postId;
            currentAttachmentItem = attachmentItem;
            deleteOverlay.hidden = false;
        }

        function hideDeleteOverlay() {
            deleteOverlay.hidden = true;
            currentAttachmentId = null;
            currentPostId = null;
            currentAttachmentItem = null;
        }

        function handleDeleteConfirm() {
            if (!currentAttachmentId || !currentPostId) return;

            var token = document.querySelector('input[name="__RequestVerificationToken"]');
            var formData = new FormData();
            formData.append("id", currentAttachmentId);
            formData.append("postId", currentPostId);
            if (token) {
                formData.append("__RequestVerificationToken", token.value);
            }

            fetch("/Post/DeleteAttachmentAjax", {
                method: "POST",
                body: formData
            })
                .then(function (response) {
                    if (!response.ok) throw new Error("Failed to delete");
                    return response.json();
                })
                .then(function (data) {
                    if (data.success) {
                        currentAttachmentItem.remove();
                        if (container.children.length === 0) {
                            container.closest(".pe-form-group").remove();
                        }
                        hideDeleteOverlay();
                    } else {
                        hideDeleteOverlay();
                        showToast("Failed to delete attachment", "error");
                    }
                })
                .catch(function (err) {
                    hideDeleteOverlay();
                    showToast("Error: " + err.message, "error");
                });
        }

        container.addEventListener("click", function (e) {
            var btn = e.target.closest(".pe-delete-attachment");
            if (!btn) return;

            e.preventDefault();
            var attachmentId = btn.getAttribute("data-attachment-id");
            var postId = btn.getAttribute("data-post-id");
            var attachmentItem = btn.closest("[data-attachment-id]");

            showDeleteOverlay(attachmentId, postId, attachmentItem);
        });

        if (deleteOverlay && deleteClose && deleteCancel && deleteConfirm) {
            deleteClose.addEventListener("click", hideDeleteOverlay);
            deleteCancel.addEventListener("click", hideDeleteOverlay);
            deleteConfirm.addEventListener("click", handleDeleteConfirm);
            if (overlayBackdrop) {
                overlayBackdrop.addEventListener("click", hideDeleteOverlay);
            }
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        initConditionalToggle();
        initUploadZone();
        initAttachmentDeletion();
    });
})();
