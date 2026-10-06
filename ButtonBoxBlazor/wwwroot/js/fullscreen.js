export function initialize(button, status) {
    const root = document.documentElement;
    const requestFullscreen = root.requestFullscreen ?? root.webkitRequestFullscreen;
    const exitFullscreen = document.exitFullscreen ?? document.webkitExitFullscreen;
    const supported = Boolean(requestFullscreen && exitFullscreen &&
        (document.fullscreenEnabled ?? document.webkitFullscreenEnabled ?? true));
    let pending = false;

    function update() {
        const active = Boolean(document.fullscreenElement ?? document.webkitFullscreenElement);
        const label = active ? "Exit fullscreen" : "Enter fullscreen";
        button.setAttribute("aria-pressed", String(active));
        button.setAttribute("aria-label", label);
        button.title = supported ? label : "Fullscreen is not supported by this browser";
        button.querySelector("[data-fullscreen-label]").textContent = active ? "Exit fullscreen" : "Fullscreen";
        button.disabled = !supported || pending;
    }

    async function toggle(event) {
        // Call directly during the tap to preserve user activation on tablets.
        // Keep the mobile menu open so the button remains available to exit.
        event.stopPropagation();
        if (!supported || pending) return;

        pending = true;
        status.textContent = "";
        update();
        try {
            if (document.fullscreenElement ?? document.webkitFullscreenElement) {
                await exitFullscreen.call(document);
            } else {
                await requestFullscreen.call(root);
            }
        } catch {
            status.textContent = "Fullscreen could not be changed. Please try again.";
        } finally {
            pending = false;
            update();
        }
    }

    button.addEventListener("click", toggle);
    document.addEventListener("fullscreenchange", update);
    document.addEventListener("webkitfullscreenchange", update);
    if (!supported) {
        status.textContent = "Fullscreen is not supported by this browser.";
    }
    update();

    return {
        dispose() {
            button.removeEventListener("click", toggle);
            document.removeEventListener("fullscreenchange", update);
            document.removeEventListener("webkitfullscreenchange", update);
        }
    };
}
