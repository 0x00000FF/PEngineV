"use strict";

/**
 * Media Dialog for WYSIWYG Editor
 * Handles image, video, audio, and YouTube embedding with width control
 */
(function () {
    var currentEditor = null;
    var currentMediaType = null;
    var overlay = null;
    var urlInput = null;
    var widthSlider = null;
    var widthValue = null;
    var fileInput = null;

    function init() {
        // Listen for media dialog events from the editor
        document.addEventListener("wysiwyg:showMediaDialog", function (e) {
            showMediaDialog(e.detail.editor, e.detail.type);
        });

        // Listen for link dialog events
        document.addEventListener("wysiwyg:showLinkDialog", function (e) {
            showLinkDialog(e.detail.editor);
        });
    }

    function initUploadZone() {
        var uploadZone = document.getElementById("pe-media-upload-zone");
        var filePreview = document.getElementById("pe-media-file-preview");
        if (!uploadZone || !fileInput || !filePreview) return;

        // Drag and drop handlers
        uploadZone.addEventListener("dragover", function (e) {
            e.preventDefault();
            uploadZone.classList.add("pe-media-upload-zone-active");
        });

        uploadZone.addEventListener("dragleave", function () {
            uploadZone.classList.remove("pe-media-upload-zone-active");
        });

        uploadZone.addEventListener("drop", function (e) {
            e.preventDefault();
            uploadZone.classList.remove("pe-media-upload-zone-active");
            if (e.dataTransfer && e.dataTransfer.files.length > 0) {
                fileInput.files = e.dataTransfer.files;
                showFilePreview(e.dataTransfer.files[0]);
            }
        });

        // Click entire zone to browse
        uploadZone.addEventListener("click", function (e) {
            if (e.target.tagName === "LABEL" || e.target.closest("label")) {
                return;
            }
            fileInput.click();
        });

        // File input change handler
        fileInput.addEventListener("change", function () {
            if (fileInput.files && fileInput.files.length > 0) {
                showFilePreview(fileInput.files[0]);
            }
        });

        function showFilePreview(file) {
            filePreview.innerHTML = "";
            filePreview.hidden = false;
            uploadZone.hidden = true;

            var previewItem = document.createElement("div");
            previewItem.className = "pe-media-file-item";

            var icon = document.createElement("i");
            icon.className = "fa-solid " + getFileIcon(file.name) + " pe-media-file-icon";
            previewItem.appendChild(icon);

            var info = document.createElement("div");
            info.className = "pe-media-file-info";

            var name = document.createElement("div");
            name.className = "pe-media-file-name";
            name.textContent = file.name;
            info.appendChild(name);

            var size = document.createElement("div");
            size.className = "pe-media-file-size";
            size.textContent = formatFileSize(file.size);
            info.appendChild(size);

            previewItem.appendChild(info);

            var removeBtn = document.createElement("button");
            removeBtn.type = "button";
            removeBtn.className = "pe-media-file-remove";
            removeBtn.innerHTML = '<i class="fa-solid fa-times"></i>';
            removeBtn.addEventListener("click", function () {
                fileInput.value = "";
                filePreview.hidden = true;
                uploadZone.hidden = false;
            });
            previewItem.appendChild(removeBtn);

            filePreview.appendChild(previewItem);
        }

        function getFileIcon(fileName) {
            var ext = fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
            var iconMap = {
                '.pdf': 'fa-file-pdf',
                '.doc': 'fa-file-word', '.docx': 'fa-file-word',
                '.jpg': 'fa-file-image', '.jpeg': 'fa-file-image', '.png': 'fa-file-image',
                '.gif': 'fa-file-image', '.bmp': 'fa-file-image', '.svg': 'fa-file-image', '.webp': 'fa-file-image',
                '.mp4': 'fa-file-video', '.avi': 'fa-file-video', '.mov': 'fa-file-video',
                '.wmv': 'fa-file-video', '.flv': 'fa-file-video', '.webm': 'fa-file-video',
                '.mp3': 'fa-file-audio', '.wav': 'fa-file-audio', '.flac': 'fa-file-audio',
                '.aac': 'fa-file-audio', '.ogg': 'fa-file-audio',
                '.zip': 'fa-file-zipper', '.rar': 'fa-file-zipper', '.7z': 'fa-file-zipper'
            };
            return iconMap[ext] || 'fa-file';
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
    }

    function showMediaDialog(editor, type) {
        currentEditor = editor;
        currentMediaType = type;

        if (!overlay) {
            createOverlay();
        }

        resetDialog();
        updateDialogForType(type);
        overlay.hidden = false;
    }

    function createOverlay() {
        overlay = document.createElement("div");
        overlay.className = "pe-media-dialog-overlay";
        overlay.hidden = true;

        var content = document.createElement("div");
        content.className = "pe-media-dialog-content";

        var header = createHeader();
        var body = createBody();
        var footer = createFooter();

        content.appendChild(header);
        content.appendChild(body);
        content.appendChild(footer);

        overlay.appendChild(content);
        document.body.appendChild(overlay);

        // Close on backdrop click
        overlay.addEventListener("click", function (e) {
            if (e.target === overlay) {
                hideOverlay();
            }
        });

        // Initialize upload zone
        initUploadZone();
    }

    function createHeader() {
        var header = document.createElement("div");
        header.className = "pe-media-dialog-header";

        var title = document.createElement("h3");
        title.className = "pe-media-dialog-title";
        title.id = "pe-media-dialog-title";
        header.appendChild(title);

        var closeBtn = document.createElement("button");
        closeBtn.type = "button";
        closeBtn.className = "pe-media-dialog-close";
        closeBtn.innerHTML = '<i class="fa-solid fa-times"></i>';
        closeBtn.addEventListener("click", hideOverlay);
        header.appendChild(closeBtn);

        return header;
    }

    function createBody() {
        var body = document.createElement("div");
        body.className = "pe-media-dialog-body";

        // URL input section
        var urlSection = document.createElement("div");
        urlSection.id = "pe-media-url-section";

        var urlLabel = document.createElement("label");
        urlLabel.className = "pe-media-width-label";
        urlLabel.textContent = "URL";
        urlLabel.setAttribute("for", "pe-media-url-input");
        urlSection.appendChild(urlLabel);

        urlInput = document.createElement("input");
        urlInput.type = "text";
        urlInput.id = "pe-media-url-input";
        urlInput.className = "pe-media-url-input";
        urlInput.placeholder = "Enter URL...";
        urlSection.appendChild(urlInput);

        body.appendChild(urlSection);

        // File upload section with fancy upload zone
        var fileSection = document.createElement("div");
        fileSection.id = "pe-media-file-section";
        fileSection.hidden = true;

        var uploadZone = document.createElement("div");
        uploadZone.className = "pe-media-upload-zone";
        uploadZone.id = "pe-media-upload-zone";

        var uploadContent = document.createElement("div");
        uploadContent.className = "pe-media-upload-content";

        var uploadIcon = document.createElement("i");
        uploadIcon.className = "fa-solid fa-cloud-arrow-up pe-media-upload-icon";
        uploadContent.appendChild(uploadIcon);

        var uploadText = document.createElement("p");
        uploadText.className = "pe-media-upload-text";
        uploadText.textContent = "Drag & drop file here";
        uploadContent.appendChild(uploadText);

        var browseLabel = document.createElement("label");
        browseLabel.className = "pe-btn pe-btn-secondary pe-btn-sm pe-media-browse-label";
        browseLabel.setAttribute("for", "pe-media-file-input");
        browseLabel.textContent = "Browse";
        uploadContent.appendChild(browseLabel);

        fileInput = document.createElement("input");
        fileInput.type = "file";
        fileInput.id = "pe-media-file-input";
        fileInput.className = "pe-upload-input";
        uploadContent.appendChild(fileInput);

        uploadZone.appendChild(uploadContent);
        fileSection.appendChild(uploadZone);

        // File preview container
        var filePreview = document.createElement("div");
        filePreview.id = "pe-media-file-preview";
        filePreview.className = "pe-media-file-preview";
        filePreview.hidden = true;
        fileSection.appendChild(filePreview);

        body.appendChild(fileSection);

        // Width control section
        var widthSection = document.createElement("div");
        widthSection.className = "pe-media-width-control";

        var widthLabel = document.createElement("label");
        widthLabel.className = "pe-media-width-label";
        widthLabel.textContent = "Width";
        widthSection.appendChild(widthLabel);

        var sliderContainer = document.createElement("div");

        widthSlider = document.createElement("input");
        widthSlider.type = "range";
        widthSlider.min = "1";
        widthSlider.max = "100";
        widthSlider.value = "100";
        widthSlider.className = "pe-media-width-slider";
        sliderContainer.appendChild(widthSlider);

        widthValue = document.createElement("span");
        widthValue.className = "pe-media-width-value";
        widthValue.textContent = "100%";
        sliderContainer.appendChild(widthValue);

        widthSection.appendChild(sliderContainer);
        body.appendChild(widthSection);

        // Update width value display
        widthSlider.addEventListener("input", function () {
            widthValue.textContent = this.value + "%";
        });

        return body;
    }

    function createFooter() {
        var footer = document.createElement("div");
        footer.className = "pe-media-dialog-footer";

        var cancelBtn = document.createElement("button");
        cancelBtn.type = "button";
        cancelBtn.className = "pe-btn pe-btn-secondary";
        cancelBtn.textContent = "Cancel";
        cancelBtn.addEventListener("click", hideOverlay);

        var insertBtn = document.createElement("button");
        insertBtn.type = "button";
        insertBtn.id = "pe-media-insert-btn";
        insertBtn.className = "pe-btn pe-btn-primary";
        insertBtn.textContent = "Insert";
        insertBtn.addEventListener("click", handleInsert);

        footer.appendChild(cancelBtn);
        footer.appendChild(insertBtn);

        return footer;
    }

    function updateDialogForType(type) {
        var title = document.getElementById("pe-media-dialog-title");
        var urlSection = document.getElementById("pe-media-url-section");
        var fileSection = document.getElementById("pe-media-file-section");

        var titles = {
            "image": "Insert Image",
            "video": "Insert Video",
            "audio": "Insert Audio",
            "youtube": "Insert YouTube Video",
            "link": "Insert Link"
        };

        var placeholders = {
            "image": "Enter image URL...",
            "video": "Enter video URL...",
            "audio": "Enter audio URL...",
            "youtube": "Enter YouTube URL or video ID...",
            "link": "Enter link URL..."
        };

        var accepts = {
            "image": "image/*",
            "video": "video/*",
            "audio": "audio/*"
        };

        title.textContent = titles[type] || "Insert Media";
        urlInput.placeholder = placeholders[type] || "Enter URL...";

        // Hide URL input for image/video/audio, show only file upload
        if (type === "image" || type === "video" || type === "audio") {
            urlSection.hidden = true;
            fileSection.hidden = false;
            fileInput.accept = accepts[type] || "*/*";
        } else if (type === "youtube" || type === "link") {
            // Show URL input for YouTube and links, hide file upload
            urlSection.hidden = false;
            fileSection.hidden = true;
        } else {
            // Default: show both
            urlSection.hidden = false;
            fileSection.hidden = false;
            fileInput.accept = "*/*";
        }

        // Hide width control for links
        if (type === "link") {
            widthSlider.parentElement.parentElement.hidden = true;
        } else {
            widthSlider.parentElement.parentElement.hidden = false;
        }
    }

    function resetDialog() {
        urlInput.value = "";
        widthSlider.value = "100";
        widthValue.textContent = "100%";
        if (fileInput) {
            fileInput.value = "";
        }

        // Reset upload zone
        var uploadZone = document.getElementById("pe-media-upload-zone");
        var filePreview = document.getElementById("pe-media-file-preview");
        if (uploadZone && filePreview) {
            uploadZone.hidden = false;
            filePreview.hidden = true;
            filePreview.innerHTML = "";
        }
    }

    function handleInsert() {
        var url = urlInput.value.trim();
        var width = parseInt(widthSlider.value);
        var hasFile = fileInput && fileInput.files && fileInput.files.length > 0;

        // Check if file was uploaded instead
        if (hasFile) {
            // For file uploads, we need to create a local URL
            var file = fileInput.files[0];
            url = URL.createObjectURL(file);

            // Note: In a real implementation, you'd want to upload this file to the server
            // and get a permanent URL. This is just for preview purposes.
            showToast("Note: File upload requires server integration. Using temporary preview URL.", "warning");
        }

        // Validate that we have either URL or file
        if (!url && !hasFile) {
            showToast("Please enter a URL or select a file", "error");
            return;
        }

        if (currentMediaType === "youtube") {
            url = parseYouTubeUrl(url);
            if (!url) {
                showToast("Invalid YouTube URL", "error");
                return;
            }
        }

        if (currentMediaType === "link") {
            insertLink(url);
        } else {
            currentEditor.insertMedia(currentMediaType, url, width);
        }

        hideOverlay();
    }

    function parseYouTubeUrl(input) {
        // Extract video ID from various YouTube URL formats
        var videoId = null;

        // Check if it's already just a video ID
        if (/^[a-zA-Z0-9_-]{11}$/.test(input)) {
            videoId = input;
        }
        // Standard watch URL
        else if (input.includes("youtube.com/watch")) {
            var match = input.match(/[?&]v=([a-zA-Z0-9_-]{11})/);
            if (match) videoId = match[1];
        }
        // Short URL
        else if (input.includes("youtu.be/")) {
            var match = input.match(/youtu\.be\/([a-zA-Z0-9_-]{11})/);
            if (match) videoId = match[1];
        }
        // Embed URL
        else if (input.includes("youtube.com/embed/")) {
            var match = input.match(/embed\/([a-zA-Z0-9_-]{11})/);
            if (match) videoId = match[1];
        }

        if (videoId) {
            return "https://www.youtube.com/embed/" + videoId;
        }

        return null;
    }

    function insertLink(url) {
        if (!currentEditor) return;

        currentEditor.editor.focus();
        currentEditor.restoreSelection();

        var selection = window.getSelection();
        if (selection.rangeCount > 0) {
            var range = selection.getRangeAt(0);
            var selectedText = range.toString();

            if (selectedText.length > 0) {
                // Create link element
                var link = document.createElement("a");
                link.href = url;
                link.textContent = selectedText;

                range.deleteContents();
                range.insertNode(link);

                // Move cursor after link
                range.setStartAfter(link);
                range.setEndAfter(link);
                selection.removeAllRanges();
                selection.addRange(range);
            } else {
                // If no text selected, insert URL as link text
                var link = document.createElement("a");
                link.href = url;
                link.textContent = url;
                range.insertNode(link);

                range.setStartAfter(link);
                range.setEndAfter(link);
                selection.removeAllRanges();
                selection.addRange(range);
            }
        }

        currentEditor.saveSelection();

        // Trigger content change
        if (currentEditor.options.onContentChange) {
            currentEditor.options.onContentChange(currentEditor.getContent());
        }
    }

    function showLinkDialog(editor) {
        currentEditor = editor;
        currentMediaType = "link";

        if (!overlay) {
            createOverlay();
        }

        resetDialog();
        updateDialogForType("link");
        overlay.hidden = false;
    }

    function hideOverlay() {
        if (overlay) {
            overlay.hidden = true;
        }
        currentEditor = null;
        currentMediaType = null;
    }

    function showToast(message, type) {
        var container = document.querySelector('.pe-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'pe-toast-container';
            document.body.appendChild(container);
        }

        var toast = document.createElement('div');
        toast.className = 'pe-toast pe-toast-' + (type || 'info');
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(function () {
            toast.remove();
        }, 3000);
    }

    // Initialize on DOM ready
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
