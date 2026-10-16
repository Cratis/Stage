// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Asserts in a browser that the live Stage rendered the screens an application builds from a domain blueprint's templates
// (`template blueprint <package> <template>`): each such screen mounts the template, shows content and reports no package or
// template diagnostic, and neither the packages the Stage serves nor the page carry the blueprint's design-time sample data (SD6).
const { readFileSync, writeFileSync } = require('node:fs');
const { join } = require('node:path');
const { chromium } = require(join(__dirname, '..', 'generated-react-browser', 'node_modules', 'playwright'));

const baseUrl = process.env.STAGE_LIVE_URL ?? 'http://127.0.0.1:9090';
const resultPath = process.env.STAGE_LIVE_RESULT ?? 'live-blueprints.json';
const playPath = process.env.STAGE_LIVE_MODEL;
const name = process.env.STAGE_LIVE_NAME ?? 'blueprint';
const approvedPath = process.env.STAGE_LIVE_APPROVED;
const manifest = JSON.parse(readFileSync(approvedPath, 'utf8')).packages[0].manifest;

const screens = [...readFileSync(playPath, 'utf8').matchAll(/^\s*screen\s+(\w+)\s*\n\s*template blueprint\s+(\S+)\s+(\w+)/gm)]
    .map(match => ({ screen: match[1], blueprint: match[2], template: match[3] }));

(async () => {
    const result = { name: `live-blueprints-${name}`, baseUrl, status: 'unknown', remainingBlockers: [], assertions: [] };
    const record = (path, status, value) => result.assertions.push({ path, status, value });
    const block = (path, reason) => { record(path, 'blocked', reason); result.remainingBlockers.push(`${path}: ${reason}`); };
    let exitCode = 2;
    let browser;
    try {
        if (screens.length === 0) throw new Error(`${playPath} has no screen built from a blueprint template`);
        browser = await chromium.launch();
        const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));

        // Only a screen template fills a route. A dialog template opens from a toolbar button. A reference to a template the package does
        // not ship is a defect, never a note: the sample names a template the package does not provide, and rendering it would show a
        // screen with no template (Cratis/Stage#457).
        const notScreens = screens.filter(({ template }) => !manifest.screenTemplates.includes(template));
        for (const { screen, template } of notScreens) {
            if (manifest.dialogTemplates.includes(template)) {
                record(`live.blueprint.${name}.${screen}`, 'noted', `${template} is a dialog template, opened from a button rather than routed to`);
            } else {
                block(`live.blueprint.${name}.${screen}`, `${template} is not shipped by ${manifest.name} ${manifest.version}`);
            }
        }

        for (const { screen, template } of screens.filter(candidate => !notScreens.includes(candidate))) {
            const path = `live.blueprint.${name}.${screen}`;
            await page.goto(`${baseUrl}/#/${screen}`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
            const mounted = page.locator(`[data-scene-id="template-${screen}-${template}"]`);
            try {
                await mounted.waitFor({ timeout: 20_000 });
                await page.waitForFunction(selector => (document.querySelector(selector)?.innerText ?? '').trim().length > 0, `[data-scene-id="template-${screen}-${template}"]`, { timeout: 20_000 });
                const alert = await page.locator('#root[role="alert"]').count();
                alert === 0
                    ? record(path, 'passed', `${template} mounted with content`)
                    : block(path, `${template} mounted but the Stage reported: ${await page.locator('#root').innerText()}`);
            } catch {
                block(path, `${template} did not mount with content; page errors: ${errors.join(' | ') || 'none'}; page: ${(await page.locator('#root').innerText()).slice(0, 300)}`);
            }
        }

        errors.length === 0
            ? record('live.blueprint.noPageErrors', 'passed')
            : block('live.blueprint.noPageErrors', errors.join(' | '));

        const served = await (await fetch(`${baseUrl}/stage/packages`)).text();
        /sampleData|"sampleData"/.test(served)
            ? block('live.blueprint.noSampleDataServed', 'the packages the Stage serves carry the blueprint sample data')
            : record('live.blueprint.noSampleDataServed', 'passed', 'the approved set was accepted with its blueprint members and serves none of them');

        const passed = result.assertions.filter(assertion => assertion.status === 'passed').length;
        result.status = result.remainingBlockers.length > 0 ? 'failed' : 'passed';
        result.summary = `${name}: ${passed} assertions passed, ${result.remainingBlockers.length} blocked (${screens.length - notScreens.length} blueprint screens, ${notScreens.length} noted)`;
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
