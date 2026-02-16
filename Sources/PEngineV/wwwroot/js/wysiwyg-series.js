"use strict";

/**
 * Series Management for WYSIWYG Editor
 * Manages post series/collections
 */
(function () {
    var seriesList = [];
    var selectedSeriesId = null;
    var overlay = null;

    function init() {
        // Listen for series dialog events
        document.addEventListener("wysiwyg:showSeriesDialog", function () {
            showSeriesDialog();
        });
    }

    function createSeriesUI() {
        var section = document.createElement("div");
        section.id = "pe-series-section";
        section.className = "pe-form-group";

        var label = document.createElement("label");
        label.className = "pe-label";
        label.textContent = "Series";
        label.setAttribute("for", "post-series");
        section.appendChild(label);

        var wrapper = document.createElement("div");
        wrapper.className = "pe-series-wrapper";

        var select = document.createElement("select");
        select.id = "post-series";
        select.name = "SeriesId";
        select.className = "pe-input";

        var noneOption = document.createElement("option");
        noneOption.value = "";
        noneOption.textContent = "-- No Series --";
        select.appendChild(noneOption);

        wrapper.appendChild(select);

        var createBtn = document.createElement("button");
        createBtn.type = "button";
        createBtn.className = "pe-btn pe-btn-secondary pe-btn-sm";
        createBtn.innerHTML = '<i class="fa-solid fa-plus"></i>';
        createBtn.title = "Create New Series";
        createBtn.addEventListener("click", showSeriesDialog);
        wrapper.appendChild(createBtn);

        section.appendChild(wrapper);

        // Hidden input for series order
        var orderInput = document.createElement("input");
        orderInput.type = "hidden";
        orderInput.id = "post-series-order";
        orderInput.name = "SeriesOrder";
        orderInput.value = "0";
        section.appendChild(orderInput);

        return section;
    }

    function showSeriesDialog() {
        if (!overlay) {
            overlay = createSeriesDialog();
            document.body.appendChild(overlay);
        }

        resetSeriesForm();
        overlay.hidden = false;
    }

    function createSeriesDialog() {
        var overlayEl = document.createElement("div");
        overlayEl.className = "pe-media-dialog-overlay";
        overlayEl.hidden = true;

        var content = document.createElement("div");
        content.className = "pe-media-dialog-content";
        content.style.maxWidth = "500px";

        var header = document.createElement("div");
        header.className = "pe-media-dialog-header";

        var title = document.createElement("h3");
        title.className = "pe-media-dialog-title";
        title.textContent = "Create New Series";
        header.appendChild(title);

        var closeBtn = document.createElement("button");
        closeBtn.type = "button";
        closeBtn.className = "pe-media-dialog-close";
        closeBtn.innerHTML = '<i class="fa-solid fa-times"></i>';
        closeBtn.addEventListener("click", hideSeriesDialog);
        header.appendChild(closeBtn);

        content.appendChild(header);

        var body = document.createElement("div");
        body.className = "pe-media-dialog-body";

        // Series name
        var nameGroup = document.createElement("div");
        nameGroup.className = "pe-form-group";
        var nameLabel = document.createElement("label");
        nameLabel.className = "pe-media-width-label";
        nameLabel.textContent = "Series Name *";
        nameLabel.setAttribute("for", "series-name");
        nameGroup.appendChild(nameLabel);
        var nameInput = document.createElement("input");
        nameInput.type = "text";
        nameInput.id = "series-name";
        nameInput.className = "pe-media-url-input";
        nameInput.required = true;
        nameInput.placeholder = "Enter series name...";
        nameGroup.appendChild(nameInput);
        body.appendChild(nameGroup);

        // Series description
        var descGroup = document.createElement("div");
        descGroup.className = "pe-form-group";
        var descLabel = document.createElement("label");
        descLabel.className = "pe-media-width-label";
        descLabel.textContent = "Description";
        descLabel.setAttribute("for", "series-description");
        descGroup.appendChild(descLabel);
        var descTextarea = document.createElement("textarea");
        descTextarea.id = "series-description";
        descTextarea.className = "pe-media-url-input";
        descTextarea.rows = 3;
        descTextarea.placeholder = "Optional description...";
        descGroup.appendChild(descTextarea);
        body.appendChild(descGroup);

        content.appendChild(body);

        var footer = document.createElement("div");
        footer.className = "pe-media-dialog-footer";

        var cancelBtn = document.createElement("button");
        cancelBtn.type = "button";
        cancelBtn.className = "pe-btn pe-btn-secondary";
        cancelBtn.textContent = "Cancel";
        cancelBtn.addEventListener("click", hideSeriesDialog);
        footer.appendChild(cancelBtn);

        var saveBtn = document.createElement("button");
        saveBtn.type = "button";
        saveBtn.className = "pe-btn pe-btn-primary";
        saveBtn.textContent = "Create Series";
        saveBtn.addEventListener("click", saveSeries);
        footer.appendChild(saveBtn);

        content.appendChild(footer);
        overlayEl.appendChild(content);

        // Close on backdrop click
        overlayEl.addEventListener("click", function (e) {
            if (e.target === overlayEl) {
                hideSeriesDialog();
            }
        });

        return overlayEl;
    }

    function hideSeriesDialog() {
        if (overlay) {
            overlay.hidden = true;
        }
        resetSeriesForm();
    }

    function resetSeriesForm() {
        var nameInput = document.getElementById("series-name");
        var descInput = document.getElementById("series-description");
        if (nameInput) nameInput.value = "";
        if (descInput) descInput.value = "";
    }

    function saveSeries() {
        var name = document.getElementById("series-name").value.trim();
        var description = document.getElementById("series-description").value.trim();

        if (!name) {
            showToast("Series name is required", "error");
            return;
        }

        // Get anti-forgery token from the form
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var formData = new FormData();
        formData.append("name", name);
        formData.append("description", description || "");
        if (token) {
            formData.append("__RequestVerificationToken", token.value);
        }

        // Make AJAX call to create the series
        fetch("/Post/CreateSeries", {
            method: "POST",
            body: formData
        })
        .then(function(response) {
            if (!response.ok) {
                throw new Error("Server returned " + response.status);
            }
            return response.json();
        })
        .then(function(data) {
            if (data.success) {
                var newSeries = {
                    id: data.id,
                    name: data.name
                };
                addSeriesToDropdown(newSeries);
                hideSeriesDialog();
                showToast("Series created successfully", "success");
            } else {
                showToast(data.error || "Failed to create series", "error");
            }
        })
        .catch(function(error) {
            console.error("Error creating series:", error);
            showToast("Failed to create series. " + error.message, "error");
        });
    }

    function addSeriesToDropdown(series) {
        var select = document.getElementById("post-series");
        if (!select) return;

        var option = document.createElement("option");
        option.value = series.id;
        option.textContent = series.name;
        option.selected = true;
        select.appendChild(option);

        selectedSeriesId = series.id;
    }

    function loadSeriesOptions(seriesData) {
        var select = document.getElementById("post-series");
        if (!select) return;

        // Clear existing options except the first one
        while (select.options.length > 1) {
            select.remove(1);
        }

        if (seriesData && seriesData.length > 0) {
            seriesData.forEach(function (series) {
                var option = document.createElement("option");
                option.value = series.Id || series.id;
                option.textContent = series.Name || series.name;
                select.appendChild(option);
            });
        }
    }

    function setSelectedSeries(seriesId) {
        var select = document.getElementById("post-series");
        if (select) {
            select.value = seriesId || "";
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
    window.WysiwygSeries = {
        init: init,
        createUI: createSeriesUI,
        loadSeriesOptions: loadSeriesOptions,
        setSelectedSeries: setSelectedSeries
    };

    // Initialize on DOM ready
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
