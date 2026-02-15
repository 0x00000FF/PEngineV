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

    document.addEventListener("DOMContentLoaded", function () {
        var btn = document.getElementById("pe-theme-toggle");
        if (btn) {
            btn.addEventListener("click", toggleTheme);
        }
        initNavToggle();
        initCommentReplyToggles();
    });
})();
