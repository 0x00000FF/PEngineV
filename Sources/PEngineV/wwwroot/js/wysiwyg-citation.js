"use strict";

/**
 * Citation Management for WYSIWYG Editor
 * Manages citations/references for blog posts
 */
(function () {
    var citations = [];
    var overlay = null;
    var citationList = null;
    var editingIndex = null;

    function init() {
        createCitationUI();
    }

    function createCitationUI() {
        // Create citation management section
        var section = document.createElement("div");
        section.id = "pe-citation-section";
        section.className = "pe-form-group";

        var label = document.createElement("label");
        label.className = "pe-label";
        label.textContent = "Citations & References";
        section.appendChild(label);

        var toolbar = document.createElement("div");
        toolbar.className = "pe-citation-toolbar";

        var addBtn = document.createElement("button");
        addBtn.type = "button";
        addBtn.className = "pe-btn pe-btn-secondary pe-btn-sm";
        addBtn.innerHTML = '<i class="fa-solid fa-plus"></i> Add Citation';
        addBtn.addEventListener("click", function () {
            showCitationDialog();
        });
        toolbar.appendChild(addBtn);

        section.appendChild(toolbar);

        citationList = document.createElement("div");
        citationList.className = "pe-citation-list";
        section.appendChild(citationList);

        // Hidden input to store citations as JSON
        var hiddenInput = document.createElement("input");
        hiddenInput.type = "hidden";
        hiddenInput.id = "pe-citations-data";
        hiddenInput.name = "CitationsJson";
        section.appendChild(hiddenInput);

        // Initialize with empty state
        updateCitationList();

        return section;
    }

    function createCitationDialog() {
        overlay = document.createElement("div");
        overlay.className = "pe-media-dialog-overlay";
        overlay.hidden = true;

        var content = document.createElement("div");
        content.className = "pe-media-dialog-content";
        content.style.maxWidth = "600px";

        var header = document.createElement("div");
        header.className = "pe-media-dialog-header";

        var title = document.createElement("h3");
        title.className = "pe-media-dialog-title";
        title.id = "pe-citation-dialog-title";
        title.textContent = "Add Citation";
        header.appendChild(title);

        var closeBtn = document.createElement("button");
        closeBtn.type = "button";
        closeBtn.className = "pe-media-dialog-close";
        closeBtn.innerHTML = '<i class="fa-solid fa-times"></i>';
        closeBtn.addEventListener("click", hideCitationDialog);
        header.appendChild(closeBtn);

        content.appendChild(header);

        var body = document.createElement("div");
        body.className = "pe-media-dialog-body";

        // Title field
        var titleGroup = createFormGroup("Title", "citation-title", "text", true);
        body.appendChild(titleGroup);

        // Author field
        var authorGroup = createFormGroup("Author(s)", "citation-author", "text", false);
        body.appendChild(authorGroup);

        // URL field
        var urlGroup = createFormGroup("URL", "citation-url", "url", false);
        body.appendChild(urlGroup);

        // Publication Date field
        var dateGroup = createFormGroup("Publication Date", "citation-date", "date", false);
        body.appendChild(dateGroup);

        // Publisher field
        var publisherGroup = createFormGroup("Publisher", "citation-publisher", "text", false);
        body.appendChild(publisherGroup);

        // Notes field
        var notesGroup = document.createElement("div");
        notesGroup.className = "pe-form-group";
        var notesLabel = document.createElement("label");
        notesLabel.className = "pe-media-width-label";
        notesLabel.textContent = "Notes";
        notesLabel.setAttribute("for", "citation-notes");
        notesGroup.appendChild(notesLabel);
        var notesTextarea = document.createElement("textarea");
        notesTextarea.id = "citation-notes";
        notesTextarea.className = "pe-media-url-input";
        notesTextarea.rows = 3;
        notesTextarea.placeholder = "Additional notes...";
        notesGroup.appendChild(notesTextarea);
        body.appendChild(notesGroup);

        content.appendChild(body);

        var footer = document.createElement("div");
        footer.className = "pe-media-dialog-footer";

        var cancelBtn = document.createElement("button");
        cancelBtn.type = "button";
        cancelBtn.className = "pe-btn pe-btn-secondary";
        cancelBtn.textContent = "Cancel";
        cancelBtn.addEventListener("click", hideCitationDialog);
        footer.appendChild(cancelBtn);

        var saveBtn = document.createElement("button");
        saveBtn.type = "button";
        saveBtn.className = "pe-btn pe-btn-primary";
        saveBtn.id = "pe-citation-save-btn";
        saveBtn.textContent = "Add Citation";
        saveBtn.addEventListener("click", saveCitation);
        footer.appendChild(saveBtn);

        content.appendChild(footer);
        overlay.appendChild(content);

        // Close on backdrop click
        overlay.addEventListener("click", function (e) {
            if (e.target === overlay) {
                hideCitationDialog();
            }
        });

        return overlay;
    }

    function createFormGroup(labelText, id, type, required) {
        var group = document.createElement("div");
        group.className = "pe-form-group";

        var label = document.createElement("label");
        label.className = "pe-media-width-label";
        label.textContent = labelText + (required ? " *" : "");
        label.setAttribute("for", id);
        group.appendChild(label);

        var input = document.createElement("input");
        input.type = type;
        input.id = id;
        input.className = "pe-media-url-input";
        if (required) {
            input.required = true;
        }
        group.appendChild(input);

        return group;
    }

    function showCitationDialog(index) {
        if (!overlay) {
            overlay = createCitationDialog();
            if (!overlay) {
                showToast("Failed to create citation dialog", "error");
                return;
            }
            document.body.appendChild(overlay);
        }

        editingIndex = index !== undefined ? index : null;

        var title = document.getElementById("pe-citation-dialog-title");
        var saveBtn = document.getElementById("pe-citation-save-btn");

        if (!title || !saveBtn) {
            showToast("Citation dialog not properly initialized", "error");
            return;
        }

        if (editingIndex !== null) {
            title.textContent = "Edit Citation";
            saveBtn.textContent = "Update Citation";

            var citation = citations[editingIndex];
            document.getElementById("citation-title").value = citation.Title || citation.title || "";
            document.getElementById("citation-author").value = citation.Author || citation.author || "";
            document.getElementById("citation-url").value = citation.Url || citation.url || "";
            document.getElementById("citation-date").value = citation.Date || citation.date || "";
            document.getElementById("citation-publisher").value = citation.Publisher || citation.publisher || "";
            document.getElementById("citation-notes").value = citation.Notes || citation.notes || "";
        } else {
            title.textContent = "Add Citation";
            saveBtn.textContent = "Add Citation";
            resetCitationForm();
        }

        overlay.hidden = false;
    }

    function hideCitationDialog() {
        if (overlay) {
            overlay.hidden = true;
        }
        editingIndex = null;
        resetCitationForm();
    }

    function resetCitationForm() {
        document.getElementById("citation-title").value = "";
        document.getElementById("citation-author").value = "";
        document.getElementById("citation-url").value = "";
        document.getElementById("citation-date").value = "";
        document.getElementById("citation-publisher").value = "";
        document.getElementById("citation-notes").value = "";
    }

    function saveCitation() {
        var title = document.getElementById("citation-title").value.trim();

        if (!title) {
            showToast("Title is required", "error");
            return;
        }

        var citation = {
            Title: title,
            Author: document.getElementById("citation-author").value.trim(),
            Url: document.getElementById("citation-url").value.trim(),
            Date: document.getElementById("citation-date").value,
            Publisher: document.getElementById("citation-publisher").value.trim(),
            Notes: document.getElementById("citation-notes").value.trim()
        };

        if (editingIndex !== null) {
            citations[editingIndex] = citation;
        } else {
            citations.push(citation);
        }

        updateCitationList();
        updateHiddenInput();
        hideCitationDialog();
    }

    function updateCitationList() {
        if (!citationList) {
            citationList = document.getElementById("pe-citation-section")?.querySelector(".pe-citation-list");
            if (!citationList) {
                return;
            }
        }

        citationList.innerHTML = "";

        if (!citations || citations.length === 0) {
            var empty = document.createElement("p");
            empty.className = "pe-text-muted";
            empty.textContent = "No citations added yet.";
            citationList.appendChild(empty);
            return;
        }

        citations.forEach(function (citation, index) {
            var item = document.createElement("div");
            item.className = "pe-citation-item";

            var info = document.createElement("div");
            info.className = "pe-citation-info";

            var titleEl = document.createElement("div");
            titleEl.className = "pe-citation-title";
            titleEl.textContent = (index + 1) + ". " + (citation.Title || citation.title);
            info.appendChild(titleEl);

            var author = citation.Author || citation.author;
            if (author) {
                var authorEl = document.createElement("div");
                authorEl.className = "pe-citation-meta";
                authorEl.textContent = "Author: " + author;
                info.appendChild(authorEl);
            }

            var url = citation.Url || citation.url;
            if (url) {
                var urlEl = document.createElement("div");
                urlEl.className = "pe-citation-meta";
                var link = document.createElement("a");
                link.href = url;
                link.textContent = url;
                link.target = "_blank";
                link.rel = "noopener noreferrer";
                urlEl.appendChild(link);
                info.appendChild(urlEl);
            }

            item.appendChild(info);

            var actions = document.createElement("div");
            actions.className = "pe-citation-actions";

            var embedBtn = document.createElement("button");
            embedBtn.type = "button";
            embedBtn.className = "pe-btn-link";
            embedBtn.innerHTML = '<i class="fa-solid fa-quote-left"></i>';
            embedBtn.title = "Embed Citation";
            (function (idx) {
                embedBtn.addEventListener("click", function () {
                    embedCitation(idx);
                });
            })(index);
            actions.appendChild(embedBtn);

            var editBtn = document.createElement("button");
            editBtn.type = "button";
            editBtn.className = "pe-btn-link";
            editBtn.innerHTML = '<i class="fa-solid fa-edit"></i>';
            editBtn.title = "Edit";
            (function (idx) {
                editBtn.addEventListener("click", function () {
                    showCitationDialog(idx);
                });
            })(index);
            actions.appendChild(editBtn);

            var deleteBtn = document.createElement("button");
            deleteBtn.type = "button";
            deleteBtn.className = "pe-btn-link pe-text-danger";
            deleteBtn.innerHTML = '<i class="fa-solid fa-trash"></i>';
            deleteBtn.title = "Delete";
            (function (idx) {
                deleteBtn.addEventListener("click", function () {
                    deleteCitation(idx);
                });
            })(index);
            actions.appendChild(deleteBtn);

            item.appendChild(actions);
            citationList.appendChild(item);
        });
    }

    function deleteCitation(index) {
        citations.splice(index, 1);
        updateCitationList();
        updateHiddenInput();
    }

    function embedCitation(index) {
        var citationNumber = index + 1;

        // Dispatch event to insert into editor with citation data
        var event = new CustomEvent("wysiwyg:insertCitationRef", {
            detail: {
                index: index,
                number: citationNumber
            }
        });
        document.dispatchEvent(event);

        showToast("Citation reference inserted", "success");
    }

    function updateHiddenInput() {
        var input = document.getElementById("pe-citations-data");
        if (!input) {
            return;
        }
        input.value = JSON.stringify(citations || []);
    }

    function loadCitations(citationsJson) {
        try {
            if (!citationsJson || citationsJson === "" || citationsJson === "null" || citationsJson === "undefined") {
                citations = [];
            } else if (typeof citationsJson === "string") {
                citations = JSON.parse(citationsJson);
            } else if (Array.isArray(citationsJson)) {
                citations = citationsJson;
            } else {
                citations = [];
            }

            if (!Array.isArray(citations)) {
                citations = [];
            }

            updateCitationList();
            updateHiddenInput();
        } catch (e) {
            citations = [];
            updateCitationList();
            updateHiddenInput();
        }
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

    // Export functions
    window.WysiwygCitation = {
        init: init,
        loadCitations: loadCitations,
        createUI: createCitationUI
    };

    // Initialize on DOM ready
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
