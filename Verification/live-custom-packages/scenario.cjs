// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Browser scenario for a LIVE Stage (the cratis/stage image) that was handed an approved custom runtime package: the package
// set mounted at /packages/approved-packages.json and the browser build of the package at /packages/@acme/scene.charts. It asserts
// the Stage loaded the package at runtime - an ES module imported from the Stage, rendering with the Stage's own React through
// the import map - and rendered the authored `component Acme.Charts.chart` from it, with the shipped components beside it.
//
// It also asserts the responsive application shell (`browser.responsive.*`), the same assertions the generated React scenarios run.
//
// With `--expect-refusal` the package folder is missing; it asserts the Stage refused to render and said why.
//
// Environment: STAGE_LIVE_URL (the Stage), STAGE_LIVE_RESULT (where to write the result json).
// Exit codes: 0 every assertion passed, 1 an assertion found a defect, 2 the scenario could not run.

const { writeFileSync } = require('node:fs');
const path = require('node:path');
const { chromium } = require(path.join(__dirname, '..', 'generated-react-browser', 'node_modules', 'playwright'));

const baseUrl = process.env.STAGE_LIVE_URL;
const resultPath = process.env.STAGE_LIVE_RESULT ?? 'live-custom-packages-result.json';
const expectRefusal = process.argv.includes('--expect-refusal');

const record = (result, name, status, value) => result.assertions.push({ path: name, status, value });
const block = (result, name, reason) => { record(result, name, 'blocked', reason); result.remainingBlockers.push(`${name}: ${reason}`); };

// The application shell adapts to the window: expanded, the navigation is beside the content; compact, it collapses off the
// canvas behind a menu toggle that opens it, and nothing overflows the window sideways. The same scenario runs against a live Stage
// and a generated application, which are one runtime.
async function assertResponsiveShell(browser, result, baseUrl) {
    const measure = page => page.evaluate(() => {
        const nav = document.querySelector('nav');
        const box = nav?.getBoundingClientRect();
        const content = document.querySelector('main, [role=main]') ?? document.body;
        const contentBox = content.getBoundingClientRect();
        return {
            scrollWidth: document.documentElement.scrollWidth,
            innerWidth,
            navLeft: box ? Math.round(box.left) : null,
            navRight: box ? Math.round(box.right) : null,
            contentRight: Math.round(contentBox.right),
            toggles: Array.from(document.querySelectorAll('button')).map(button => button.getAttribute('aria-label') ?? button.textContent?.trim() ?? '')
        };
    });

    const open = async (width, height) => {
        const page = await browser.newPage({ viewport: { width, height } });
        await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
        await page.locator('nav').first().waitFor({ state: 'attached', timeout: 20_000 });
        await page.waitForTimeout(1_000);
        return page;
    };

    const expanded = await open(1440, 900);
    const wide = await measure(expanded);
    if (wide.navLeft !== null && wide.navLeft >= 0 && wide.navRight <= wide.innerWidth) record(result, 'browser.responsive.expanded.navigationBesideContent', 'passed', `nav ${wide.navLeft}..${wide.navRight} of ${wide.innerWidth}`);
    else block(result, 'browser.responsive.expanded.navigationBesideContent', `expected the navigation inside the 1440px window, got ${JSON.stringify(wide)}`);
    if (wide.scrollWidth <= wide.innerWidth) record(result, 'browser.responsive.expanded.noHorizontalOverflow', 'passed');
    else block(result, 'browser.responsive.expanded.noHorizontalOverflow', `scroll width ${wide.scrollWidth} exceeds ${wide.innerWidth}`);
    await expanded.close();

    const compact = await open(390, 800);
    const narrow = await measure(compact);
    if (narrow.navRight !== null && narrow.navRight <= 0) record(result, 'browser.responsive.compact.navigationCollapsed', 'passed', `nav ${narrow.navLeft}..${narrow.navRight}`);
    else block(result, 'browser.responsive.compact.navigationCollapsed', `expected the navigation off the canvas at 390px, got ${JSON.stringify(narrow)}`);
    if (narrow.scrollWidth <= narrow.innerWidth) record(result, 'browser.responsive.compact.noHorizontalOverflow', 'passed');
    else block(result, 'browser.responsive.compact.noHorizontalOverflow', `scroll width ${narrow.scrollWidth} exceeds ${narrow.innerWidth}`);
    if (narrow.contentRight <= narrow.innerWidth + 1) record(result, 'browser.responsive.compact.contentFitsWindow', 'passed', `content right edge ${narrow.contentRight}`);
    else block(result, 'browser.responsive.compact.contentFitsWindow', `content extends to ${narrow.contentRight} in a ${narrow.innerWidth}px window`);

    const toggle = compact.getByRole('button', { name: /open the menu/i });
    if (await toggle.count() === 0) {
        block(result, 'browser.responsive.compact.menuToggle', `no "Open the menu" control; buttons: ${JSON.stringify(narrow.toggles)}`);
    } else {
        record(result, 'browser.responsive.compact.menuToggle', 'passed');
        await toggle.first().click();
        await compact.waitForTimeout(600);
        const opened = await measure(compact);
        if (opened.navLeft !== null && opened.navLeft >= 0 && opened.navRight <= opened.innerWidth) record(result, 'browser.responsive.compact.menuOpens', 'passed', `nav ${opened.navLeft}..${opened.navRight}`);
        else block(result, 'browser.responsive.compact.menuOpens', `the menu did not bring the navigation into the window: ${JSON.stringify(opened)}`);
        if (await compact.getByRole('button', { name: /close the menu/i }).count() > 0) record(result, 'browser.responsive.compact.menuToggleReflectsState', 'passed');
        else block(result, 'browser.responsive.compact.menuToggleReflectsState', 'the toggle still offers to open the menu once it is open');
    }

    await compact.close();
}


(async () => {
    const result = { name: expectRefusal ? 'live-custom-packages-refusal' : 'live-custom-packages', baseUrl, status: 'unknown', remainingBlockers: [], assertions: [] };
    const record = (name, status, value) => result.assertions.push({ path: name, status, value });
    const block = (name, reason) => { record(name, 'blocked', reason); result.remainingBlockers.push(`${name}: ${reason}`); };
    let exitCode = 2;
    let browser;
    try {
        browser = await chromium.launch();
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });

        const chart = page.locator('[data-acme-chart="salesChart"]');
        if (expectRefusal) {
            const alert = page.locator('#root[role="alert"]');
            try {
                await alert.waitFor({ timeout: 20_000 });
                const text = await alert.innerText();
                text.includes("Package 'Acme.Charts'")
                    ? record('live.customPackage.refusal', 'passed', 'the Stage refused to render and named the package that is missing')
                    : block('live.customPackage.refusal', `the refusal does not name the package: ${text}`);
            } catch {
                block('live.customPackage.refusal', 'the Stage rendered without the approved package instead of refusing');
            }

            (await chart.count()) === 0
                ? record('live.customPackage.notRendered', 'passed', 'no custom component rendered')
                : block('live.customPackage.notRendered', 'a custom component rendered although its package is missing');
        } else {
            try {
                await chart.waitFor({ timeout: 20_000 });
                record('live.customPackage.rendered', 'passed', 'the authored Acme.Charts.chart rendered from the package the live Stage loaded');
            } catch {
                block('live.customPackage.rendered', `no [data-acme-chart="salesChart"] element; page errors: ${errors.join(' | ') || 'none'}`);
            }

            if (await chart.count() > 0) {
                const text = (await chart.first().innerText()).trim();
                text === 'Sales by week'
                    ? record('live.customPackage.property', 'passed', text)
                    : block('live.customPackage.property', `expected the authored title 'Sales by week', got '${text}'`);

                const border = await chart.first().evaluate(element => getComputedStyle(element).borderTopStyle);
                border === 'solid'
                    ? record('live.customPackage.styles', 'passed', 'the package style sheet was applied')
                    : block('live.customPackage.styles', `expected the package style sheet to give the chart a solid border, got '${border}'`);
            }

            try {
                await page.getByText('Work items', { exact: false }).first().waitFor({ timeout: 10_000 });
                record('live.customPackage.shippedComponents', 'passed', 'the shipped Cratis components render beside the custom package');
            } catch {
                block('live.customPackage.shippedComponents', 'the shipped Cratis components did not render');
            }

            (await page.locator('#root[role="alert"]').count()) === 0
                ? record('live.customPackage.noLoadDiagnostics', 'passed', 'the Stage reported no package load diagnostics')
                : block('live.customPackage.noLoadDiagnostics', await page.locator('#root').innerText());

            await assertResponsiveShell(browser, result, baseUrl);
        }

        const passed = result.assertions.filter(assertion => assertion.status === 'passed').length;
        result.status = result.remainingBlockers.length > 0 ? 'failed' : 'passed';
        result.summary = `${passed} assertions passed, ${result.remainingBlockers.length} blocked`;
        exitCode = result.remainingBlockers.length > 0 ? 1 : 0;
    } catch (error) {
        result.status = 'could-not-run';
        result.message = error.stack ?? error.message;
    } finally {
        if (browser) await browser.close();
        writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    }

    console.log(result.summary ?? result.message);
    for (const blocker of result.remainingBlockers) console.log(`BLOCKED ${blocker}`);
    process.exit(exitCode);
})();
