"use strict";

/**
 * Table of Contents Generator for WYSIWYG Editor
 * Automatically generates TOC from section headers
 */
(function () {
    /**
     * Generates a TOC from the editor content
     * @param {HTMLElement} editorElement - The editor content element
     * @returns {Object} - TOC data structure
     */
    function generateTOC(editorElement) {
        var headings = editorElement.querySelectorAll("h1, h2, h3, h4, h5, h6");
        var tocItems = [];
        var usedIds = {};

        headings.forEach(function (heading, index) {
            var text = heading.textContent.trim();
            var level = parseInt(heading.tagName.substring(1));

            // Generate unique ID
            var id = generateId(text, usedIds);
            usedIds[id] = true;

            // Set ID on heading if not already set
            if (!heading.id) {
                heading.id = id;
            }

            tocItems.push({
                id: heading.id,
                text: text,
                level: level,
                element: heading
            });
        });

        return tocItems;
    }

    /**
     * Generates a URL-friendly ID from text
     * @param {string} text - The heading text
     * @param {Object} usedIds - Already used IDs
     * @returns {string} - Generated ID
     */
    function generateId(text, usedIds) {
        // Convert to lowercase and replace spaces with hyphens
        var baseId = text
            .toLowerCase()
            .replace(/[^\w\s-]/g, "") // Remove special chars
            .replace(/\s+/g, "-")      // Replace spaces with hyphens
            .replace(/--+/g, "-")      // Replace multiple hyphens with single
            .replace(/^-+|-+$/g, "");  // Trim hyphens from ends

        // Ensure ID is unique
        var id = baseId;
        var counter = 1;
        while (usedIds[id]) {
            id = baseId + "-" + counter;
            counter++;
        }

        return id || "heading-" + Object.keys(usedIds).length;
    }

    /**
     * Renders a TOC HTML structure
     * @param {Array} tocItems - TOC items from generateTOC
     * @returns {HTMLElement} - TOC container element
     */
    function renderTOC(tocItems) {
        if (tocItems.length === 0) {
            return null;
        }

        var container = document.createElement("div");
        container.className = "pe-toc";

        var title = document.createElement("h3");
        title.className = "pe-toc-title";
        title.textContent = "Table of Contents";
        container.appendChild(title);

        var list = document.createElement("ul");
        list.className = "pe-toc-list";

        var currentLevel = tocItems[0].level;
        var currentList = list;
        var listStack = [{ level: currentLevel, list: list }];

        tocItems.forEach(function (item) {
            // Handle nesting based on heading levels
            if (item.level > currentLevel) {
                // Create nested list
                var nestedList = document.createElement("ul");
                nestedList.className = "pe-toc-list-nested";

                var lastItem = currentList.lastElementChild;
                if (lastItem) {
                    lastItem.appendChild(nestedList);
                }

                listStack.push({ level: item.level, list: nestedList });
                currentList = nestedList;
                currentLevel = item.level;
            } else if (item.level < currentLevel) {
                // Go back up the stack
                while (listStack.length > 0 && listStack[listStack.length - 1].level > item.level) {
                    listStack.pop();
                }

                if (listStack.length > 0) {
                    currentList = listStack[listStack.length - 1].list;
                    currentLevel = listStack[listStack.length - 1].level;
                } else {
                    currentList = list;
                    currentLevel = item.level;
                }
            }

            // Create TOC item
            var li = document.createElement("li");
            li.className = "pe-toc-item pe-toc-item-level-" + item.level;

            var link = document.createElement("a");
            link.href = "#" + item.id;
            link.className = "pe-toc-link";
            link.textContent = item.text;

            // Smooth scroll to heading
            link.addEventListener("click", function (e) {
                e.preventDefault();
                item.element.scrollIntoView({ behavior: "smooth", block: "start" });
            });

            li.appendChild(link);
            currentList.appendChild(li);
        });

        container.appendChild(list);
        return container;
    }

    /**
     * Updates or creates TOC in a container
     * @param {HTMLElement} editorElement - The editor content element
     * @param {HTMLElement} tocContainer - Container to render TOC into
     */
    function updateTOC(editorElement, tocContainer) {
        if (!tocContainer) return;

        var tocItems = generateTOC(editorElement);
        var tocElement = renderTOC(tocItems);

        // Clear existing TOC
        tocContainer.innerHTML = "";

        if (tocElement) {
            tocContainer.appendChild(tocElement);
            tocContainer.hidden = false;
        } else {
            tocContainer.hidden = true;
        }
    }

    /**
     * Ensures all headings in the editor have IDs
     * @param {HTMLElement} editorElement - The editor content element
     */
    function ensureHeadingIds(editorElement) {
        var usedIds = {};

        // First pass: collect existing IDs
        var headings = editorElement.querySelectorAll("h1, h2, h3, h4, h5, h6");
        headings.forEach(function (heading) {
            if (heading.id) {
                usedIds[heading.id] = true;
            }
        });

        // Second pass: assign IDs to headings without them
        headings.forEach(function (heading) {
            if (!heading.id) {
                var text = heading.textContent.trim();
                var id = generateId(text, usedIds);
                usedIds[id] = true;
                heading.id = id;
            }
        });
    }

    /**
     * Creates a TOC widget that can be toggled
     * @param {HTMLElement} editorElement - The editor content element
     * @returns {HTMLElement} - TOC widget container
     */
    function createTOCWidget(editorElement) {
        var widget = document.createElement("div");
        widget.className = "pe-toc-widget";

        var toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = "pe-toc-toggle";
        toggle.innerHTML = '<i class="fa-solid fa-list"></i> Show TOC';

        var tocContainer = document.createElement("div");
        tocContainer.className = "pe-toc-container";
        tocContainer.hidden = true;

        toggle.addEventListener("click", function () {
            if (tocContainer.hidden) {
                updateTOC(editorElement, tocContainer);
                toggle.innerHTML = '<i class="fa-solid fa-list"></i> Hide TOC';
            } else {
                tocContainer.hidden = true;
                toggle.innerHTML = '<i class="fa-solid fa-list"></i> Show TOC';
            }
        });

        widget.appendChild(toggle);
        widget.appendChild(tocContainer);

        return widget;
    }

    // Export functions to global scope
    window.WysiwygTOC = {
        generateTOC: generateTOC,
        renderTOC: renderTOC,
        updateTOC: updateTOC,
        ensureHeadingIds: ensureHeadingIds,
        createTOCWidget: createTOCWidget
    };
})();
