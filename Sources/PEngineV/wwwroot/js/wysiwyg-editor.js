"use strict";

/**
 * WYSIWYG Editor Component
 * CSP-compliant rich text editor for blog posts
 */
(function () {
    /**
     * Creates a new WYSIWYG editor instance
     * @param {HTMLElement} container - The container element for the editor
     * @param {Object} options - Configuration options
     */
    function WysiwygEditor(container, options) {
        this.container = container;
        this.options = Object.assign({
            placeholder: "Start writing...",
            initialContent: "",
            onContentChange: null
        }, options);

        this.toolbar = null;
        this.editor = null;
        this.currentRange = null;

        this.init();
    }

    WysiwygEditor.prototype.init = function () {
        this.createToolbar();
        this.createEditor();
        this.attachEventListeners();

        if (this.options.initialContent) {
            this.setContent(this.options.initialContent);
        }
    };

    WysiwygEditor.prototype.createToolbar = function () {
        var toolbar = document.createElement("div");
        toolbar.className = "pe-wysiwyg-toolbar";

        var groups = [
            this.createFormattingGroup(),
            this.createHeadingGroup(),
            this.createFontSizeGroup(),
            this.createColorGroup(),
            this.createMediaGroup(),
            this.createListGroup(),
            this.createLinkGroup()
        ];

        groups.forEach(function (group) {
            toolbar.appendChild(group);
        });

        this.container.appendChild(toolbar);
        this.toolbar = toolbar;
    };

    WysiwygEditor.prototype.createFormattingGroup = function () {
        var group = document.createElement("div");
        group.className = "pe-wysiwyg-toolbar-group";

        var buttons = [
            { command: "bold", icon: "fa-bold", title: "Bold (Ctrl+B)" },
            { command: "italic", icon: "fa-italic", title: "Italic (Ctrl+I)" },
            { command: "underline", icon: "fa-underline", title: "Underline (Ctrl+U)" },
            { command: "strikeThrough", icon: "fa-strikethrough", title: "Strikethrough" }
        ];

        var self = this;
        buttons.forEach(function (btn) {
            var button = self.createToolbarButton(btn.command, btn.icon, btn.title);
            group.appendChild(button);
        });

        return group;
    };

    WysiwygEditor.prototype.createHeadingGroup = function () {
        var group = document.createElement("div");
        group.className = "pe-wysiwyg-toolbar-group";

        var select = document.createElement("select");
        select.className = "pe-wysiwyg-toolbar-select";
        select.title = "Heading";

        var options = [
            { value: "", label: "-- Format --" },
            { value: "p", label: "Paragraph" },
            { value: "h1", label: "Heading 1" },
            { value: "h2", label: "Heading 2" },
            { value: "h3", label: "Heading 3" },
            { value: "h4", label: "Heading 4" },
            { value: "h5", label: "Heading 5" },
            { value: "h6", label: "Heading 6" }
        ];

        options.forEach(function (opt) {
            var option = document.createElement("option");
            option.value = opt.value;
            option.textContent = opt.label;
            select.appendChild(option);
        });

        var self = this;
        select.addEventListener("change", function () {
            if (this.value) {
                self.formatBlock(this.value);
                this.value = "";
            }
        });

        group.appendChild(select);
        return group;
    };

    WysiwygEditor.prototype.createFontSizeGroup = function () {
        var group = document.createElement("div");
        group.className = "pe-wysiwyg-toolbar-group";

        var select = document.createElement("select");
        select.className = "pe-wysiwyg-toolbar-select";
        select.title = "Font Size";

        var options = [
            { value: "", label: "-- Size --" },
            { value: "normal", label: "Normal" },
            { value: "xs", label: "X-Small" },
            { value: "sm", label: "Small" },
            { value: "lg", label: "Large" },
            { value: "xl", label: "X-Large" },
            { value: "xxl", label: "XX-Large" }
        ];

        options.forEach(function (opt) {
            var option = document.createElement("option");
            option.value = opt.value;
            option.textContent = opt.label;
            select.appendChild(option);
        });

        var self = this;
        select.addEventListener("change", function () {
            var size = this.value;
            if (size) {
                if (size === "normal") {
                    self.removeFontSize();
                } else {
                    self.applyFontSize(size);
                }
                this.value = "";
            }
        });

        group.appendChild(select);
        return group;
    };

    WysiwygEditor.prototype.createColorGroup = function () {
        var group = document.createElement("div");
        group.className = "pe-wysiwyg-toolbar-group";

        var foreColorBtn = this.createToolbarButton("foreColor", "fa-palette", "Text Color", true);
        var backColorBtn = this.createToolbarButton("backColor", "fa-fill-drip", "Background Color", true);

        group.appendChild(foreColorBtn);
        group.appendChild(backColorBtn);

        return group;
    };

    WysiwygEditor.prototype.createMediaGroup = function () {
        var group = document.createElement("div");
        group.className = "pe-wysiwyg-toolbar-group";

        var buttons = [
            { command: "insertImage", icon: "fa-image", title: "Insert Image" },
            { command: "insertVideo", icon: "fa-video", title: "Insert Video" },
            { command: "insertAudio", icon: "fa-music", title: "Insert Audio" },
            { command: "insertYoutube", icon: "fa-brands fa-youtube", title: "Insert YouTube" }
        ];

        var self = this;
        buttons.forEach(function (btn) {
            var button = self.createToolbarButton(btn.command, btn.icon, btn.title, true);
            group.appendChild(button);
        });

        return group;
    };

    WysiwygEditor.prototype.createListGroup = function () {
        var group = document.createElement("div");
        group.className = "pe-wysiwyg-toolbar-group";

        var buttons = [
            { command: "insertUnorderedList", icon: "fa-list-ul", title: "Bullet List" },
            { command: "insertOrderedList", icon: "fa-list-ol", title: "Numbered List" }
        ];

        var self = this;
        buttons.forEach(function (btn) {
            var button = self.createToolbarButton(btn.command, btn.icon, btn.title);
            group.appendChild(button);
        });

        return group;
    };

    WysiwygEditor.prototype.createLinkGroup = function () {
        var group = document.createElement("div");
        group.className = "pe-wysiwyg-toolbar-group";

        var linkBtn = this.createToolbarButton("createLink", "fa-link", "Insert Link", true);
        var unlinkBtn = this.createToolbarButton("unlink", "fa-link-slash", "Remove Link");

        group.appendChild(linkBtn);
        group.appendChild(unlinkBtn);

        return group;
    };

    WysiwygEditor.prototype.createToolbarButton = function (command, icon, title, customHandler) {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "pe-wysiwyg-toolbar-btn";
        button.title = title;
        button.setAttribute("data-command", command);

        var iconElement = document.createElement("i");
        iconElement.className = "fa-solid " + icon;
        button.appendChild(iconElement);

        var self = this;
        if (customHandler) {
            button.addEventListener("click", function (e) {
                e.preventDefault();
                self.handleCustomCommand(command);
            });
        } else {
            button.addEventListener("click", function (e) {
                e.preventDefault();
                self.execCommand(command);
            });
        }

        return button;
    };

    WysiwygEditor.prototype.createEditor = function () {
        var editorWrapper = document.createElement("div");
        editorWrapper.className = "pe-wysiwyg-editor-wrapper";

        var editor = document.createElement("div");
        editor.className = "pe-wysiwyg-editor";
        editor.contentEditable = "true";
        editor.setAttribute("role", "textbox");
        editor.setAttribute("aria-multiline", "true");
        editor.setAttribute("data-placeholder", this.options.placeholder);

        editorWrapper.appendChild(editor);
        this.container.appendChild(editorWrapper);
        this.editor = editor;
    };

    WysiwygEditor.prototype.attachEventListeners = function () {
        var self = this;

        // Save selection when editor loses focus
        this.editor.addEventListener("blur", function () {
            self.saveSelection();
        });

        // Track content changes
        this.editor.addEventListener("input", function () {
            if (self.options.onContentChange) {
                self.options.onContentChange(self.getContent());
            }
            self.updateToolbarState();
        });

        // Handle keyboard shortcuts
        this.editor.addEventListener("keydown", function (e) {
            self.handleKeyboardShortcut(e);
        });

        // Update toolbar state on selection change
        this.editor.addEventListener("mouseup", function () {
            self.updateToolbarState();
        });

        this.editor.addEventListener("keyup", function () {
            self.updateToolbarState();
        });

        // Listen for citation reference insertion
        document.addEventListener("wysiwyg:insertCitationRef", function (e) {
            self.editor.focus();
            self.restoreSelection();

            // Create the citation reference element in the editor's document context
            var citationRef = document.createElement("sup");
            citationRef.className = "pe-citation-ref";
            citationRef.textContent = "[" + e.detail.number + "]";
            citationRef.setAttribute("data-citation-index", e.detail.index);

            self.insertElement(citationRef);
            self.saveSelection();

            if (self.options.onContentChange) {
                self.options.onContentChange(self.getContent());
            }
        });
    };

    WysiwygEditor.prototype.handleKeyboardShortcut = function (e) {
        if (!e.ctrlKey && !e.metaKey) return;

        var commands = {
            "b": "bold",
            "i": "italic",
            "u": "underline"
        };

        var command = commands[e.key.toLowerCase()];
        if (command) {
            e.preventDefault();
            this.execCommand(command);
        }
    };

    WysiwygEditor.prototype.saveSelection = function () {
        var selection = window.getSelection();
        if (selection.rangeCount > 0) {
            this.currentRange = selection.getRangeAt(0);
        }
    };

    WysiwygEditor.prototype.restoreSelection = function () {
        if (this.currentRange) {
            var selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(this.currentRange);
        }
    };

    WysiwygEditor.prototype.execCommand = function (command, value) {
        this.editor.focus();
        this.restoreSelection();
        document.execCommand(command, false, value);
        this.saveSelection();
        this.updateToolbarState();
    };

    WysiwygEditor.prototype.formatBlock = function (tag) {
        this.editor.focus();
        this.restoreSelection();

        // For modern browsers, use formatBlock
        if (document.queryCommandSupported("formatBlock")) {
            document.execCommand("formatBlock", false, "<" + tag + ">");
        }

        this.saveSelection();
        this.updateToolbarState();
    };

    WysiwygEditor.prototype.handleCustomCommand = function (command) {
        var handlers = {
            "foreColor": this.showColorPicker.bind(this, "foreColor"),
            "backColor": this.showColorPicker.bind(this, "backColor"),
            "insertImage": this.showImageDialog.bind(this),
            "insertVideo": this.showVideoDialog.bind(this),
            "insertAudio": this.showAudioDialog.bind(this),
            "insertYoutube": this.showYoutubeDialog.bind(this),
            "createLink": this.showLinkDialog.bind(this)
        };

        var handler = handlers[command];
        if (handler) {
            handler();
        }
    };

    WysiwygEditor.prototype.showColorPicker = function (type) {
        // Emit custom event for color picker
        var event = new CustomEvent("wysiwyg:showColorPicker", {
            detail: { type: type, editor: this }
        });
        document.dispatchEvent(event);
    };

    WysiwygEditor.prototype.showImageDialog = function () {
        var event = new CustomEvent("wysiwyg:showMediaDialog", {
            detail: { type: "image", editor: this }
        });
        document.dispatchEvent(event);
    };

    WysiwygEditor.prototype.showVideoDialog = function () {
        var event = new CustomEvent("wysiwyg:showMediaDialog", {
            detail: { type: "video", editor: this }
        });
        document.dispatchEvent(event);
    };

    WysiwygEditor.prototype.showAudioDialog = function () {
        var event = new CustomEvent("wysiwyg:showMediaDialog", {
            detail: { type: "audio", editor: this }
        });
        document.dispatchEvent(event);
    };

    WysiwygEditor.prototype.showYoutubeDialog = function () {
        var event = new CustomEvent("wysiwyg:showMediaDialog", {
            detail: { type: "youtube", editor: this }
        });
        document.dispatchEvent(event);
    };

    WysiwygEditor.prototype.showLinkDialog = function () {
        var event = new CustomEvent("wysiwyg:showLinkDialog", {
            detail: { editor: this }
        });
        document.dispatchEvent(event);
    };

    WysiwygEditor.prototype.insertMedia = function (type, url, width) {
        this.editor.focus();
        this.restoreSelection();

        var element;
        var widthPercent = width || 100;

        if (type === "image") {
            element = document.createElement("img");
            element.src = url;
            element.alt = "Inserted image";
        } else if (type === "video") {
            element = document.createElement("video");
            element.src = url;
            element.controls = true;
        } else if (type === "audio") {
            element = document.createElement("audio");
            element.src = url;
            element.controls = true;
        } else if (type === "youtube") {
            var wrapper = document.createElement("div");
            wrapper.className = "pe-wysiwyg-embed-wrapper";
            wrapper.setAttribute("data-width", widthPercent);

            element = document.createElement("iframe");
            element.src = url;
            element.title = "YouTube video";
            element.setAttribute("frameborder", "0");
            element.setAttribute("allow", "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture");
            element.setAttribute("allowfullscreen", "");

            wrapper.appendChild(element);
            this.insertElement(wrapper);
            this.saveSelection();
            return;
        }

        element.className = "pe-wysiwyg-media";
        element.setAttribute("data-width", widthPercent);

        this.insertElement(element);
        this.saveSelection();
    };

    WysiwygEditor.prototype.insertElement = function (element) {
        var selection = window.getSelection();
        if (selection.rangeCount > 0) {
            var range = selection.getRangeAt(0);
            range.deleteContents();
            range.insertNode(element);

            // Move cursor after inserted element
            range.setStartAfter(element);
            range.setEndAfter(element);
            selection.removeAllRanges();
            selection.addRange(range);
        }
    };

    WysiwygEditor.prototype.applyFontSize = function (size) {
        this.editor.focus();
        this.restoreSelection();

        var selection = window.getSelection();
        if (selection.rangeCount > 0) {
            var range = selection.getRangeAt(0);
            var selectedText = range.toString();

            if (selectedText.length > 0) {
                // Wrap selected text in span with font size class
                var span = document.createElement("span");
                span.className = "pe-font-size-" + size;

                try {
                    range.surroundContents(span);
                } catch (e) {
                    // If surroundContents fails, use extractContents and insertNode
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

        this.saveSelection();

        if (this.options.onContentChange) {
            this.options.onContentChange(this.getContent());
        }
    };

    WysiwygEditor.prototype.removeFontSize = function () {
        this.editor.focus();
        this.restoreSelection();

        var selection = window.getSelection();
        if (selection.rangeCount > 0) {
            var range = selection.getRangeAt(0);
            var selectedText = range.toString();

            if (selectedText.length > 0) {
                // Handle selected text - remove font size classes from all spans in selection
                var container = range.commonAncestorContainer;

                // Create a temporary div to work with the selection
                var tempDiv = document.createElement("div");
                tempDiv.appendChild(range.cloneContents());

                // Remove font size classes from all spans in the selection
                var spans = tempDiv.querySelectorAll("span[class*='pe-font-size-']");
                for (var i = 0; i < spans.length; i++) {
                    var span = spans[i];
                    // Remove font size classes but keep other classes
                    var classes = span.className.split(" ").filter(function(cls) {
                        return cls.indexOf("pe-font-size-") === -1;
                    });
                    if (classes.length > 0) {
                        span.className = classes.join(" ");
                    } else {
                        // If no classes left, unwrap the span
                        var parent = span.parentNode;
                        while (span.firstChild) {
                            parent.insertBefore(span.firstChild, span);
                        }
                        parent.removeChild(span);
                    }
                }

                // Replace the selection with the cleaned content
                range.deleteContents();
                var frag = document.createDocumentFragment();
                var node;
                while ((node = tempDiv.firstChild)) {
                    frag.appendChild(node);
                }
                range.insertNode(frag);
            } else {
                // No selection - check if cursor is inside a font size span
                var parent = range.commonAncestorContainer;
                if (parent.nodeType === Node.TEXT_NODE) {
                    parent = parent.parentNode;
                }

                // Check if parent has font size class
                if (parent && parent.className && parent.className.indexOf("pe-font-size-") !== -1) {
                    // Remove font size classes but keep other classes
                    var classes = parent.className.split(" ").filter(function(cls) {
                        return cls.indexOf("pe-font-size-") === -1;
                    });
                    if (classes.length > 0) {
                        parent.className = classes.join(" ");
                    } else {
                        // If no classes left, unwrap the span
                        var parentNode = parent.parentNode;
                        while (parent.firstChild) {
                            parentNode.insertBefore(parent.firstChild, parent);
                        }
                        parentNode.removeChild(parent);
                    }
                }
            }
        }

        this.saveSelection();

        if (this.options.onContentChange) {
            this.options.onContentChange(this.getContent());
        }
    };

    WysiwygEditor.prototype.updateToolbarState = function () {
        var self = this;
        var buttons = this.toolbar.querySelectorAll(".pe-wysiwyg-toolbar-btn");

        buttons.forEach(function (button) {
            var command = button.getAttribute("data-command");

            // Skip custom commands
            if (["foreColor", "backColor", "insertImage", "insertVideo", "insertAudio", "insertYoutube", "createLink"].indexOf(command) !== -1) {
                return;
            }

            try {
                if (document.queryCommandState(command)) {
                    button.classList.add("pe-wysiwyg-toolbar-btn-active");
                } else {
                    button.classList.remove("pe-wysiwyg-toolbar-btn-active");
                }
            } catch (e) {
                // Some commands don't support queryCommandState
            }
        });
    };

    WysiwygEditor.prototype.getContent = function () {
        return this.editor.innerHTML;
    };

    WysiwygEditor.prototype.setContent = function (html) {
        this.editor.innerHTML = html;
    };

    WysiwygEditor.prototype.focus = function () {
        this.editor.focus();
    };

    // Export to global scope
    window.WysiwygEditor = WysiwygEditor;
})();
