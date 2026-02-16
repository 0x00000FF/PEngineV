// Post list page search functionality
(function () {
    'use strict';

    const searchInput = document.getElementById('post-list-search');
    const clearButton = document.getElementById('post-list-search-clear');
    const categoryBtn = document.getElementById('category-dropdown-btn');
    const categoryMenu = document.getElementById('category-dropdown-menu');
    if (!searchInput) return;

    // Get all post lists (there might be multiple)
    const allPostLists = document.querySelectorAll('.pe-post-list');
    if (!allPostLists.length) return;

    // Get all post items (actual posts, not directories or parent)
    const postItems = [];
    const dirItems = [];
    const parentItems = [];

    allPostLists.forEach(list => {
        // Find actual post items (not directories or parent)
        list.querySelectorAll('.pe-post-item').forEach(item => {
            if (item.classList.contains('pe-post-item-dir')) {
                dirItems.push(item);
            } else if (item.classList.contains('pe-post-item-parent')) {
                parentItems.push(item);
            } else {
                postItems.push(item);
            }
        });
    });

    function filterPosts(query) {
        const searchTerm = query.toLowerCase().trim();

        // Show/hide clear button
        if (clearButton) {
            clearButton.hidden = !searchTerm;
        }

        if (!searchTerm) {
            // Show all items when search is empty
            postItems.forEach(item => {
                item.classList.remove('pe-hidden');
            });
            dirItems.forEach(item => {
                item.classList.remove('pe-hidden');
            });
            parentItems.forEach(item => {
                item.classList.remove('pe-hidden');
            });
            hideNoResultsMessage();
            return;
        }

        // Hide directory, parent items, and dividers during search
        dirItems.forEach(item => {
            item.classList.add('pe-hidden');
        });
        parentItems.forEach(item => {
            item.classList.add('pe-hidden');
        });

        // Filter post items
        let visibleCount = 0;
        postItems.forEach(item => {
            const title = item.querySelector('.pe-post-title')?.textContent.toLowerCase() || '';
            const author = item.querySelector('.pe-author-name')?.textContent.toLowerCase() || '';
            const tags = Array.from(item.querySelectorAll('.pe-tag')).map(t => t.textContent.toLowerCase()).join(' ');

            const matches = title.includes(searchTerm) ||
                          author.includes(searchTerm) ||
                          tags.includes(searchTerm);

            if (matches) {
                item.classList.remove('pe-hidden');
                visibleCount++;
            } else {
                item.classList.add('pe-hidden');
            }
        });

        // Show "no results" message if needed
        if (visibleCount === 0) {
            showNoResultsMessage(searchTerm);
        } else {
            hideNoResultsMessage();
        }
    }

    function showNoResultsMessage(term) {
        hideNoResultsMessage();

        // Find the last post list to append message
        const lastList = allPostLists[allPostLists.length - 1];
        const noResultsMsg = document.createElement('li');
        noResultsMsg.className = 'pe-post-item pe-search-no-results';

        const emptyDiv = document.createElement('div');
        emptyDiv.className = 'pe-empty';
        emptyDiv.textContent = `No posts found for "${term}"`;

        noResultsMsg.appendChild(emptyDiv);
        lastList.appendChild(noResultsMsg);
    }

    function hideNoResultsMessage() {
        document.querySelectorAll('.pe-search-no-results').forEach(el => el.remove());
    }

    // Debounce search for performance
    let debounceTimer;
    searchInput.addEventListener('input', function (e) {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            filterPosts(e.target.value);
        }, 150);
    });

    // Clear search on escape key
    searchInput.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            searchInput.value = '';
            filterPosts('');
        }
    });

    // Clear button click handler
    if (clearButton) {
        clearButton.addEventListener('click', function () {
            searchInput.value = '';
            filterPosts('');
            searchInput.focus();
        });
    }

    // Category dropdown toggle
    if (categoryBtn && categoryMenu) {
        categoryBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            const dropdown = categoryBtn.closest('.pe-category-dropdown');
            const isOpen = !categoryMenu.hidden;

            if (isOpen) {
                categoryMenu.hidden = true;
                dropdown.classList.remove('open');
            } else {
                categoryMenu.hidden = false;
                dropdown.classList.add('open');
            }
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', function () {
            if (!categoryMenu.hidden) {
                categoryMenu.hidden = true;
                categoryBtn.closest('.pe-category-dropdown').classList.remove('open');
            }
        });

        // Prevent closing when clicking inside menu
        categoryMenu.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }
})();
