// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The assertions the blueprint-template vector runs, identically, against the live Stage and against a generated React application
// (which are one runtime): the default blueprint's MasterDetail, Dashboard and ProfileSettings templates applied to the screens of
// model/templates.play (Cratis/Scene#95).
//
// For every screen and every viewport (desktop, tablet, phone) in both colour schemes:
//   - the template mounted, and every slot it fills carries its content;
//   - the layout answers the viewport: the master/detail side panel sits beside the list on an expanded window and under it on a phone,
//     the dashboard's secondary column beside the primary one and under it on a phone, and nothing overflows the window sideways;
//   - the page is accessible in the browser: axe-core WCAG 2.0/2.1/2.2 A and AA - colour contrast and target size included - and the
//     landmark structure (one main, no duplicates, unique names, content inside a landmark, a level-one heading).
// A control breaks the page it just judged and expects every kind of finding, so a clean run is never vacuous.
const { show, schemes, formatViolations } = require('./contrast.cjs');

const viewports = { desktop: { width: 1440, height: 900 }, tablet: { width: 768, height: 1024 }, phone: { width: 375, height: 667 } };

// Slots are the ids the blueprint's template content gives its elements; a slot is filled when every id is on the page with some content.
const screens = [
    { screen: 'Customers', template: 'MasterDetail', slots: { header: ['master-header'], primary: ['customers-table'], sidePanel: ['customer-detail'] } },
    { screen: 'Overview', template: 'Dashboard', slots: { stats: ['stat-revenue', 'stat-orders', 'stat-customers', 'stat-open'], primary: ['widget-revenue', 'widget-orders'], secondary: ['widget-activity', 'widget-capacity'] } },
    { screen: 'Profile', template: 'ProfileSettings', slots: { header: ['profile-header'], body: ['profile-identity', 'profile-security'], actions: ['profile-actions'] } },
];

const landmarkRules = [
    'landmark-one-main', 'landmark-no-duplicate-main', 'landmark-no-duplicate-banner', 'landmark-no-duplicate-contentinfo', 'landmark-unique',
    'landmark-main-is-top-level', 'landmark-banner-is-top-level', 'landmark-contentinfo-is-top-level', 'landmark-complementary-is-top-level', 'region', 'page-has-heading-one',
];

const box = (page, id) => page.evaluate(identifier => {
    const element = document.querySelector(`[data-scene-id="${identifier}"]`);
    if (!element) return null;
    const rectangle = element.getBoundingClientRect();
    return { x: rectangle.x, y: rectangle.y, width: rectangle.width, height: rectangle.height, text: (element.innerText ?? '').trim().length };
}, id);

async function audit(page) {
    const violations = await page.evaluate(async rules => {
        const axe = window.axe;
        const wcag = await axe.run(document.body, { runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] }, rules: { region: { enabled: false } } });
        const landmarks = await axe.run(document.body, { runOnly: { type: 'rule', values: rules } });
        return [...wcag.violations, ...landmarks.violations];
    }, landmarkRules);
    return formatViolations(violations);
}

const overflowOf = page => page.evaluate(() => (document.documentElement.scrollWidth > window.innerWidth ? `the page is ${document.documentElement.scrollWidth}px wide in a ${window.innerWidth}px window` : null));

/**
 * Runs the vector.
 * @param {{ page: import('playwright').Page, baseUrl: string, errors: string[], prefix: string, record: Function, block: Function }} context
 */
async function run({ page, baseUrl, errors, prefix, record, block }) {
    for (const [viewportName, size] of Object.entries(viewports)) {
        await page.setViewportSize(size);
        for (const scheme of schemes) {
            for (const screen of screens) {
                const where = `${prefix}.${screen.screen}.${viewportName}.${scheme}`;
                try {
                    await show(page, baseUrl, screen, scheme);
                } catch (error) {
                    block(`${where}.mounts`, `${screen.template} did not mount: ${error.message.split('\n')[0]}`);
                    continue;
                }

                const missing = [];
                for (const [slot, ids] of Object.entries(screen.slots)) {
                    for (const id of ids) {
                        const found = await box(page, id);
                        if (!found || found.text === 0) missing.push(`${slot}:${id}`);
                    }
                }
                missing.length === 0 ? record(`${where}.slotsFilled`, 'passed', Object.keys(screen.slots).join(', ')) : block(`${where}.slotsFilled`, `empty or missing: ${missing.join(', ')}`);

                const overflow = await overflowOf(page);
                overflow === null ? record(`${where}.noOverflow`, 'passed') : block(`${where}.noOverflow`, overflow);

                const arrangement = await arrangementOf(page, screen, viewportName);
                if (arrangement !== undefined) arrangement === null ? record(`${where}.layout`, 'passed') : block(`${where}.layout`, arrangement);

                const violations = await audit(page);
                violations.length === 0 ? record(`${where}.accessible`, 'passed') : block(`${where}.accessible`, violations.join(' | '));
            }
        }
    }

    errors.length === 0 ? record(`${prefix}.noPageErrors`, 'passed') : block(`${prefix}.noPageErrors`, errors.join(' | '));
    await control({ page, baseUrl, prefix, record, block });
}

/** Whether the screen's slots are arranged as the template says for the viewport: null when they are, a reason when they are not, undefined when it says nothing. */
async function arrangementOf(page, screen, viewportName) {
    const wide = viewportName !== 'phone';
    if (screen.template === 'MasterDetail') {
        const list = await box(page, 'customers-table');
        const detail = await box(page, 'customer-detail');
        if (!list || !detail) return 'the list or the side panel is missing';
        const beside = detail.x >= list.x + list.width - 1 && Math.abs(detail.y - list.y) < 4;
        const under = detail.y >= list.y + list.height - 1 && Math.abs(detail.x - list.x) < 4;
        if (wide) return beside ? null : `on a ${viewportName} the side panel is not beside the list (list ${JSON.stringify(list)}, panel ${JSON.stringify(detail)})`;
        return under ? null : `on a phone the side panel is not under the list (list ${JSON.stringify(list)}, panel ${JSON.stringify(detail)})`;
    }

    if (screen.template === 'Dashboard') {
        const primary = await box(page, 'widget-revenue');
        const secondary = await box(page, 'widget-activity');
        if (!primary || !secondary) return 'the primary or the secondary column is missing';
        const beside = secondary.x >= primary.x + primary.width - 1 && Math.abs(secondary.y - primary.y) < 4;
        const under = secondary.y >= primary.y + primary.height - 1 && Math.abs(secondary.x - primary.x) < 4;
        if (wide) return beside ? null : `on a ${viewportName} the secondary column is not beside the primary one (primary ${JSON.stringify(primary)}, secondary ${JSON.stringify(secondary)})`;
        return under ? null : `on a phone the secondary column is not under the primary one (primary ${JSON.stringify(primary)}, secondary ${JSON.stringify(secondary)})`;
    }

    return undefined;
}

/** Breaks the page just judged and expects each kind of finding, so a clean audit is never vacuous. */
async function control({ page, baseUrl, prefix, record, block }) {
    await page.setViewportSize(viewports.desktop);
    await show(page, baseUrl, screens[0], 'light');
    const found = await page.evaluate(async rules => {
        document.body.insertAdjacentHTML('beforeend', '<p style="color:#c8c8c8;background:#ffffff">Pale on white</p><main>Second</main><button style="width:10px;height:10px;padding:0">x</button><button style="width:10px;height:10px;padding:0">y</button>');
        const axe = window.axe;
        const wcag = await axe.run(document.body, { runOnly: { type: 'tag', values: ['wcag2aa', 'wcag22aa'] } });
        const landmarks = await axe.run(document.body, { runOnly: { type: 'rule', values: rules } });
        return [...wcag.violations, ...landmarks.violations].map(violation => violation.id);
    }, landmarkRules);
    const expected = ['color-contrast', 'landmark-no-duplicate-main', 'region', 'target-size'];
    const lacking = expected.filter(id => !found.includes(id));
    lacking.length === 0 ? record(`${prefix}.auditIsNotVacuous`, 'passed', expected.join(', ')) : block(`${prefix}.auditIsNotVacuous`, `the audit did not report: ${lacking.join(', ')}`);
}

module.exports = { run, screens, viewports };
