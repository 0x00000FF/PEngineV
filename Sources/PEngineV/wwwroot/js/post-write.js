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

    document.addEventListener("DOMContentLoaded", function () {
        initConditionalToggle();
        initUploadZone();
    });
})();
