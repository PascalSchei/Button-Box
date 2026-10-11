const key = "buttonbox.device";

export function load() {
    try {
        return localStorage.getItem(key);
    } catch {
        return null;
    }
}

export function save(json) {
    try {
        localStorage.setItem(key, json);
    } catch {
        // Storage blocked or full: settings just do not persist.
    }
}

export function getViewport() {
    return { width: window.innerWidth, height: window.innerHeight };
}
