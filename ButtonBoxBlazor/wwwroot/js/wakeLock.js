// Keeps the tablet display on while the app is active: Screen Wake Lock API, falling back to a playing hidden video.
let sentinel = null;
let wanted = false;
let fallbackVideo = null;
let fallbackTimer = null;

async function requestNative() {
    if (sentinel !== null) {
        return true;
    }

    // navigator.wakeLock only exists in secure contexts (HTTPS/localhost), not on http://<LAN-IP>.
    if (!("wakeLock" in navigator)) {
        return false;
    }

    try {
        const lock = await navigator.wakeLock.request("screen");
        lock.addEventListener("release", () => {
            if (sentinel === lock) {
                sentinel = null;
            }
        });
        sentinel = lock;
        return true;
    } catch {
        // Rejected e.g. due to low battery or power-save mode.
        return false;
    }
}

// A playing video keeps the screen on in mobile browsers even on insecure origins.
async function startFallback() {
    if (fallbackVideo === null) {
        const canvas = document.createElement("canvas");
        canvas.width = 16;
        canvas.height = 16;
        const ctx = canvas.getContext("2d");
        let tick = false;
        const draw = () => {
            tick = !tick;
            ctx.fillStyle = tick ? "#000" : "#010101";
            ctx.fillRect(0, 0, canvas.width, canvas.height);
        };
        draw();
        // Changing frames are needed, otherwise the stream stalls and the browser stops treating it as playing.
        fallbackTimer = setInterval(draw, 1000);

        const video = document.createElement("video");
        video.muted = true;
        video.playsInline = true;
        video.setAttribute("aria-hidden", "true");
        video.srcObject = canvas.captureStream(1);
        video.style.cssText = "position:fixed;left:0;bottom:0;width:2px;height:2px;opacity:0.01;pointer-events:none;";
        document.body.appendChild(video);
        fallbackVideo = video;
    }

    try {
        await fallbackVideo.play();
        return true;
    } catch {
        // Autoplay blocked until the first user gesture - retried in the pointerdown handler below.
        return false;
    }
}

function stopFallback() {
    if (fallbackTimer !== null) {
        clearInterval(fallbackTimer);
        fallbackTimer = null;
    }

    if (fallbackVideo !== null) {
        fallbackVideo.pause();
        fallbackVideo.srcObject = null;
        fallbackVideo.remove();
        fallbackVideo = null;
    }
}

async function acquire() {
    if (await requestNative()) {
        stopFallback();
        return true;
    }

    return await startFallback();
}

export async function requestWakeLock() {
    wanted = true;
    return await acquire();
}

export async function releaseWakeLock() {
    wanted = false;
    stopFallback();

    if (sentinel === null) {
        return;
    }

    const lock = sentinel;
    sentinel = null;
    try {
        await lock.release();
    } catch {
        // Already released by the browser (e.g. tab hidden).
    }
}

// The browser auto-releases the lock / pauses the video when the tab is hidden, so re-acquire once visible again.
document.addEventListener("visibilitychange", async () => {
    if (wanted && document.visibilityState === "visible") {
        await acquire();
    }
});

// Video autoplay may need a user gesture; retry on the first tap if nothing is holding the screen awake yet.
document.addEventListener("pointerdown", async () => {
    if (wanted && sentinel === null && (fallbackVideo === null || fallbackVideo.paused)) {
        await acquire();
    }
});
