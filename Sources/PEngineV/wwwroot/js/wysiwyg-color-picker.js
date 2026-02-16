"use strict";

/**
 * Color Picker for WYSIWYG Editor
 * Manages 64-color palette for foreground and background colors
 */
(function () {
    // 64 color palette
    var colors = [
        "#000000", "#1a1a1a", "#333333", "#4d4d4d", "#666666", "#808080", "#999999", "#b3b3b3",
        "#cccccc", "#e6e6e6", "#ffffff", "#ff0000", "#ff3333", "#ff6666", "#ff9999", "#ffcccc",
        "#cc0000", "#990000", "#660000", "#330000", "#00ff00", "#33ff33", "#66ff66", "#99ff99",
        "#ccffcc", "#00cc00", "#009900", "#006600", "#003300", "#0000ff", "#3333ff", "#6666ff",
        "#9999ff", "#ccccff", "#0000cc", "#000099", "#000066", "#000033", "#ffff00", "#ffff33",
        "#ffff66", "#ffff99", "#ffffcc", "#cccc00", "#999900", "#666600", "#333300", "#ff00ff",
        "#ff33ff", "#ff66ff", "#ff99ff", "#ffccff", "#cc00cc", "#990099", "#660066", "#330033",
        "#00ffff", "#33ffff", "#66ffff", "#99ffff", "#ccffff", "#00cccc", "#009999", "#006666"
    ];

    var currentEditor = null;
    var currentType = null;
    var overlay = null;

    function init() {
        // Listen for color picker events from the editor
        document.addEventListener("wysiwyg:showColorPicker", function (e) {
            showColorPicker(e.detail.editor, e.detail.type);
        });
    }

    function showColorPicker(editor, type) {
        currentEditor = editor;
        currentType = type;

        if (!overlay) {
            createOverlay();
        }

        updateTitle();
        overlay.hidden = false;
    }

    function createOverlay() {
        overlay = document.createElement("div");
        overlay.className = "pe-color-picker-overlay";
        overlay.hidden = true;

        var content = document.createElement("div");
        content.className = "pe-color-picker-content";

        var header = createHeader();
        var grid = createColorGrid();
        var footer = createFooter();

        content.appendChild(header);
        content.appendChild(grid);
        content.appendChild(footer);

        overlay.appendChild(content);
        document.body.appendChild(overlay);

        // Close on backdrop click
        overlay.addEventListener("click", function (e) {
            if (e.target === overlay) {
                hideOverlay();
            }
        });
    }

    function createHeader() {
        var header = document.createElement("div");
        header.className = "pe-color-picker-header";

        var title = document.createElement("h3");
        title.className = "pe-color-picker-title";
        title.id = "pe-color-picker-title";
        header.appendChild(title);

        var closeBtn = document.createElement("button");
        closeBtn.type = "button";
        closeBtn.className = "pe-color-picker-close";
        closeBtn.innerHTML = '<i class="fa-solid fa-times"></i>';
        closeBtn.addEventListener("click", hideOverlay);
        header.appendChild(closeBtn);

        return header;
    }

    function createColorGrid() {
        var grid = document.createElement("div");
        grid.className = "pe-color-picker-grid";

        colors.forEach(function (color, index) {
            var swatch = document.createElement("button");
            swatch.type = "button";
            swatch.className = "pe-color-picker-swatch";
            swatch.style.backgroundColor = color;
            swatch.title = color;
            swatch.setAttribute("data-color", color);
            swatch.setAttribute("data-index", index);

            swatch.addEventListener("click", function () {
                applyColor(color, index);
            });

            grid.appendChild(swatch);
        });

        return grid;
    }

    function createFooter() {
        var footer = document.createElement("div");
        footer.className = "pe-media-dialog-footer";

        var cancelBtn = document.createElement("button");
        cancelBtn.type = "button";
        cancelBtn.className = "pe-btn pe-btn-secondary";
        cancelBtn.textContent = "Cancel";
        cancelBtn.addEventListener("click", hideOverlay);

        footer.appendChild(cancelBtn);
        return footer;
    }

    function updateTitle() {
        var title = document.getElementById("pe-color-picker-title");
        if (title) {
            title.textContent = currentType === "foreColor" ? "Text Color" : "Background Color";
        }
    }

    function applyColor(color, index) {
        if (!currentEditor) return;

        currentEditor.editor.focus();
        currentEditor.restoreSelection();

        // Apply color using CSS class for CSP compliance
        var selection = window.getSelection();
        if (selection.rangeCount > 0) {
            var range = selection.getRangeAt(0);
            var selectedText = range.toString();

            if (selectedText.length > 0) {
                // Wrap selected text in span with color class
                var span = document.createElement("span");
                var className = currentType === "foreColor" ? "pe-color-fg-" + index : "pe-color-bg-" + index;
                span.className = className;

                try {
                    range.surroundContents(span);
                } catch (e) {
                    // If surroundContents fails (e.g., partial element selection),
                    // use extractContents and insertNode instead
                    var contents = range.extractContents();
                    span.appendChild(contents);
                    range.insertNode(span);
                }

                // Move cursor after the span
                range.setStartAfter(span);
                range.setEndAfter(span);
                selection.removeAllRanges();
                selection.addRange(range);
            }
        }

        currentEditor.saveSelection();
        hideOverlay();

        // Trigger content change
        if (currentEditor.options.onContentChange) {
            currentEditor.options.onContentChange(currentEditor.getContent());
        }
    }

    function hideOverlay() {
        if (overlay) {
            overlay.hidden = true;
        }
        currentEditor = null;
        currentType = null;
    }

    // Initialize on DOM ready
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
