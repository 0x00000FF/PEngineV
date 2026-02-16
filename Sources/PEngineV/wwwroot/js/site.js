"use strict";

(function () {
    var THEME_KEY = "pe-theme";

    function getSystemTheme() {
        return window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    }

    function getStoredTheme() {
        try {
            return localStorage.getItem(THEME_KEY);
        } catch (_) {
            return null;
        }
    }

    function setStoredTheme(theme) {
        try {
            localStorage.setItem(THEME_KEY, theme);
        } catch (_) {
            /* noop */
        }
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute("data-theme", theme);
    }

    function initTheme() {
        var stored = getStoredTheme();
        applyTheme(stored || getSystemTheme());
    }

    function toggleTheme() {
        var current = document.documentElement.getAttribute("data-theme");
        var next = current === "dark" ? "light" : "dark";
        applyTheme(next);
        setStoredTheme(next);
    }

    function initNavToggle() {
        var toggle = document.getElementById("pe-nav-toggle");
        var navArea = document.getElementById("pe-nav-area");

        if (toggle && navArea) {
            toggle.addEventListener("click", function () {
                navArea.classList.toggle("open");
            });

            document.addEventListener("keydown", function (e) {
                if (e.key === "Escape" && navArea.classList.contains("open")) {
                    navArea.classList.remove("open");
                    toggle.focus();
                }
            });
        }
    }

    function initCommentReplyToggles() {
        var buttons = document.querySelectorAll(".pe-comment-reply-toggle");
        buttons.forEach(function (btn) {
            btn.addEventListener("click", function () {
                var commentId = btn.getAttribute("data-comment-id");
                var form = document.getElementById("reply-form-" + commentId);
                if (form) {
                    form.hidden = !form.hidden;
                }
            });
        });
    }

    function showToast(message, type) {
        var container = document.getElementById("pe-toast-container");
        if (!container) {
            container = document.createElement("div");
            container.id = "pe-toast-container";
            container.className = "pe-toast-container";
            document.body.appendChild(container);
        }

        var toast = document.createElement("div");
        toast.className = "pe-toast" + (type ? " pe-toast-" + type : "");
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(function () {
            toast.remove();
        }, 4000);
    }

    window.peToast = showToast;

    initTheme();

    window.matchMedia("(prefers-color-scheme: dark)")
        .addEventListener("change", function (e) {
            if (!getStoredTheme()) {
                applyTheme(e.matches ? "dark" : "light");
            }
        });

    function initAuditLogOverlay() {
        var overlay = document.getElementById("pe-audit-overlay");
        if (!overlay) return;

        var backdrop = document.getElementById("pe-audit-overlay-backdrop");
        var closeBtn = document.getElementById("pe-audit-overlay-close");
        var closeFooterBtn = document.getElementById("pe-audit-overlay-close-btn");
        var rows = document.querySelectorAll(".pe-audit-row");

        function openOverlay(row) {
            document.getElementById("pe-audit-detail-timestamp").textContent =
                row.getAttribute("data-timestamp") || "";
            document.getElementById("pe-audit-detail-action").textContent =
                row.getAttribute("data-action") || "";
            document.getElementById("pe-audit-detail-ip").textContent =
                row.getAttribute("data-ip") || "";
            document.getElementById("pe-audit-detail-useragent").textContent =
                row.getAttribute("data-useragent") || "";
            document.getElementById("pe-audit-detail-details").textContent =
                row.getAttribute("data-details") || "";
            overlay.hidden = false;
        }

        function closeOverlay() {
            overlay.hidden = true;
        }

        rows.forEach(function (row) {
            row.addEventListener("click", function () {
                openOverlay(row);
            });
        });

        if (backdrop) backdrop.addEventListener("click", closeOverlay);
        if (closeBtn) closeBtn.addEventListener("click", closeOverlay);
        if (closeFooterBtn) closeFooterBtn.addEventListener("click", closeOverlay);

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !overlay.hidden) {
                closeOverlay();
            }
        });
    }

    function initProfileImageUpload() {
        var avatarBtn = document.getElementById("profile-avatar-btn");
        var fileInput = document.getElementById("mypage-profile-image");
        var form = document.getElementById("profile-image-form");

        if (!avatarBtn || !fileInput || !form) return;

        function triggerFileSelect() {
            fileInput.click();
        }

        avatarBtn.addEventListener("click", triggerFileSelect);
        avatarBtn.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                triggerFileSelect();
            }
        });

        fileInput.addEventListener("change", function () {
            if (fileInput.files && fileInput.files.length > 0) {
                form.submit();
            }
        });
    }

    function initTabs() {
        var tabBtns = document.querySelectorAll(".pe-tab-btn");
        if (tabBtns.length === 0) return;

        function activateTab(target) {
            tabBtns.forEach(function (b) { b.classList.remove("active"); });
            document.querySelectorAll(".pe-tab-panel").forEach(function (p) {
                p.classList.remove("active");
            });

            var btn = document.querySelector('.pe-tab-btn[data-pe-tab-target="' + target + '"]');
            var panel = document.querySelector('.pe-tab-panel[data-pe-tab="' + target + '"]');
            if (btn) btn.classList.add("active");
            if (panel) panel.classList.add("active");
        }

        tabBtns.forEach(function (btn) {
            btn.addEventListener("click", function () {
                var target = btn.getAttribute("data-pe-tab-target");
                activateTab(target);
                history.replaceState(null, "", "#" + target);
            });
        });

        // Restore tab from URL hash
        var hash = window.location.hash.replace("#", "");
        if (hash && document.querySelector('.pe-tab-panel[data-pe-tab="' + hash + '"]')) {
            activateTab(hash);
        }
    }

    function initToastTrigger() {
        var trigger = document.getElementById("pe-toast-trigger");
        if (!trigger) return;
        var message = trigger.getAttribute("data-message");
        var type = trigger.getAttribute("data-type") || "success";
        if (message) {
            showToast(message, type);
        }
    }

    function initButtonAnimation() {
        var forms = document.querySelectorAll("form");
        forms.forEach(function (form) {
            form.addEventListener("submit", function () {
                var btn = form.querySelector('button[type="submit"]');
                if (btn && !btn.classList.contains("pe-btn-saving")) {
                    btn.classList.add("pe-btn-saving");
                    setTimeout(function () {
                        btn.classList.remove("pe-btn-saving");
                    }, 300);
                }
            });
        });
    }

    function initAuditLogFilter() {
        var table = document.getElementById("pe-audit-table");
        if (!table) return;

        var rows = table.querySelectorAll("tbody .pe-audit-row");
        var filterFrom = document.getElementById("audit-filter-from");
        var filterTo = document.getElementById("audit-filter-to");
        var filterAction = document.getElementById("audit-filter-action");
        var filterIp = document.getElementById("audit-filter-ip");
        var filterKeyword = document.getElementById("audit-filter-keyword");

        // Populate action dropdown from unique values
        var actions = new Set();
        rows.forEach(function (row) {
            var action = row.getAttribute("data-action");
            if (action) actions.add(action);
        });
        var sortedActions = Array.from(actions).sort();
        sortedActions.forEach(function (action) {
            var opt = document.createElement("option");
            opt.value = action;
            opt.textContent = action;
            filterAction.appendChild(opt);
        });

        var debounceTimer;
        function applyFilters() {
            var fromVal = filterFrom.value;
            var toVal = filterTo.value;
            var actionVal = filterAction.value.toLowerCase();
            var ipVal = filterIp.value.toLowerCase();
            var keywordVal = filterKeyword.value.toLowerCase();

            rows.forEach(function (row) {
                var timestamp = row.getAttribute("data-timestamp") || "";
                var dateOnly = timestamp.substring(0, 10);
                var action = (row.getAttribute("data-action") || "").toLowerCase();
                var ip = (row.getAttribute("data-ip") || "").toLowerCase();
                var details = (row.getAttribute("data-details") || "").toLowerCase();
                var useragent = (row.getAttribute("data-useragent") || "").toLowerCase();

                var show = true;
                if (fromVal && dateOnly < fromVal) show = false;
                if (toVal && dateOnly > toVal) show = false;
                if (actionVal && action !== actionVal) show = false;
                if (ipVal && ip.indexOf(ipVal) === -1) show = false;
                if (keywordVal) {
                    var all = timestamp + " " + action + " " + ip + " " + details + " " + useragent;
                    if (all.toLowerCase().indexOf(keywordVal) === -1) show = false;
                }

                row.style.display = show ? "" : "none";
            });
        }

        function debouncedFilter() {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(applyFilters, 200);
        }

        if (filterFrom) filterFrom.addEventListener("change", applyFilters);
        if (filterTo) filterTo.addEventListener("change", applyFilters);
        if (filterAction) filterAction.addEventListener("change", applyFilters);
        if (filterIp) filterIp.addEventListener("input", debouncedFilter);
        if (filterKeyword) filterKeyword.addEventListener("input", debouncedFilter);
    }

    document.addEventListener("DOMContentLoaded", function () {
        var btn = document.getElementById("pe-theme-toggle");
        if (btn) {
            btn.addEventListener("click", toggleTheme);
        }
        initNavToggle();
        initCommentReplyToggles();
        initAuditLogOverlay();
        initProfileImageUpload();
        initTabs();
        initToastTrigger();
        initButtonAnimation();
        initAuditLogFilter();
    });
})();
