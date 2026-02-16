"use strict";

(function () {
    var categoryInput = document.getElementById("post-category");
    var datalist = document.getElementById("category-list");
    if (!categoryInput || !datalist) return;

    // Store all categories
    var allCategories = [];
    var options = datalist.querySelectorAll("option");
    options.forEach(function (option) {
        allCategories.push(option.value);
    });

    // Create custom dropdown
    var dropdown = document.createElement("div");
    dropdown.className = "pe-category-dropdown-menu";
    dropdown.hidden = true;
    categoryInput.parentNode.appendChild(dropdown);

    function showDropdown() {
        if (allCategories.length === 0) return;
        updateDropdown(categoryInput.value);
        dropdown.hidden = false;
        positionDropdown();
    }

    function hideDropdown() {
        dropdown.hidden = true;
    }

    function positionDropdown() {
        var rect = categoryInput.getBoundingClientRect();
        dropdown.style.position = "absolute";
        dropdown.style.top = (categoryInput.offsetTop + categoryInput.offsetHeight) + "px";
        dropdown.style.left = categoryInput.offsetLeft + "px";
        dropdown.style.width = categoryInput.offsetWidth + "px";
    }

    function updateDropdown(query) {
        var filtered = allCategories;

        if (query && query.trim() !== "") {
            var lowerQuery = query.toLowerCase();
            filtered = allCategories.filter(function (cat) {
                return cat.toLowerCase().indexOf(lowerQuery) !== -1;
            });
        }

        dropdown.innerHTML = "";

        if (filtered.length === 0) {
            hideDropdown();
            return;
        }

        filtered.forEach(function (cat) {
            var item = document.createElement("div");
            item.className = "pe-category-dropdown-item";
            item.textContent = cat;
            item.addEventListener("click", function () {
                categoryInput.value = cat;
                hideDropdown();
                categoryInput.focus();
            });
            dropdown.appendChild(item);
        });
    }

    // Show dropdown on focus/click
    categoryInput.addEventListener("focus", function () {
        showDropdown();
    });

    categoryInput.addEventListener("click", function () {
        showDropdown();
    });

    // Update dropdown as user types
    categoryInput.addEventListener("input", function () {
        updateDropdown(categoryInput.value);
        if (!dropdown.hidden) {
            positionDropdown();
        }
    });

    // Hide dropdown when clicking outside
    document.addEventListener("click", function (e) {
        if (e.target !== categoryInput && !dropdown.contains(e.target)) {
            hideDropdown();
        }
    });

    // Handle keyboard navigation
    categoryInput.addEventListener("keydown", function (e) {
        if (!dropdown.hidden) {
            var items = dropdown.querySelectorAll(".pe-category-dropdown-item");
            var activeItem = dropdown.querySelector(".pe-category-dropdown-item.active");
            var activeIndex = -1;

            if (activeItem) {
                activeIndex = Array.from(items).indexOf(activeItem);
            }

            if (e.key === "ArrowDown") {
                e.preventDefault();
                if (activeIndex < items.length - 1) {
                    if (activeItem) activeItem.classList.remove("active");
                    items[activeIndex + 1].classList.add("active");
                    items[activeIndex + 1].scrollIntoView({ block: "nearest" });
                } else if (items.length > 0) {
                    if (activeItem) activeItem.classList.remove("active");
                    items[0].classList.add("active");
                }
            } else if (e.key === "ArrowUp") {
                e.preventDefault();
                if (activeIndex > 0) {
                    activeItem.classList.remove("active");
                    items[activeIndex - 1].classList.add("active");
                    items[activeIndex - 1].scrollIntoView({ block: "nearest" });
                } else if (items.length > 0) {
                    if (activeItem) activeItem.classList.remove("active");
                    items[items.length - 1].classList.add("active");
                }
            } else if (e.key === "Enter") {
                if (activeItem) {
                    e.preventDefault();
                    activeItem.click();
                }
            } else if (e.key === "Escape") {
                hideDropdown();
            }
        }
    });

    // Update position on window resize
    window.addEventListener("resize", function () {
        if (!dropdown.hidden) {
            positionDropdown();
        }
    });
})();
