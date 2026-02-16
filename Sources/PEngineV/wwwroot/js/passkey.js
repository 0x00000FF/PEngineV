"use strict";

(function () {
    function base64UrlEncode(buffer) {
        var bytes = new Uint8Array(buffer);
        var binary = "";
        for (var i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
    }

    function base64UrlDecode(str) {
        str = str.replace(/-/g, "+").replace(/_/g, "/");
        while (str.length % 4) str += "=";
        var binary = atob(str);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes.buffer;
    }

    function coerceToArrayBuffer(thing) {
        if (typeof thing === "string") {
            return base64UrlDecode(thing);
        }
        if (Array.isArray(thing)) {
            return new Uint8Array(thing).buffer;
        }
        if (thing instanceof ArrayBuffer) {
            return thing;
        }
        if (thing.buffer instanceof ArrayBuffer) {
            return thing.buffer;
        }
        return thing;
    }

    function initPasskeyRegistration() {
        var form = document.getElementById("passkey-add-form");
        var btn = document.getElementById("btn-add-passkey");
        if (!form || !btn) return;

        form.addEventListener("submit", function (e) {
            e.preventDefault();
            var nameInput = document.getElementById("mypage-passkey-name");
            var name = nameInput ? nameInput.value.trim() : "My Passkey";
            if (!name) return;

            btn.disabled = true;
            btn.textContent = "Registering...";

            // Note: Passkey endpoints use [IgnoreAntiforgeryToken] because
            // WebAuthn has built-in CSRF protection via origin validation
            fetch("/MyPage/BeginPasskeyRegistration", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name: name })
            })
            .then(function (res) { return res.json(); })
            .then(function (options) {
                options.challenge = coerceToArrayBuffer(options.challenge);
                options.user.id = coerceToArrayBuffer(options.user.id);
                if (options.excludeCredentials) {
                    options.excludeCredentials.forEach(function (cred) {
                        cred.id = coerceToArrayBuffer(cred.id);
                    });
                }
                return navigator.credentials.create({ publicKey: options });
            })
            .then(function (credential) {
                var attestationResponse = {
                    id: credential.id,
                    rawId: base64UrlEncode(credential.rawId),
                    type: credential.type,
                    response: {
                        attestationObject: base64UrlEncode(credential.response.attestationObject),
                        clientDataJSON: base64UrlEncode(credential.response.clientDataJSON)
                    },
                    extensions: credential.getClientExtensionResults()
                };

                return fetch("/MyPage/CompletePasskeyRegistration", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(attestationResponse)
                });
            })
            .then(function (res) { return res.json(); })
            .then(function (result) {
                if (result.success) {
                    if (window.peToast) window.peToast("Passkey registered successfully.", "success");
                    setTimeout(function () { window.location.reload(); }, 1000);
                } else {
                    if (window.peToast) window.peToast("Failed to register passkey.", "error");
                    btn.disabled = false;
                    btn.textContent = "Add Passkey";
                }
            })
            .catch(function (err) {
                console.error("Passkey registration error:", err);
                if (window.peToast) window.peToast("Passkey registration cancelled or failed.", "error");
                btn.disabled = false;
                btn.textContent = "Add Passkey";
            });
        });
    }

    function initPasskeyLogin() {
        var btn = document.getElementById("btn-login-passkey");
        if (!btn) return;

        btn.addEventListener("click", function () {
            btn.disabled = true;

            fetch("/Account/BeginPasskeyLogin", {
                method: "POST",
                headers: { "Content-Type": "application/json" }
            })
            .then(function (res) { return res.json(); })
            .then(function (options) {
                options.challenge = coerceToArrayBuffer(options.challenge);
                if (options.allowCredentials) {
                    options.allowCredentials.forEach(function (cred) {
                        cred.id = coerceToArrayBuffer(cred.id);
                    });
                }
                return navigator.credentials.get({ publicKey: options });
            })
            .then(function (assertion) {
                var assertionResponse = {
                    id: assertion.id,
                    rawId: base64UrlEncode(assertion.rawId),
                    type: assertion.type,
                    response: {
                        authenticatorData: base64UrlEncode(assertion.response.authenticatorData),
                        clientDataJSON: base64UrlEncode(assertion.response.clientDataJSON),
                        signature: base64UrlEncode(assertion.response.signature),
                        userHandle: assertion.response.userHandle
                            ? base64UrlEncode(assertion.response.userHandle)
                            : null
                    },
                    extensions: assertion.getClientExtensionResults()
                };

                return fetch("/Account/CompletePasskeyLogin", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(assertionResponse)
                });
            })
            .then(function (res) { return res.json(); })
            .then(function (result) {
                if (result.success && result.redirect) {
                    window.location.href = result.redirect;
                } else {
                    if (window.peToast) window.peToast("Passkey login failed.", "error");
                    btn.disabled = false;
                }
            })
            .catch(function (err) {
                console.error("Passkey login error:", err);
                btn.disabled = false;
            });
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initPasskeyRegistration();
        initPasskeyLogin();
    });
})();
