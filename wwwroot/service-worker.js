
const CACHE_NAME = "localbite-offline-v1";

const OFFLINE_PAGE = "/Home/Offline";

const OFFLINE_ASSETS = [
    OFFLINE_PAGE,
    "/images/branding/restaurant-mark.svg",
    "/images/branding/offline-illustration.svg"
];

// Install: save the offline page and its essential images.
self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(OFFLINE_ASSETS))
            .then(() => self.skipWaiting())
    );
});

// Activate: remove older LocalBite caches.
self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(cacheNames =>
            Promise.all(
                cacheNames
                    .filter(name =>
                        name.startsWith("localbite-") &&
                        name !== CACHE_NAME
                    )
                    .map(name => caches.delete(name))
            )
        ).then(() => self.clients.claim())
    );
});

// Navigation: use the website when online,
// otherwise display the saved offline page.
self.addEventListener("fetch", event => {
    const request = event.request;

    if (request.method !== "GET") return;

    if (request.mode === "navigate") {
        event.respondWith(
            fetch(request).catch(async () => {
                const cache = await caches.open(CACHE_NAME);

                return await cache.match(OFFLINE_PAGE) ||
                    new Response(
                        "You are offline. Please reconnect and try again.",
                        {
                            status: 503,
                            headers: {
                                "Content-Type": "text/plain"
                            }
                        }
                    );
            })
        );
    }
});
