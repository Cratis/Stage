// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Runs only against the new generated, packed secondary-command candidate, served by run.sh.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { randomUUID } = require('node:crypto');
const { chromium } = require('playwright');

const application = process.env.STAGE_GENERATED_REACT_APPLICATION;
const baseURL = process.env.STAGE_GENERATED_REACT_URL;
const resultPath = process.env.STAGE_GENERATED_REACT_RESULT;
const result = { baseURL, application, publishedPinEquivalence: 'unknown', cases: [], errors: [] };
const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
const outcome = (isSuccess, extra = {}) => ({ isSuccess, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], ...extra });

function metadata() {
    const bindings = fs.readFileSync(path.join(application, 'src/bindings.ts'), 'utf8');
    const imports = new Map([...bindings.matchAll(/import \{ (\w+) as (__sceneCommand\d+) \} from '([^']+)';/g)].map(match => [match[2], { name: match[1], module: match[3] }]));
    const commands = [...bindings.matchAll(/"([^"]+)": (__sceneCommand\d+)/g)].map(match => {
        const imported = imports.get(match[2]);
        const folder = path.resolve(application, 'src', imported.module);
        const proxy = fs.readdirSync(folder).filter(file => file.endsWith('.ts')).map(file => path.join(folder, file))
            .find(file => fs.readFileSync(file, 'utf8').includes(`export class ${imported.name} `));
        assert.ok(proxy, `No generated class ${imported.name} in ${folder}`);
        const source = fs.readFileSync(proxy, 'utf8').split(`export class ${imported.name} `)[1].split('export class ')[0];
        const route = source.match(/(?:readonly\s+)?route(?:\s*:\s*string)?\s*=\s*['"]([^'"]+)['"]/)?.[1]
            ?? source.match(/get route\(\)[^{]*\{\s*return ['"]([^'"]+)['"]/)?.[1];
        assert.ok(route, `No native proxy route in ${proxy}`);
        return { key: match[1], name: imported.name, route: `/${route.replace(/^\//, '')}`, proxy, id: 'projectId' };
    });
    assert.equal(commands.length, 4);
    assert.equal(new Set(commands.map(command => command.route)).size, 4);
    const scene = JSON.parse(fs.readFileSync(path.join(application, 'src/stage-scene.json'), 'utf8'));
    assert.equal(JSON.stringify(scene).includes('"slice":'), false, 'Native command toolbars must submit without an unrelated slice button.');
    result.actionOnlyToolbar = true;
    const toolbar = [];
    function visit(value) {
        if (!value || typeof value !== 'object') return;
        if (value.componentName === 'core:toolbarItem' && /^(A|B) (Register|Archive)Project$/.test(value.properties?.label ?? '')) toolbar.push(value.properties);
        Object.values(value).forEach(child => Array.isArray(child) ? child.forEach(visit) : visit(child));
    }
    visit(scene);
    assert.equal(toolbar.length, 4);
    return { commands: toolbar.map(item => ({ ...commands.find(command => command.key === item.target), label: item.label })), firstScreen: scene.screens[0].name };
}

async function runCase(name, run) {
    await run();
    result.cases.push({ name, passed: true });
}

async function main() {
    assert.ok(application && baseURL && resultPath, 'Use the generated browser run.sh harness.');
    result.runtime = JSON.parse(fs.readFileSync(path.join(application, 'runtime-proof.json'), 'utf8'));
    const { commands, firstScreen } = metadata();
    result.commands = commands;
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext();
    await context.tracing.start({ screenshots: true, snapshots: true, sources: true });
    const page = await context.newPage();
    const requests = [];
    const stageRequests = [];
    page.on('pageerror', error => result.errors.push(String(error)));
    page.on('request', request => {
        const pathname = new URL(request.url()).pathname;
        if (pathname.startsWith('/stage/')) stageRequests.push(request.url());
        if (request.method() === 'POST' && commands.some(command => command.route === pathname)) requests.push({ route: pathname, body: request.postDataJSON() });
    });
    const count = command => requests.filter(request => request.route === command.route).length;
    const snapshot = () => commands.map(count);
    function delta(before, command, expected = 1) {
        commands.forEach((candidate, index) => assert.equal(count(candidate) - before[index], candidate.key === command.key ? expected : 0, `${candidate.key} POST delta`));
    }
    let selectionName;
    async function selectCurrentRow() {
        const row = page.getByRole('row').filter({ hasText: selectionName });
        await row.waitFor({ timeout: 45000 });
        if (await row.getAttribute('aria-selected') !== 'true') await row.click();
    }
    async function open(command) {
        const button = page.getByRole('button', { name: command.label, exact: true });
        if (await button.isDisabled()) await selectCurrentRow();
        await button.click();
        await page.getByRole('dialog').waitFor();
    }
    async function edit(name) {
        const dialog = page.getByRole('dialog');
        const member = dialog.getByLabel('Name', { exact: true });
        if (await member.count()) await member.fill(name);
        else await dialog.getByLabel('Archive reason', { exact: true }).fill(name);
    }
    const execute = () => page.getByRole('dialog').getByRole('button', { name: /^Execute .+$/ }).and(page.locator('button[type="submit"]'));
    // Read-only instrumentation of the real Stage provider; no callback replacement or production hook.
    async function refreshVersion() {
        return page.locator('button[data-scene-id]').first().evaluate(button => {
            let fiber = button[Object.keys(button).find(key => key.startsWith('__reactFiber$'))];
            while (fiber?.return) fiber = fiber.return;
            // A DOM node's return chain may be a bailed-out, stale tree. Read the root's committed tree only.
            const pending = [fiber?.stateNode?.current];
            const versions = [];
            while (pending.length) {
                const current = pending.pop();
                if (!current) continue;
                const value = current.memoizedProps?.value;
                if (typeof value?.refreshAfterCommand === 'function' && typeof value.refreshVersion === 'number') versions.push(value.refreshVersion);
                pending.push(current.child, current.sibling);
            }
            if (!versions.length) throw new Error('Stage data provider refresh instrumentation was not found');
            return Math.max(...versions);
        });
    }
    async function submit(command, name) {
        const before = snapshot();
        await open(command);
        await edit(name);
        await execute().click();
        await page.getByRole('dialog').waitFor({ state: 'hidden' });
        delta(before, command);
        const body = requests.at(-1).body;
        assert.equal(body[command.name === 'ArchiveProject' ? 'archiveReason' : 'name'], name);
        assert.ok(body[command.id]);
        assert.equal(Object.hasOwn(body, 'intruder'), false);
        if (command.label === 'B ArchiveProject') assert.equal(Object.hasOwn(body, 'name'), false, 'secondary schema must not become the primary schema');
        if (command.label.startsWith('A ')) {
            selectionName = name;
            await page.getByRole('row').filter({ hasText: name }).waitFor({ timeout: 45000 });
        }
    }
    try {
        const selectedId = randomUUID();
        const selectedName = `Selected-${selectedId}`;
        selectionName = selectedName;
        for (const command of commands.filter(command => command.name === 'RegisterProject')) {
            const response = await context.request.post(`${baseURL}${command.route}`, { data: { [command.id]: selectedId, name: selectedName } });
            assert.ok(response.ok(), `${command.key} seed: ${response.status()} ${await response.text()}`);
        }
        const primary = commands.find(command => command.label === 'A RegisterProject');
        const invalidName = `Invalid-selection-${selectedId}`;
        const invalidSeed = await context.request.post(`${baseURL}${primary.route}`, { data: { projectId: '00000000-0000-0000-0000-000000000000', name: invalidName } });
        assert.ok(invalidSeed.ok(), 'The real backend must expose the invalid-identifier selection fixture.');
        await page.goto(baseURL, { waitUntil: 'networkidle' });
        await page.getByRole('link', { name: firstScreen, exact: true }).click();
        const row = page.getByRole('row').filter({ hasText: selectedName });
        await row.first().waitFor({ timeout: 45000 });
        await runCase('compatible selection gates primary command', async () => {
            const primary = commands.find(command => command.label === 'A RegisterProject');
            await assert.doesNotReject(() => page.getByRole('button', { name: primary.label, exact: true }).waitFor());
            assert.equal(await page.getByRole('button', { name: primary.label, exact: true }).isDisabled(), true);
            await row.first().click();
            await page.getByRole('button', { name: primary.label, exact: true }).and(page.locator(':enabled')).waitFor();
            assert.equal(await page.getByRole('button', { name: primary.label, exact: true }).isEnabled(), true);
        });
        await runCase('invalid selected identifier stays disabled and makes no native POST', async () => {
            const invalid = page.getByRole('row').filter({ hasText: invalidName });
            await invalid.first().waitFor({ timeout: 45000 });
            await invalid.first().click();
            const before = snapshot();
            await page.getByRole('button', { name: primary.label, exact: true }).and(page.locator(':disabled')).waitFor();
            assert.equal(await page.getByRole('button', { name: primary.label, exact: true }).isDisabled(), true);
            await page.getByRole('button', { name: primary.label, exact: true }).dispatchEvent('click');
            await delay(100);
            assert.equal(await page.getByRole('dialog').count(), 0);
            commands.forEach((command, index) => assert.equal(count(command), before[index]));
            await row.first().click();
        });
        for (const command of commands) await runCase(`native exact-route ${command.label}`, () => submit(command, `Executed-${command.label}`));
        const secondary = commands.find(command => command.label === 'B ArchiveProject');
        await runCase('inline hidden Stage-provided value and nonmember filtering', async () => {
            await selectCurrentRow();
            const suppliedReason = (await page.getByRole('row', { selected: true }).innerText()).trim();
            const before = snapshot();
            await Promise.all([
                page.waitForResponse(response => response.request().method() === 'POST' && new URL(response.url()).pathname === secondary.route),
                page.getByRole('button', { name: 'Execute inline secondary', exact: true }).click(),
            ]);
            delta(before, secondary);
            const body = requests.at(-1).body;
            assert.equal(body.archiveReason, suppliedReason, 'Stage-supplied hidden value must survive unchanged');
            assert.equal(Object.hasOwn(body, 'name'), false);
            assert.equal(body.projectId, selectedId);
            assert.equal(Object.hasOwn(body, 'intruder'), false);
        });
        await runCase('three rapid clicks plus Enter produce one native POST', async () => {
            let release;
            const held = new Promise(resolve => { release = resolve; });
            await page.route(`${baseURL}${secondary.route}`, async route => { await held; await route.continue(); });
            const before = snapshot();
            await open(secondary);
            await edit('Rapid native');
            await execute().evaluate(button => {
                for (let index = 0; index < 3; index++) button.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
                button.form.dispatchEvent(new SubmitEvent('submit', { bubbles: true, cancelable: true, submitter: button }));
            });
            await delay(200);
            delta(before, secondary);
            release();
            await page.getByRole('dialog').waitFor({ state: 'hidden' });
            await page.unroute(`${baseURL}${secondary.route}`);
        });
        for (const oldFailure of [false, true]) await runCase(`cancel/reopen ignores stale ${oldFailure ? 'failure' : 'success'} and finally`, async () => {
            let releaseOld;
            let releaseNew;
            const oldHeld = new Promise(resolve => { releaseOld = resolve; });
            const newHeld = new Promise(resolve => { releaseNew = resolve; });
            let attempts = 0;
            await page.route(`${baseURL}${secondary.route}`, async route => {
                attempts++;
                if (attempts === 1) {
                    await oldHeld;
                    if (oldFailure) await route.abort('failed');
                    else await route.fulfill({ status: 200, json: outcome(true) });
                } else {
                    await newHeld;
                    await route.continue();
                }
            });
            await open(secondary);
            await edit('Retired native');
            await execute().click();
            await page.getByRole('dialog').getByRole('button', { name: 'Close', exact: true }).click();
            await open(secondary);
            await edit('New live native');
            const before = snapshot();
            await execute().click();
            await delay(100);
            delta(before, secondary);
            releaseOld();
            await delay(200);
            assert.equal(await page.getByRole('dialog').count(), 1);
            assert.equal(await page.getByRole('dialog').getByLabel('Archive reason', { exact: true }).inputValue(), 'New live native');
            await execute().evaluate(button => {
                button.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
                button.form.dispatchEvent(new SubmitEvent('submit', { bubbles: true, cancelable: true, submitter: button }));
            });
            await delay(100);
            delta(before, secondary);
            releaseNew();
            await page.getByRole('dialog').waitFor({ state: 'hidden' });
            await page.unroute(`${baseURL}${secondary.route}`);
        });
        await runCase('standalone open and cancel sends nothing', async () => {
            const before = snapshot();
            await open(secondary);
            await edit('Cancelled without submit');
            await page.getByRole('dialog').getByRole('button', { name: 'Close', exact: true }).click();
            await page.getByRole('dialog').waitFor({ state: 'hidden' });
            await delay(200);
            delta(before, secondary, 0);
            await open(secondary);
            delta(before, secondary, 0);
            await page.getByRole('dialog').getByRole('button', { name: 'Close', exact: true }).click();
            await page.getByRole('dialog').waitFor({ state: 'hidden' });
            delta(before, secondary, 0);
        });
        await runCase('local required validation retains the dialog and edits without POST', async () => {
            const before = snapshot();
            await open(secondary);
            await edit('Local edit');
            await edit('');
            await execute().click();
            await page.getByRole('dialog').getByText(/(?:Archive reason|archiveReason) is required/).first().waitFor();
            assert.equal(await page.getByRole('dialog').getByLabel('Archive reason', { exact: true }).inputValue(), '');
            assert.equal(await page.getByRole('dialog').count(), 1);
            delta(before, secondary, 0);
            await page.getByRole('dialog').getByRole('button', { name: 'Close', exact: true }).click();
            await page.getByRole('dialog').waitFor({ state: 'hidden' });
        });
        for (const failure of ['validation', 'authorization', 'server', 'exception', 'transport']) await runCase(`fault-injected ${failure} retains form and permits corrected retry`, async () => {
            await page.route(`${baseURL}${secondary.route}`, async route => {
                if (failure === 'transport') await route.abort('failed');
                else {
                    const failureState = failure === 'authorization' ? { isAuthorized: false }
                        : failure === 'validation' ? { isValid: false, validationResults: [{ members: ['archiveReason'], message: 'Correct the reason.', severity: 2 }] }
                            : failure === 'exception' ? { hasExceptions: true, exceptionMessages: ['Injected server exception.'] } : {};
                    await route.fulfill({ status: 200, json: outcome(false, failureState) });
                }
            }, { times: 1 });
            const before = snapshot();
            await open(secondary);
            await edit(`Rejected-${failure}`);
            await execute().click();
            await page.getByRole('dialog').getByRole('alert').first().waitFor();
            assert.equal(await page.getByRole('dialog').getByLabel('Archive reason', { exact: true }).inputValue(), `Rejected-${failure}`);
            delta(before, secondary);
            await edit(`Corrected-${failure}`);
            await execute().click();
            await page.getByRole('dialog').waitFor({ state: 'hidden' });
            delta(before, secondary, 2);
            await page.unroute(`${baseURL}${secondary.route}`);
        });
        await runCase('success refreshes one cycle and closes exactly once', async () => {
            await delay(3200); // Drain the previous command's deliberate 500/1500/3000ms settle reads.
            const command = commands.find(candidate => candidate.label === 'A ArchiveProject');
            await open(command);
            await edit('Exactly-once success');
            const before = snapshot();
            const version = await refreshVersion();
            await page.evaluate(() => {
                globalThis.__stage820CloseCount = 0;
                let open = true;
                globalThis.__stage820CloseObserver = new MutationObserver(() => {
                    const next = Boolean(document.querySelector('[role="dialog"]'));
                    if (open && !next) globalThis.__stage820CloseCount++;
                    open = next;
                });
                globalThis.__stage820CloseObserver.observe(document.body, { childList: true, subtree: true });
            });
            await execute().click();
            await page.getByRole('dialog').waitFor({ state: 'hidden' });
            selectionName = 'Exactly-once success';
            await page.getByRole('row').filter({ hasText: selectionName }).waitFor({ timeout: 45000 });
            await delay(3200);
            delta(before, command);
            // One refreshAfterCommand call: immediate refresh plus its three existing settle reads.
            const refreshed = await refreshVersion();
            const closed = await page.evaluate(() => globalThis.__stage820CloseCount);
            result.successCycle = { before: version, after: refreshed, closed };
            assert.equal(refreshed - version, 4);
            assert.equal(closed, 1);
            await page.evaluate(() => globalThis.__stage820CloseObserver.disconnect());
        });
        await runCase('native required-member validation (same as base)', async () => {
            await selectCurrentRow();
            const command = commands.find(candidate => candidate.label === 'A ArchiveProject');
            const before = snapshot();
            const button = page.getByRole('button', { name: 'Execute native omission', exact: true });
            const form = button.locator('xpath=ancestor::form');
            const edits = await form.locator('input').evaluateAll(inputs => inputs.map(input => input.value));
            await button.click();
            await form.getByText('archiveReason is required', { exact: true }).waitFor();
            await delay(200);
            delta(before, command, 0);
            assert.equal(await form.isVisible(), true);
            assert.deepEqual(await form.locator('input').evaluateAll(inputs => inputs.map(input => input.value)), edits);
            result.requiredMember = { baseCommit: 'f1b3bc0245778c723fe94559a91cf97a1b763689', verdict: 'NOT A REGRESSION', comparison: 'parent-verified native client validation: same required-member message and no POST on base' };
        });
        await runCase('session-supplied fallback identifier stays hidden (same as base)', async () => {
            await page.getByRole('link', { name: 'Fallback', exact: true }).click();
            // Retain storage but drop the previous screen's live selections before testing session-only fallback.
            await page.reload();
            await page.getByRole('button', { name: 'Fallback secondary', exact: true }).waitFor();
            const held = await page.evaluate(() => localStorage.getItem('stage.session.OtherProjectId'));
            assert.match(held, /^[0-9a-f-]{36}$/);
            await page.getByRole('button', { name: 'Fallback secondary', exact: true }).click();
            const dialog = page.getByRole('dialog');
            await dialog.waitFor();
            assert.equal(await dialog.getByLabel('Project id', { exact: true }).count(), 0);
            const before = snapshot();
            await edit('Inherited native identity');
            await execute().click();
            await dialog.waitFor({ state: 'hidden' });
            delta(before, secondary);
            assert.equal(requests.at(-1).body.projectId, held);
            result.fallbackFollowUp = 'Pre-existing on f1b3bc02: an unmarked inline form can create a concept session identity before query rows arrive; later fallback actions reuse and hide it. Clean-session fallback is checked separately.';
        });
        await runCase('clean-session fallback: missing identifier sends nothing; malformed identifier follows native rejection', async () => {
            // Isolate the no-identifier shape from the earlier inline form's existing session identity.
            await page.evaluate(() => localStorage.removeItem('stage.session.OtherProjectId'));
            await page.reload();
            await page.getByRole('button', { name: 'Fallback secondary', exact: true }).click();
            const dialog = page.getByRole('dialog');
            await dialog.waitFor();
            const identifier = dialog.getByLabel('Project id', { exact: true });
            await identifier.waitFor();
            assert.equal(await identifier.isVisible(), true);
            await edit('Invalid key');
            const before = snapshot();
            await identifier.fill('');
            await execute().click();
            await dialog.getByText(/(?:Project id|projectId) is required/).first().waitFor();
            delta(before, secondary, 0);
            assert.equal(await dialog.count(), 1);
            assert.equal(await dialog.getByLabel('Archive reason', { exact: true }).inputValue(), 'Invalid key');
            await identifier.fill('not-a-guid');
            const [invalidResponse] = await Promise.all([
                page.waitForResponse(response => response.request().method() === 'POST' && new URL(response.url()).pathname === secondary.route),
                execute().click(),
            ]);
            // Base's required-member validator has no UUID-format rule; native model binding rejects the malformed wire value.
            assert.equal(invalidResponse.status(), 400);
            delta(before, secondary);
            assert.equal(requests.at(-1).body.projectId, 'not-a-guid');
            assert.equal(await dialog.count(), 1);
            assert.equal(await identifier.inputValue(), 'not-a-guid');
            assert.equal(await dialog.getByLabel('Archive reason', { exact: true }).inputValue(), 'Invalid key');
            const retry = snapshot();
            await identifier.fill(randomUUID());
            await execute().click();
            await dialog.waitFor({ state: 'hidden' });
            delta(retry, secondary);
            result.fallbackMalformedKey = 'Pre-existing native behavior: missing identifier is blocked locally; nonempty malformed UUID reaches the native endpoint and receives HTTP 400. Base requiredMessage/missingMember has no UUID-format rule.';
        });
        assert.deepEqual(stageRequests, []);
        assert.deepEqual(result.errors, []);
        result.requests = requests;
        result.passed = true;
    } finally {
        await page.screenshot({ path: `${resultPath}.png`, fullPage: true });
        await context.tracing.stop({ path: `${resultPath}.trace.zip` });
        await browser.close();
    }
}

main().catch(error => { result.passed = false; result.error = error.stack ?? String(error); process.exitCode = 1; }).finally(() => {
    if (resultPath) fs.writeFileSync(resultPath, JSON.stringify(result, null, 2));
    console.log(JSON.stringify({ passed: result.passed, cases: result.cases.length, error: result.error }));
});
