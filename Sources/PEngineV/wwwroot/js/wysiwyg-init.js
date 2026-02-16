"use strict";

/**
 * WYSIWYG Editor Initialization
 * Initializes the editor and all related components from data attributes
 */
(function () {
    function init() {
        var container = document.getElementById('wysiwyg-container');
        var contentInput = document.getElementById('post-content');
        var citationContainer = document.getElementById('pe-citation-container');

        if (!container || !contentInput) return;

        // Get placeholder from data attribute
        var placeholder = container.getAttribute('data-placeholder') || 'Start writing...';

        // Initialize WYSIWYG editor
        var editor = new WysiwygEditor(container, {
            placeholder: placeholder,
            initialContent: contentInput.value,
            onContentChange: function(content) {
                contentInput.value = content;
            }
        });

        // Initialize citation UI
        if (citationContainer) {
            var citationUI = WysiwygCitation.createUI();
            citationContainer.appendChild(citationUI);

            // Load existing citations from hidden input
            var citationsInput = document.getElementById('pe-citations-init-data');
            if (citationsInput && citationsInput.value) {
                WysiwygCitation.loadCitations(citationsInput.value);
            }
        }

        // Initialize series management - series are already rendered by server

        // Hook up series create button
        var createSeriesBtn = document.getElementById('create-series-btn');
        if (createSeriesBtn) {
            createSeriesBtn.addEventListener('click', function() {
                var event = new CustomEvent('wysiwyg:showSeriesDialog');
                document.dispatchEvent(event);
            });
        }

        // Ensure heading IDs before form submit
        var form = container.closest('form');
        if (form) {
            form.addEventListener('submit', function(e) {
                WysiwygTOC.ensureHeadingIds(editor.editor);
                contentInput.value = editor.getContent();
            });
        }
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
