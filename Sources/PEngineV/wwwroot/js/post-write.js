"use strict";

(function () {
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
                input.files = e.dataTransfer.files;
                renderFileList(input.files);
            }
        });

        input.addEventListener("change", function () {
            renderFileList(input.files);
        });

        function renderFileList(files) {
            list.textContent = "";
            for (var i = 0; i < files.length; i++) {
                var item = document.createElement("div");
                item.className = "pe-upload-file-item";

                var name = document.createElement("span");
                name.className = "pe-upload-file-name";
                name.textContent = files[i].name;

                var size = document.createElement("span");
                size.className = "pe-upload-file-size";
                size.textContent = formatFileSize(files[i].size);

                item.appendChild(name);
                item.appendChild(size);
                list.appendChild(item);
            }
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

        container.addEventListener("click", function (e) {
            var btn = e.target.closest(".pe-delete-attachment");
            if (!btn) return;

            e.preventDefault();
            var attachmentId = btn.getAttribute("data-attachment-id");
            var postId = btn.getAttribute("data-post-id");
            var attachmentItem = btn.closest("[data-attachment-id]");

            if (!confirm("Delete this attachment?")) return;

            var token = document.querySelector('input[name="__RequestVerificationToken"]');
            var formData = new FormData();
            formData.append("id", attachmentId);
            formData.append("postId", postId);
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
                        attachmentItem.remove();
                        if (container.children.length === 0) {
                            container.closest(".pe-form-group").remove();
                        }
                    } else {
                        alert("Failed to delete attachment");
                    }
                })
                .catch(function (err) {
                    alert("Error: " + err.message);
                });
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initConditionalToggle();
        initUploadZone();
        initAttachmentDeletion();
    });
})();
