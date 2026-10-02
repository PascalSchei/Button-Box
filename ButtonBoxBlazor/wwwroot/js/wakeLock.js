// Keeps the tablet display on while the app is active, using the Screen Wake Lock API.
let sentinel = null;

export async function requestWakeLock() {
    if (!("wakeLock" in navigator)) {
        return false;
    }

    try {
        sentinel = await navigator.wakeLock.request("screen");
        sentinel.addEventListener("release", () => {
            sentinel = null;
        });
        return true;
    } catch {
        // Rejected e.g. due to low battery, power-save mode or an insecure context - app keeps working without it.
        sentinel = null;
        return false;
    }
}

export async function releaseWakeLock() {
    if (sentinel === null) {
        return;
    }

    try {
        await sentinel.release();
    } catch {
        // Already released by the browser (e.g. tab hidden).
    }
    sentinel = null;
}

// The browser auto-releases the lock when the tab is hidden, so re-acquire it once it's visible again.
document.addEventListener("visibilitychange", async () => {
    if (sentinel === null && document.visibilityState === "visible") {
        await requestWakeLock();
    }
});
