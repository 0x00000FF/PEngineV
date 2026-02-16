"use strict";

(function () {
    var tagInput = document.getElementById("post-tags");
    if (!tagInput) return;

    var tags = [];
    var container = document.createElement("div");
    container.className = "pe-tag-editor";

    var tagsContainer = document.createElement("div");
    tagsContainer.className = "pe-tag-editor-tags";

    var inputWrapper = document.createElement("div");
    inputWrapper.className = "pe-tag-editor-input-wrapper";

    var newTagInput = document.createElement("input");
    newTagInput.type = "text";
    newTagInput.className = "pe-tag-editor-input";
    newTagInput.placeholder = "Add tag...";

    inputWrapper.appendChild(newTagInput);
    container.appendChild(tagsContainer);
    container.appendChild(inputWrapper);

    // Hide original input and insert tag editor
    tagInput.style.display = "none";
    tagInput.parentNode.insertBefore(container, tagInput.nextSibling);

    // Parse existing tags
    if (tagInput.value) {
        tags = tagInput.value.split(",").map(function (tag) {
            return tag.trim();
        }).filter(function (tag) {
            return tag !== "";
        });
    }

    function updateHiddenInput() {
        tagInput.value = tags.join(", ");
    }

    function renderTags() {
        tagsContainer.innerHTML = "";
        tags.forEach(function (tag, index) {
            var tagChip = document.createElement("div");
            tagChip.className = "pe-tag-chip";

            var tagText = document.createElement("span");
            tagText.className = "pe-tag-chip-text";
            tagText.textContent = tag;

            var removeBtn = document.createElement("button");
            removeBtn.type = "button";
            removeBtn.className = "pe-tag-chip-remove";
            removeBtn.innerHTML = '<i class="fa-solid fa-times"></i>';
            removeBtn.addEventListener("click", function () {
                tags.splice(index, 1);
                updateHiddenInput();
                renderTags();
            });

            tagChip.appendChild(tagText);
            tagChip.appendChild(removeBtn);
            tagsContainer.appendChild(tagChip);
        });
    }

    function addTag(tagText) {
        var trimmed = tagText.trim();
        if (trimmed === "") return;
        if (tags.indexOf(trimmed) !== -1) return; // Avoid duplicates

        tags.push(trimmed);
        updateHiddenInput();
        renderTags();
        newTagInput.value = "";
    }

    newTagInput.addEventListener("keydown", function (e) {
        if (e.key === "Enter" || e.key === ",") {
            e.preventDefault();
            addTag(newTagInput.value);
        } else if (e.key === "Backspace" && newTagInput.value === "" && tags.length > 0) {
            tags.pop();
            updateHiddenInput();
            renderTags();
        }
    });

    newTagInput.addEventListener("blur", function () {
        if (newTagInput.value.trim() !== "") {
            addTag(newTagInput.value);
        }
    });

    // Initial render
    renderTags();
})();
