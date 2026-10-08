(function (window, document) {
    "use strict";

    // Remembers which tab a form was submitted from, so the page comes back to
    // that tab after the POST redirects to /Account/Settings.
    const STORAGE_KEY = "accountSettings.activeTab";
    const buttons = document.querySelectorAll("[data-settings-tab]");
    if (!buttons.length || !window.bootstrap?.Tab) return;

    const paneOf = (el) => (el?.classList.contains("tab-pane") ? el : el?.closest(".tab-pane"));

    function showPane(pane) {
        const button = pane && document.querySelector(`[data-settings-tab][data-bs-target="#${pane.id}"]`);
        if (button) window.bootstrap.Tab.getOrCreateInstance(button).show();
        return Boolean(button);
    }

    // "#keamanan" opens a tab; "#kontak" opens the tab holding that card and scrolls to it.
    function showFromHash() {
        const key = decodeURIComponent(window.location.hash.slice(1));
        if (!key) return false;
        const target = document.getElementById(key) || document.getElementById(`tab-${key}`);
        const pane = paneOf(target);
        if (!showPane(pane)) return false;
        if (target !== pane) window.requestAnimationFrame(() => target.scrollIntoView({ block: "start" }));
        return true;
    }

    // A cancelled confirm dialog still records the tab; expire it so a later
    // unrelated visit does not jump to it.
    const STORED_TTL_MS = 60 * 1000;

    function readStoredPane() {
        try {
            const raw = window.sessionStorage.getItem(STORAGE_KEY);
            window.sessionStorage.removeItem(STORAGE_KEY);
            const stored = raw ? JSON.parse(raw) : null;
            if (!stored || Date.now() - stored.at > STORED_TTL_MS) return null;
            return document.getElementById(stored.id);
        } catch {
            return null;
        }
    }

    function rememberPane(pane) {
        try {
            if (pane) window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ id: pane.id, at: Date.now() }));
        } catch {
            /* storage unavailable: the page just opens on the default tab */
        }
    }

    // Priority: a tab with validation errors, then the tab a form was sent from, then the URL hash.
    const errorPane = paneOf(document.querySelector(".tab-pane .input-validation-error, .tab-pane .field-validation-error"));
    const storedPane = readStoredPane();
    if (!showPane(errorPane) && !showPane(storedPane)) showFromHash();

    buttons.forEach((button) => {
        button.addEventListener("shown.bs.tab", () => {
            window.history.replaceState(window.history.state, "", `#${button.dataset.settingsTab}`);
        });
    });

    // Confirmed delete-forms submit programmatically (no submit event), so also catch the click.
    document.querySelectorAll(".tab-pane form").forEach((form) => {
        const remember = () => rememberPane(paneOf(form));
        form.addEventListener("submit", remember);
        form.querySelectorAll('button[type="submit"]').forEach((b) => b.addEventListener("click", remember));
    });

    // htmx re-runs this script on every visit; keep a single hash listener.
    if (window.__accountSettingsHashHandler) window.removeEventListener("hashchange", window.__accountSettingsHashHandler);
    window.__accountSettingsHashHandler = showFromHash;
    window.addEventListener("hashchange", showFromHash);
})(window, document);
