
// =========================================
// LOCALBITE SCROLL REVEAL SYSTEM
// =========================================

document.addEventListener("DOMContentLoaded", () => {

    const selectors = [
        ".home-hero",
        ".section-heading",
        ".food-category-card",
        ".process-section",
        ".process-section .col-md-4",
        ".home-final-cta",
        ".hero-image-wrap",
        ".hero-image-glass"
    ];

    const elements = document.querySelectorAll(selectors.join(","));

    console.log("LocalBite animation elements found:", elements.length);

    const directions = ["up", "left", "right", "zoom", "down"];

    elements.forEach((element, index) => {

        element.setAttribute(
            "data-reveal",
            directions[index % directions.length]
        );

        if (element.classList.contains("food-category-card")) {
            element.style.setProperty(
                "--reveal-delay",
                `${(index % 4) * 120}ms`
            );
        }
    });

    // Respect accessibility preferences
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        elements.forEach(element => element.classList.add("revealed"));
        return;
    }

    // Reveal elements already visible on the screen
    const observer = new IntersectionObserver((entries, observer) => {

        entries.forEach(entry => {

            if (entry.isIntersecting) {
                entry.target.classList.add("revealed");
                observer.unobserve(entry.target);
            }

        });

    }, {
        threshold: 0.05,
        rootMargin: "0px 0px -30px 0px"
    });

    elements.forEach(element => observer.observe(element));

});
