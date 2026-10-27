// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Browser assertions against a live Stage for navigation regions, toolbar slice dialogs, deep links and the absence of sample data.
const { chromium } = require('playwright');
const fs = require('fs');

const base = process.env.STAGE_LIVE_URL;
const resultPath = process.env.STAGE_LIVE_RESULT;
const shots = process.env.STAGE_LIVE_SCREENSHOTS;
const failures = [];
const passed = [];

function check(name, actual, expected) {
    const same = JSON.stringify(actual) === JSON.stringify(expected);
    (same ? passed : failures).push(same ? name : `${name}: expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
}

(async () => {
    const browser = await chromium.launch();
    const page = await browser.newPage({ viewport: { width: 1500, height: 900 } });
    const errors = [];
    page.on('pageerror', error => errors.push(String(error)));
    const posts = [];
    page.on('request', request => { if (request.method() === 'POST' && request.url().includes('/api/')) posts.push({ url: new URL(request.url()).pathname, body: JSON.parse(request.postData() || '{}') }); });
    const items = async label => (await page.locator(`nav[aria-label="${label}"] a, nav[aria-label="${label}"] button`).allInnerTexts()).map(text => text.trim());
    const active = async label => page.locator(`nav[aria-label="${label}"] [aria-current]`).allInnerTexts();

    try {
        const scene = await (await fetch(`${base}/stage/scene`)).json();
        check('the served scene carries no sample data packs', [scene.sampleDataPacks, scene.blueprintSampleDataPacks], [[], []]);
        check('the served scene carries no sample data selection in a profile', scene.uiProfiles.every(profile => profile.sampleData === undefined || profile.sampleData === null), true);

        await page.goto(base);
        await page.waitForSelector('.layout-topbar');
        check('the topbar shows the application regions', await items('Modules'), ['Sales', 'Support']);
        check('the sidebar shows the application items outside a module', await items('Sidebar'), ['Home']);
        check('the logo is the application\'s', (await page.locator('.layout-logo').first().innerText()).includes('Acme'), true);

        await page.locator('nav[aria-label="Modules"]').getByText('Sales', { exact: true }).click();
        await page.getByRole('button', { name: 'New order' }).waitFor();
        check('the module overrides the sidebar', await items('Sidebar'), ['Orders', 'Customers']);
        check('the module is active in the topbar', await active('Modules'), ['Sales']);
        check('the feature is active in the sidebar', await active('Sidebar'), ['Orders']);
        check('the address records where the user is', new URL(page.url()).pathname, '/List');
        if (shots) await page.screenshot({ path: `${shots}/live-sales.png` });

        await page.getByRole('button', { name: 'New order' }).click();
        const dialog = page.locator('.scene-slice-dialog');
        await dialog.waitFor();
        check('the slice button opens its task dialog', [await dialog.locator('h2').innerText(), await dialog.locator('.scene-slice-dialog-subtitle').innerText()], ['New order', 'State Change · PlaceOrder']);
        check('the dialog fields are the command schema', await dialog.locator('input').evaluateAll(inputs => inputs.map(input => [input.name, input.type])), [['orderId', 'text'], ['customer', 'text'], ['quantity', 'number']]);
        if (shots) await page.screenshot({ path: `${shots}/live-slice-dialog.png` });
        await dialog.locator('input[name=orderId]').fill('11111111-1111-4111-8111-111111111111');
        await dialog.locator('input[name=customer]').fill('Fjordline AS');
        await dialog.locator('input[name=quantity]').fill('4');
        await dialog.locator('button[type=submit]').click();
        await page.locator('[role=dialog]').waitFor({ state: 'detached' });
        check('submitting posts the command to the route the Stage registered', posts, [{ url: '/api/sales/orders', body: { orderId: '11111111-1111-4111-8111-111111111111', customer: 'Fjordline AS', quantity: 4 } }]);

        const link = page.url();
        const second = await browser.newPage({ viewport: { width: 1500, height: 900 } });
        await second.goto(link);
        await second.waitForSelector('.layout-topbar');
        check('a deep link restores the module sidebar', (await second.locator('nav[aria-label="Sidebar"] a, nav[aria-label="Sidebar"] button').allInnerTexts()).map(text => text.trim()), ['Orders', 'Customers']);
        check('a deep link restores the page', (await second.locator('[data-scene-outlet="primary"] h1').allInnerTexts()).includes('Orders'), true);

        await page.locator('nav[aria-label="Sidebar"]').getByText('Customers').click();
        await page.waitForFunction(() => location.pathname === '/CustomerScreen');
        check('another feature of the module becomes active', await active('Sidebar'), ['Customers']);
        await page.goBack();
        await page.waitForFunction(() => location.pathname === '/List');
        check('back returns to the previous feature', await active('Sidebar'), ['Orders']);

        await page.locator('nav[aria-label="Modules"]').getByText('Support', { exact: true }).click();
        await page.waitForFunction(() => location.pathname === '/TicketScreen');
        check('a module that overrides nothing inherits the application sidebar', await items('Sidebar'), ['Home']);
        check('no error was raised in the page', errors, []);
    } catch (error) {
        failures.push(`the scenario stopped: ${error.message}`);
    } finally {
        await browser.close();
    }

    fs.writeFileSync(resultPath, JSON.stringify({ passed, failures }, null, 2));
    console.log(`${passed.length} assertions passed, ${failures.length} failed`);
    failures.forEach(failure => console.log(`FAIL ${failure}`));
    process.exit(failures.length === 0 ? 0 : 1);
})();
