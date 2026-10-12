// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Browser scenario for a LIVE Stage (the cratis/stage image) that was handed an approved custom runtime package: the package
// set mounted at /packages/approved-packages.json and the browser build of the package at /packages/@acme/scene.charts. It asserts
// the Stage loaded the package at runtime - an ES module imported from the Stage, rendering with the Stage's own React through
// the import map - and rendered the authored `component Acme.Charts.chart` from it, with the shipped components beside it.
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
