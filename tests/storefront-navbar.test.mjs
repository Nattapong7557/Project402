import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const [layout, stylesheet, adminSidebar] = await Promise.all([
    readFile(new URL('../Views/Shared/_Layout.cshtml', import.meta.url), 'utf8'),
    readFile(new URL('../wwwroot/css/cafe.css', import.meta.url), 'utf8'),
    readFile(new URL('../Views/Shared/_AdminSidebar.cshtml', import.meta.url), 'utf8')
]);

test('the shared layout enables the hero navbar by page rather than account role', () => {
    const heroCondition = layout.match(/var hasStorefrontHero = ([^;]+);/)?.[1];

    assert.equal(heroCondition, 'isHome || IsSectionDefined("StorefrontHero")');
    assert.ok(layout.includes('hasStorefrontHero ? "storefront-hero-layout" : ""'));
    assert.ok(layout.includes('<partial name="_SiteHeader" />'));
});

test('the navbar overlays the hero with a transparent borderless surface', () => {
    const heroHeader = stylesheet.match(/\.storefront-hero-layout \.site-header\s*\{([^}]+)\}/)?.[1];

    assert.match(heroHeader, /position:\s*relative;/);
    assert.match(heroHeader, /margin-bottom:\s*calc\(-1 \* var\(--site-nav-height\)\);/);
    assert.match(heroHeader, /border:\s*0;/);
    assert.match(heroHeader, /background:\s*transparent;/);
});

test('the admin sidebar emits no leading text above the storefront navbar', () => {
    assert.match(adminSidebar, /^\s*@\{/);
});

test('the expanded compact menu has a readable panel over the hero', () => {
    const mobilePanel = stylesheet.match(/\.site-header \.site-nav-collapse:is\(\.show, \.collapsing\)\s*\{([^}]+)\}/)?.[1];

    assert.match(mobilePanel, /position:\s*var\(--site-nav-panel-position, absolute\);/);
    assert.match(mobilePanel, /top:\s*100%;/);
    assert.match(mobilePanel, /background:\s*var\(--site-nav-panel-background, #[\da-f]{6}\);/i);
});

test('storefront menu hover changes text color and leaves the selection underline to the indicator', async () => {
    const hover = stylesheet.match(/\.site-header \.site-nav-link:hover,[^{}]+\{([^}]+)\}/)?.[1];
    const motion = await readFile(new URL('../wwwroot/css/motion.css', import.meta.url), 'utf8');
    const active = motion.match(/\.nav-indicator-ready \[data-nav-key\]\[aria-current\]\s*\{([^}]+)\}/)?.[1];

    assert.match(hover, /color:/);
    assert.doesNotMatch(hover, /text-decoration:/);
    assert.match(active, /text-decoration:\s*none/);
});
