"use strict";

/**
 * Citation Reference Click Handler
 * Handles clicking on citation references to scroll and highlight citations
 */
(function () {
    function init() {
        // Find all citation references in the post content
        var postContent = document.querySelector('.pe-post-content');
        if (!postContent) return;

        var citationRefs = postContent.querySelectorAll('.pe-citation-ref, sup[data-citation-index]');

        citationRefs.forEach(function (ref) {
            ref.style.cursor = 'pointer';

            ref.addEventListener('click', function (e) {
                e.preventDefault();

                // Get citation index from data attribute or text content
                var citationIndex = ref.getAttribute('data-citation-index');
                if (!citationIndex) {
                    // Try to extract number from text content like "[1]"
                    var match = ref.textContent.match(/\[?(\d+)\]?/);
                    if (match) {
                        citationIndex = parseInt(match[1]) - 1;
                    }
                } else {
                    citationIndex = parseInt(citationIndex);
                }

                if (citationIndex !== null && !isNaN(citationIndex)) {
                    var citationNumber = citationIndex + 1;
                    scrollToCitation(citationNumber);
                }
            });
        });
    }

    function scrollToCitation(number) {
        var citationElement = document.getElementById('citation-' + number);
        if (!citationElement) return;

        // Remove existing highlight class from all citations
        var allCitations = document.querySelectorAll('.pe-citations-list > li');
        allCitations.forEach(function (li) {
            li.classList.remove('citation-highlight');
        });

        // Scroll to citation with smooth behavior
        citationElement.scrollIntoView({
            behavior: 'smooth',
            block: 'center'
        });

        // Add highlight class after a small delay to ensure scroll has started
        setTimeout(function () {
            citationElement.classList.add('citation-highlight');

            // Remove highlight class after animation completes
            setTimeout(function () {
                citationElement.classList.remove('citation-highlight');
            }, 1500);
        }, 100);
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
