// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Runs the browser scenarios used against a live Stage (Cratis/Screenplay
// Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/screen-composition-browser-native-controls.cjs)
// against a generated React application rendered from the canonical screen-composition corpus and served by its own
// generated backend. The assertions are the live harness's; what differs is only what a generated application is:
// - its routes are its generated Arc proxies' routes (no repeated command segment), not the live Stage's;
// - it serves no /stage/* endpoints - the runtime reads the Scene, routes and strings it was built with, so this
//   asserts that no /stage/* request is made at all;
// - the Scene document is read from the application's own src/stage-scene.json.
//
// Exit codes: 0 every assertion passed, 1 at least one assertion found a defect, 2 the scenarios could not run.

const { readFileSync, writeFileSync } = require('node:fs');
const { join } = require('node:path');

const baseUrl = process.env.STAGE_GENERATED_REACT_URL ?? 'http://127.0.0.1:19371';
const applicationDirectory = process.env.STAGE_GENERATED_REACT_APPLICATION;
const resultPath = process.env.STAGE_GENERATED_REACT_RESULT ?? join(process.cwd(), 'generated-react-browser-result.json');

const routes = {
    createWorkItem: '/api/workspaces/tracking/create-work-item',
    addComment: '/api/workspaces/tracking/add-comment'
};

const workItemA = {
    id: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    title: 'Design master detail',
    commentId: '11111111-1111-1111-1111-111111111111',
    comment: 'Needs compact layout'
};
const workItemB = {
    id: '22222222-2222-2222-2222-222222222222',
    title: 'Implement native browser forms',
    commentId: '22222222-2222-2222-2222-222222222223',
    comment: 'Validate selected work item only'
};
const createdWorkItem = {
    id: '33333333-3333-3333-3333-333333333333',
    title: 'Browser-created work item',
    notes: 'Notes typed across both columns'
};
const pushedWorkItem = {
    id: '55555555-5555-5555-5555-555555555555',
    title: 'Pushed from another client'
};
const nativeComment = {
    id: '44444444-4444-4444-4444-444444444444',
    text: 'Added through native browser form'
};

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function record(result, path, status, value = undefined) {
    result.assertions.push({ path, status, value });
}

function block(result, path, reason) {
    record(result, path, 'blocked', reason);
    result.remainingBlockers.push(`${path}: ${reason}`);
}

async function postJson(path, body) {
    const response = await fetch(`${baseUrl}${path}`, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!response.ok) throw new Error(`${path} returned ${response.status}`);
    const result = await response.json();
    if (result.isSuccess === false) throw new Error(`${path} was rejected: ${JSON.stringify(result)}`);
    return result;
}

async function expectText(page, result, path, text) {
    try {
        await page.getByText(text, { exact: false }).first().waitFor({ timeout: 10_000 });
        record(result, path, 'passed', text);
        return true;
    } catch {
        const screenshotPath = `${resultPath}.${path}.png`;
        const screenshot = await page.screenshot({ path: screenshotPath, fullPage: true, timeout: 5_000 })
            .then(() => screenshotPath, error => `not taken: ${error.message.split('\n')[0]}`);
        const body = await page.locator('body').innerText().catch(() => '');
        const pending = [...(page.__pendingRequests ?? new Map()).entries()].map(([url, started]) => `${url} (${Date.now() - started}ms)`);
        block(result, path, `missing text: ${text} (url ${page.url()}; screenshot ${screenshot}; pending ${pending.join(', ') || 'none'}; body ${body.slice(0, 400).replace(/\s+/g, ' ')})`);
        return false;
    }
}

async function expectNoText(page, result, path, text) {
    try {
        await page.getByText(text, { exact: false }).first().waitFor({ state: 'detached', timeout: 2_000 });
        record(result, path, 'passed', `absent: ${text}`);
        return true;
    } catch {
        block(result, path, `unexpected text remained: ${text}`);
        return false;
    }
}

async function describeControls(page) {
    return page.evaluate(() => Array.from(document.querySelectorAll('input, textarea, select, button')).map((element, index) => ({
        index,
        tag: element.tagName.toLowerCase(),
        type: element.getAttribute('type'),
        name: element.getAttribute('name'),
        ariaLabel: element.getAttribute('aria-label'),
        text: element.textContent?.trim(),
        disabled: element.disabled
    })));
}

async function closeDialog(page, result, path) {
    const dialogs = page.getByRole('dialog');
    if (await dialogs.count() === 0) return true;

    await page.keyboard.press('Escape');
    await page.waitForTimeout(500);
    if (await dialogs.count() === 0) {
        record(result, path, 'passed');
        return true;
    }

    const closeInsideDialog = dialogs.first().getByRole('button', { name: /cancel|close|dismiss/i }).first();
    if (await closeInsideDialog.count() > 0) {
        try {
            await closeInsideDialog.click({ timeout: 2_000 });
            await page.waitForTimeout(500);
        } catch (error) {
            record(result, `${path}.closeButton`, 'blocked', error.message);
        }
    }

    if (await dialogs.count() === 0) {
        record(result, path, 'passed');
        return true;
    }

    block(result, path, 'dialog remained open');
    return false;
}

// A command property is labelled by its name or by its words (commentId is shown as "Comment Id"). The pattern still
// names one field: "Work Item Id" never matches commentId.
function fieldLabelPattern(name) {
    const words = name.replace(/([a-z0-9])([A-Z])/g, '$1 $2').split(' ');
    return new RegExp(words.join('\\s*'), 'i');
}

async function fillField(page, result, labelOrName, value) {
    const byLabel = page.getByLabel(fieldLabelPattern(labelOrName)).first();
    if (await byLabel.count() > 0) {
        await byLabel.fill(value);
        record(result, `browser.native.field.${labelOrName}`, 'passed', 'label');
        return true;
    }

    const byName = page.locator(`input[name="${labelOrName}"], textarea[name="${labelOrName}"]`).first();
    if (await byName.count() > 0) {
        await byName.fill(value);
        record(result, `browser.native.field.${labelOrName}`, 'passed', 'name');
        return true;
    }

    record(result, `browser.native.controls.${labelOrName}`, 'info', JSON.stringify(await describeControls(page)));
    block(result, `browser.native.field.${labelOrName}`, 'field missing');
    return false;
}

async function clickSubmit(page, result, path) {
    const dialogs = page.getByRole('dialog');
    const searchRoot = await dialogs.count() > 0 ? dialogs.first() : page;
    let submit = searchRoot.getByRole('button', { name: /create|rename|add|submit|save/i }).filter({ hasNotText: /close|cancel/i }).last();

    if (await submit.count() === 0 && searchRoot !== page) {
        submit = searchRoot.getByRole('button').filter({ hasNotText: /close|cancel/i }).last();
    }

    if (await submit.count() === 0) {
        record(result, `${path}.controls`, 'info', JSON.stringify(await describeControls(page)));
        block(result, path, 'submit control missing');
        return false;
    }

    await submit.click();
    return true;
}

async function recordNativeButton(page, result, label, path) {
    const button = page.getByRole('button', { name: new RegExp(label, 'i') }).first();
    if (await button.count() === 0) {
        block(result, path, 'control missing');
        return { state: 'missing' };
    }

    const disabled = await button.isDisabled();
    const title = await button.getAttribute('title');
    if (disabled) {
        block(result, path, title ?? 'disabled');
        return { state: 'disabled', title };
    }

    record(result, path, 'passed', title ?? 'enabled');
    return { state: 'enabled', button };
}

function commandRequests(network, commandName) {
    return network.requests.filter(request => request.method === 'POST' && request.url.toLowerCase().includes(commandName.toLowerCase()));
}

function requestContainsWorkItem(request, workItem) {
    const body = typeof request.postData === 'string' ? request.postData : '';
    return request.url.includes(workItem.id) || body.includes(workItem.id) || body.includes(workItem.title);
}

// A row is found by the title the projection currently shows: after a rename, the original title no longer exists.
function titleCandidates(item) {
    return [...new Set([item.currentTitle, item.title].filter(Boolean))];
}

function hashRoute(url) {
    try {
        return new URL(url).hash.split('?')[0];
    } catch {
        return '';
    }
}

async function selectWorkItem(page, result, path, item) {
    for (const title of titleCandidates(item)) {
        const row = page.getByText(title, { exact: false }).first();
        if (await row.count() === 0) continue;

        await row.click();
        await page.waitForTimeout(1_000);
        record(result, path, 'passed', `${title} -> ${page.url()}`);
        return true;
    }

    block(result, path, `no row shows ${titleCandidates(item).join(' or ')}`);
    return false;
}

// Reach a screen the way a user would: an authored navigation entry first, then browser history. A direct URL is used only
// when neither exists, and that is recorded so the run shows the screen was not reachable through the UI. `label` is what the
// navigation shows for the screen: its title, or its humanized name without a trailing Screen, Page or Dialog.
async function navigateByUi(page, result, path, screen, label) {
    const target = `#/${screen}`;
    const entries = [
        ['navigation link', page.getByRole('link', { name: label, exact: true })],
        ['navigation menu item', page.getByRole('menuitem', { name: label, exact: true })],
        ['navigation button', page.getByRole('button', { name: label, exact: true })],
        ['navigation text', page.getByRole('navigation').getByText(label, { exact: true })]
    ];

    for (const [method, entry] of entries) {
        if (await entry.count() === 0 || !await entry.first().isVisible()) continue;

        await entry.first().click();
        await page.waitForTimeout(1_500);
        if (hashRoute(page.url()) === target) {
            record(result, path, 'passed', `${method}: ${page.url()}`);
            return;
        }
    }

    for (let step = 0; step < 8 && hashRoute(page.url()) !== target && page.url().startsWith(baseUrl); step++) {
        const before = page.url();
        await page.goBack({ waitUntil: 'domcontentloaded', timeout: 10_000 }).catch(() => null);
        await page.waitForTimeout(500);
        if (page.url() === before) break;
    }

    if (hashRoute(page.url()) === target) {
        record(result, path, 'passed', `history back: ${page.url()}`);
        return;
    }

    await page.goto(`${baseUrl}/${target}`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(2_000);
    record(result, path, 'info', `url fallback; no UI path to ${screen}`);
}

async function seedData(result) {
    for (const item of [workItemA, workItemB]) {
        await postJson(routes.createWorkItem, { workItemId: item.id, title: item.title, notes: 'Seeded' });
        record(result, `browser.seed.CreateWorkItem.${item.id}`, 'passed', item.title);
        await postJson(routes.addComment, { commentId: item.commentId, workItemId: item.id, text: item.comment });
        record(result, `browser.seed.AddComment.${item.commentId}`, 'passed', item.comment);
    }
}

function collectSceneIds(value, ids = []) {
    if (Array.isArray(value)) {
        for (const item of value) collectSceneIds(item, ids);
        return ids;
    }

    if (value && typeof value === 'object') {
        if (typeof value.id === 'string') ids.push(value.id);
        if (typeof value.stableId === 'string') ids.push(value.stableId);
        for (const child of Object.values(value)) collectSceneIds(child, ids);
    }

    return ids;
}

// Whether the rendered corpus's CreateWorkItemForm is the manual two-column form with a notes field spanning both columns.
let declaredTwoColumnCreateForm = false;

function declareCreateForm(scene) {
    const form = collectComponents(scene).find(component => String(component.componentName).endsWith('ommandForm') && component.properties?.command === 'CreateWorkItem');
    const layout = form?.properties?.layout;
    declaredTwoColumnCreateForm = form?.properties?.generationMode === 'manual' && Array.isArray(layout?.columns) && layout.columns.length === 2 &&
        (form.properties.fields ?? []).some(field => field.name === 'notes');
}

function collectComponents(value, components = []) {
    if (Array.isArray(value)) {
        for (const item of value) collectComponents(item, components);
        return components;
    }

    if (value && typeof value === 'object') {
        if (typeof value.componentName === 'string') components.push(value);
        for (const child of Object.values(value)) collectComponents(child, components);
    }

    return components;
}

// The authored Close action is guarded: `when item.status == "open" execute CloseWorkItem ... otherwise hidden`.
// The generated application carries it with its guard and fallback, and never as an unconditional command.
function isGuardedClose(component) {
    const properties = component.properties ?? {};
    const alternatives = Array.isArray(properties.alternatives) ? properties.alternatives : [];
    const fallback = JSON.stringify(properties.otherwise ?? '').toLowerCase();
    return component.componentName === 'core:action' &&
        alternatives.some(alternative => alternative.command === 'CloseWorkItem' &&
            JSON.stringify(alternative.condition ?? {}).includes('item.status') && JSON.stringify(alternative.condition ?? {}).includes('open')) &&
        fallback.includes('hidden');
}

function assertSceneDocument(result, scene) {
    const ids = [...new Set(collectSceneIds(scene))];
    const punctuationIds = ids.filter(id => id.includes('.') || id.includes(':'));
    if (punctuationIds.length > 0) record(result, 'browser.scene.identifierFidelity.punctuation', 'passed', punctuationIds.join(','));
    else block(result, 'browser.scene.identifierFidelity.punctuation', 'no punctuation-bearing stable ids found in src/stage-scene.json');

    const guardedClose = collectComponents(scene).filter(isGuardedClose);
    if (guardedClose.length > 0) record(result, 'browser.guarded.scene.closeCarriesGuard', 'passed', guardedClose.map(_ => _.id).join(','));
    else block(result, 'browser.guarded.scene.closeCarriesGuard', 'no Close action guarded by item.status == "open" with a hidden fallback');

    // A production application ships no sample data (spec SD6): not the packs, not the blueprint packs it names, not a profile's selection.
    const sampleData = [scene.sampleDataPacks, scene.blueprintSampleDataPacks].filter(value => Array.isArray(value) ? value.length > 0 : value !== undefined && value !== null);
    const selections = (scene.uiProfiles ?? []).filter(profile => profile.sampleData !== undefined && profile.sampleData !== null);
    if (sampleData.length === 0 && selections.length === 0) record(result, 'browser.scene.noSampleData', 'passed');
    else block(result, 'browser.scene.noSampleData', `the production scene carries sample data: ${JSON.stringify({ sampleData, selections })}`);

    const closeCommand = collectComponents(scene).filter(component => component.properties?.command === 'CloseWorkItem');
    if (closeCommand.length === 0) record(result, 'browser.guarded.scene.noCloseCommand', 'passed');
    else block(result, 'browser.guarded.scene.noCloseCommand', `CloseWorkItem is reachable without its guard from: ${closeCommand.map(_ => _.id).join(',')}`);
}

async function workItemRow(page, item) {
    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    // WorkItemDetails shows the list too, but without the double-click bindings; wait until it is the List screen.
    await page.getByText('Select a work item', { exact: false }).first().waitFor({ timeout: 10_000 });
    const row = page.getByText(item.title, { exact: false }).first();
    await row.waitFor({ timeout: 10_000 });
    return row;
}

// Exercises both guards in the browser: double-click on an open row navigates, Close is offered only while the item
// is open and posts CloseWorkItem for it, after which Close disappears and double-click runs the otherwise branch.
async function assertGuardedActions(page, result, network) {
    const notice = 'This work item is closed';

    await (await workItemRow(page, workItemA)).dblclick();
    await page.waitForTimeout(1_000);
    if (page.url().includes('WorkItemDetails') && page.url().includes(workItemA.id)) record(result, 'browser.guarded.doubleClick.open.navigates', 'passed', page.url());
    else block(result, 'browser.guarded.doubleClick.open.navigates', `double-click on open A stayed at ${page.url()}`);
    if (await page.getByText(notice, { exact: false }).count() === 0) record(result, 'browser.guarded.doubleClick.open.noNotice', 'passed');
    else block(result, 'browser.guarded.doubleClick.open.noNotice', 'the otherwise branch ran for an open item');

    await page.goto(`${baseUrl}/#/WorkItemDetails?workItemId=${workItemA.id}`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    const close = page.getByRole('button', { name: /^close$/i });
    try {
        await close.first().waitFor({ timeout: 10_000 });
        record(result, 'browser.guarded.close.offeredWhileOpen', 'passed');
    } catch {
        block(result, 'browser.guarded.close.offeredWhileOpen', 'no Close control for open A');
        return;
    }
    if (!(await close.first().isEnabled())) {
        block(result, 'browser.guarded.close.enabledWhileOpen', `Close is disabled for open A: ${await close.first().getAttribute('title')}`);
        return;
    }
    record(result, 'browser.guarded.close.enabledWhileOpen', 'passed');

    const before = commandRequests(network, 'close-work-item').length;
    await close.first().click();
    await page.waitForTimeout(2_000);
    const posts = commandRequests(network, 'close-work-item').slice(before);
    if (posts.length === 1 && requestContainsWorkItem(posts[0], { id: workItemA.id })) record(result, 'browser.guarded.close.postsForItem', 'passed', posts[0].postData);
    else block(result, 'browser.guarded.close.postsForItem', `expected one CloseWorkItem for A, got ${JSON.stringify(posts.map(_ => _.postData))}`);

    await page.goto(`${baseUrl}/#/WorkItemDetails?workItemId=${workItemA.id}`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    const details = await page.request.get(`${baseUrl}/api/workspaces/tracking/work-item-details/get-work-item?workItemId=${workItemA.id}`);
    const status = (await details.json().catch(() => ({})))?.data?.status;
    // The backend sends an enumeration as its ordinal, so closed is 1; a name is accepted too.
    if ([1, 'closed', 'Closed'].includes(status)) record(result, 'browser.guarded.close.projectsClosed', 'passed', String(status));
    else block(result, 'browser.guarded.close.projectsClosed', `A's status after Close is ${JSON.stringify(status)}`);
    if (await page.getByRole('button', { name: /^close$/i }).count() === 0) record(result, 'browser.guarded.close.hiddenOnceClosed', 'passed');
    else block(result, 'browser.guarded.close.hiddenOnceClosed', 'Close is still offered for closed A');

    await (await workItemRow(page, workItemA)).dblclick();
    await expectText(page, result, 'browser.guarded.doubleClick.closed.notifies', notice);
    if (!page.url().includes(workItemA.id)) record(result, 'browser.guarded.doubleClick.closed.staysOnList', 'passed', page.url());
    else block(result, 'browser.guarded.doubleClick.closed.staysOnList', `double-click on closed A navigated to ${page.url()}`);
}

// A command from a client other than this page must appear in the open list without a reload: the list is observable.
async function assertListUpdatesFromAnotherClient(page, result) {
    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await expectText(page, result, 'browser.live.list.loaded', workItemA.title);
    if (await page.getByText(pushedWorkItem.title, { exact: false }).count() > 0) {
        block(result, 'browser.live.list.beforeCommand', 'the pushed work item was listed before it was created');
        return;
    }

    await postJson(routes.createWorkItem, { workItemId: pushedWorkItem.id, title: pushedWorkItem.title, notes: 'Created by another client' });
    await expectText(page, result, 'browser.live.list.updatesWithoutRefresh', pushedWorkItem.title);
}

async function assertTwoItemSelectionLifecycle(page, result, network) {
    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(3_000);
    // The authored navigation entry names the screen "Work items" inside the Workspaces > Tracking hierarchy.
    await expectText(page, result, 'browser.navigation.WorkItemList', 'Work items');
    await expectText(page, result, 'browser.navigation.hierarchy.module', 'Workspaces');
    await expectText(page, result, 'browser.navigation.hierarchy.feature', 'Tracking');
    await expectText(page, result, 'browser.query.AllWorkItems.titleA', workItemA.title);
    await expectText(page, result, 'browser.query.AllWorkItems.titleB', workItemB.title);

    const beforeBRequests = network.requests.length;
    await page.getByText(workItemB.title, { exact: false }).first().click();
    await expectText(page, result, 'browser.masterDetail.selection', 'Clear selection');
    await expectText(page, result, 'browser.query.WorkItemDetails.titleB', workItemB.title);
    await expectText(page, result, 'browser.query.CommentsForWorkItem.commentB', workItemB.comment);
    await expectNoText(page, result, 'browser.query.CommentsForWorkItem.commentAAbsent', workItemA.comment);

    const bScopedRequests = network.requests.slice(beforeBRequests).filter(request => request.url.includes('/api/') && requestContainsWorkItem(request, workItemB));
    if (bScopedRequests.length > 0) record(result, 'browser.queryArgs.selectedB', 'passed', bScopedRequests.map(request => request.url).join(','));
    else block(result, 'browser.queryArgs.selectedB', 'no API request carried selected row B identity');

    const directLink = page.url();
    if (directLink.includes(workItemB.id)) record(result, 'browser.deepLink.selectedB', 'passed', directLink);
    else block(result, 'browser.deepLink.selectedB', `selected URL does not carry B identity: ${directLink}`);

    await assertStaleSelectionDiscard(page, result, network);

    await page.getByText('Clear selection', { exact: true }).click();
    await page.waitForTimeout(1_000);
    if (await page.getByText('Clear selection', { exact: true }).count() === 0) record(result, 'browser.queryRebind.clearSelection.button', 'passed');
    else block(result, 'browser.queryRebind.clearSelection.button', 'clear selection button remained');

    await expectNoText(page, result, 'browser.queryRebind.clearSelection.commentBAbsent', workItemB.comment);
    const clearUrl = page.url();
    if (!clearUrl.includes(workItemA.id) && !clearUrl.includes(workItemB.id)) record(result, 'browser.queryRebind.clearSelection.route', 'passed', clearUrl);
    else block(result, 'browser.queryRebind.clearSelection.route', `route still carries selected identity: ${clearUrl}`);
}

async function assertStaleSelectionDiscard(page, result, network) {
    let delayedA = 0;
    await page.route('**/api/**', async route => {
        const request = route.request();
        const postData = request.postData() ?? '';
        if (request.url().includes(workItemA.id) || postData.includes(workItemA.id)) {
            delayedA++;
            await sleep(1_500);
        }
        await route.continue();
    });

    await page.getByText(workItemA.title, { exact: false }).first().click();
    await sleep(50);
    await page.getByText(workItemB.title, { exact: false }).first().click();
    await page.waitForTimeout(2_500);
    await page.unroute('**/api/**');

    if (delayedA > 0) record(result, 'browser.queryRebind.staleDelayedA.request', 'passed', String(delayedA));
    else block(result, 'browser.queryRebind.staleDelayedA.request', 'no delayed A query was observed');

    const body = await page.locator('body').innerText();
    if (body.includes(workItemB.title) && body.includes(workItemB.comment) && !body.includes(workItemA.comment)) record(result, 'browser.queryRebind.staleDelayedA.discard', 'passed');
    else block(result, 'browser.queryRebind.staleDelayedA.discard', 'delayed A response was not proven discarded');

    const staleRequests = network.requests.filter(request => requestContainsWorkItem(request, workItemA) || requestContainsWorkItem(request, workItemB));
    record(result, 'browser.queryRebind.staleDelayedA.observedRequests', 'info', staleRequests.map(request => `${request.method} ${request.url}`).join('\n'));
}

// The corpus CreateWorkItemForm is a manual two-column form: column 1 is 1fr, column 2 is 2fr, a 24px column gap and 16px
// row gap; workItemId sits in row 1 column 1, title in row 1 column 2 and notes spans both columns in row 2. The same
// measurement runs against the generated React application here and against a live Stage in Cratis/Screenplay's browser harness,
// so both are held to the same geometry: track ratio, gap, placed fields and the spanning field.
async function assertTwoColumnFormGeometry(page, result) {
    const prefix = 'browser.native.CreateWorkItem.form.geometry';
    const measured = await page.evaluate(() => {
        const dialog = document.querySelector('[role="dialog"]') ?? document.body;
        const grids = [...dialog.querySelectorAll('.stage-command-form__layout')];
        const grid = grids[0];
        if (!grid) return { grids: 0, forms: dialog.querySelectorAll('form').length };
        const style = getComputedStyle(grid);
        const box = grid.getBoundingClientRect();
        const field = name => {
            const element = grid.querySelector(`[data-field="${name}"]`);
            if (!element) return null;
            const rect = element.getBoundingClientRect();
            return { left: rect.left - box.left, width: rect.width, top: rect.top - box.top, copies: dialog.querySelectorAll(`[data-field="${name}"]`).length, inputs: element.querySelectorAll('input, textarea, select').length };
        };
        return {
            grids: grids.length,
            forms: dialog.querySelectorAll('form').length,
            tracks: style.gridTemplateColumns.split(' ').map(Number.parseFloat),
            columnGap: Number.parseFloat(style.columnGap),
            rowGap: Number.parseFloat(style.rowGap),
            width: box.width,
            fields: { workItemId: field('workItemId'), title: field('title'), notes: field('notes') },
            namedInputs: ['workItemId', 'title', 'notes'].map(name => dialog.querySelectorAll(`input[name="${name}"], textarea[name="${name}"]`).length),
            submitButtons: dialog.querySelectorAll('button[type="submit"]').length
        };
    });
    record(result, `${prefix}.measured`, 'info', JSON.stringify(measured));

    if (measured.grids !== 1) {
        block(result, `${prefix}.layout`, `expected one two-column layout grid in the form, saw ${measured.grids}`);
        return;
    }

    const near = (actual, expected, tolerance = 1.5) => Math.abs(actual - expected) <= tolerance;
    const [first, second] = measured.tracks;
    const { workItemId, title, notes } = measured.fields;
    const checks = [
        ['two tracks', measured.tracks.length === 2],
        ['unequal tracks 1fr:2fr', second > first && near(second / first, 2, 0.05)],
        ['column gap 24px', near(measured.columnGap, 24, 0.5)],
        ['row gap 16px', near(measured.rowGap, 16, 0.5)],
        ['workItemId in column 1', !!workItemId && near(workItemId.left, 0) && near(workItemId.width, first)],
        ['title in column 2', !!title && near(title.left, first + measured.columnGap) && near(title.width, second)],
        ['title shares the row with workItemId', !!title && !!workItemId && near(title.top, workItemId.top)],
        ['notes spans both columns', !!notes && near(notes.left, 0) && near(notes.width, first + measured.columnGap + second)],
        ['notes is on a later row', !!notes && !!title && notes.top > title.top]
    ];
    for (const [name, ok] of checks) {
        if (ok) record(result, `${prefix}.${name.replace(/\W+/g, '-')}`, 'passed');
        else block(result, `${prefix}.${name.replace(/\W+/g, '-')}`, JSON.stringify(measured));
    }

    const oneInputEach = ['workItemId', 'title', 'notes'].every(name => measured.fields[name]?.copies === 1 && measured.fields[name]?.inputs === 1) && measured.namedInputs.every(count => count <= 1);
    if (oneInputEach) record(result, `${prefix}.noDuplicateInputs`, 'passed');
    else block(result, `${prefix}.noDuplicateInputs`, JSON.stringify(measured));

    if (measured.forms === 1) record(result, `${prefix}.oneSubmitBoundary`, 'passed');
    else block(result, `${prefix}.oneSubmitBoundary`, `expected one form element, saw ${measured.forms}`);
}

async function assertInvalidThenValidCreate(page, result, network) {
    const button = await recordNativeButton(page, result, 'Create work item', 'browser.native.CreateWorkItem.control');
    if (button.state !== 'enabled') return;

    await button.button.click();
    await page.waitForTimeout(500);
    const beforeInvalid = commandRequests(network, 'create-work-item').length;
    await clickSubmit(page, result, 'browser.native.CreateWorkItem.invalidSubmit.submitControl');
    await page.waitForTimeout(1_000);
    const afterInvalid = commandRequests(network, 'create-work-item').length;
    if (afterInvalid === beforeInvalid) record(result, 'browser.native.CreateWorkItem.invalidSubmit.zeroRequests', 'passed');
    else block(result, 'browser.native.CreateWorkItem.invalidSubmit.zeroRequests', `expected zero command requests, saw ${afterInvalid - beforeInvalid}`);
    await expectText(page, result, 'browser.native.CreateWorkItem.invalidSubmit.validation', 'required');

    // The scenario asserts what the rendered corpus declares: a corpus whose CreateWorkItemForm is the manual two-column form
    // (Cratis.Screenplay.CanonicalCorpus 4.211.2 and later) is held to its grid geometry and its extra field; an earlier
    // single-column corpus keeps the original assertions.
    const twoColumn = declaredTwoColumnCreateForm;
    if (twoColumn) await assertTwoColumnFormGeometry(page, result);

    const fieldsFilled = [
        await fillField(page, result, 'workItemId', createdWorkItem.id),
        await fillField(page, result, 'title', createdWorkItem.title),
        ...(twoColumn ? [await fillField(page, result, 'notes', createdWorkItem.notes)] : [])
    ].every(Boolean);
    if (!fieldsFilled) {
        await closeDialog(page, result, 'browser.native.CreateWorkItem.dialog.closeAfterMissingFields');
        return;
    }

    const beforeValid = commandRequests(network, 'create-work-item').length;
    await clickSubmit(page, result, 'browser.native.CreateWorkItem.validSubmit.submitControl');
    await page.waitForTimeout(3_000);
    const validRequests = commandRequests(network, 'create-work-item').slice(beforeValid);
    if (validRequests.length === 1 && requestContainsWorkItem(validRequests[0], createdWorkItem) && (!declaredTwoColumnCreateForm || (validRequests[0].postData ?? '').includes(createdWorkItem.notes))) record(result, 'browser.native.CreateWorkItem.validSubmit.payload', 'passed', validRequests[0].postData ?? validRequests[0].url);
    else block(result, 'browser.native.CreateWorkItem.validSubmit.payload', `expected one canonical create payload, saw ${validRequests.length}`);

    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(2_000);
    await expectText(page, result, 'browser.native.CreateWorkItem.validSubmit.projectionReopen', createdWorkItem.title);
}

async function assertRenameDialog(page, result, network) {
    await page.getByText(workItemB.title, { exact: false }).first().click();
    const button = await recordNativeButton(page, result, 'Rename', 'browser.native.RenameWorkItem.control');
    if (button.state !== 'enabled') return;

    const selectedUrl = page.url();
    await button.button.click();
    await page.waitForTimeout(500);
    if (page.url() !== selectedUrl || await page.getByRole('dialog').count() > 0) record(result, 'browser.dialog.open', 'passed', page.url());
    else block(result, 'browser.dialog.open', 'rename did not open a dialog or deep-link destination');

    const renamedTitle = 'Renamed selected B in browser';
    if (!await fillField(page, result, 'title', renamedTitle)) {
        await closeDialog(page, result, 'browser.dialog.closeAfterMissingRenameFields');
        return;
    }

    const beforeValid = commandRequests(network, 'rename-work-item').length;
    await clickSubmit(page, result, 'browser.native.RenameWorkItem.validSubmit.submitControl');
    await page.waitForTimeout(3_000);
    const validRequests = commandRequests(network, 'rename-work-item').slice(beforeValid);
    if (validRequests.length === 1 && requestContainsWorkItem(validRequests[0], { id: workItemB.id, title: renamedTitle })) record(result, 'browser.native.RenameWorkItem.validSubmit.payload', 'passed', validRequests[0].postData ?? validRequests[0].url);
    else block(result, 'browser.native.RenameWorkItem.validSubmit.payload', `expected one rename payload for selected B, saw ${validRequests.length}`);

    if (await expectText(page, result, 'browser.native.RenameWorkItem.validSubmit.projection', renamedTitle)) workItemB.currentTitle = renamedTitle;
    await closeDialog(page, result, 'browser.dialog.close');
}

async function assertAddComment(page, result, network) {
    if (!await selectWorkItem(page, result, 'browser.native.AddComment.selectB', workItemB)) return;

    const button = await recordNativeButton(page, result, 'Add comment', 'browser.native.AddComment.control');
    if (button.state !== 'enabled') return;

    await button.button.click();
    await page.waitForTimeout(500);
    const beforeInvalid = commandRequests(network, 'add-comment').length;
    await clickSubmit(page, result, 'browser.native.AddComment.invalidSubmit.submitControl');
    await page.waitForTimeout(1_000);
    const afterInvalid = commandRequests(network, 'add-comment').length;
    if (afterInvalid === beforeInvalid) record(result, 'browser.native.AddComment.invalidSubmit.zeroRequests', 'passed');
    else block(result, 'browser.native.AddComment.invalidSubmit.zeroRequests', `expected zero command requests, saw ${afterInvalid - beforeInvalid}`);
    await expectText(page, result, 'browser.native.AddComment.invalidSubmit.validation', 'required');

    const fieldsFilled = [
        await fillField(page, result, 'commentId', nativeComment.id),
        await fillField(page, result, 'text', nativeComment.text)
    ].every(Boolean);
    if (!fieldsFilled) {
        await closeDialog(page, result, 'browser.native.AddComment.dialog.closeAfterMissingFields');
        return;
    }

    const beforeValid = commandRequests(network, 'add-comment').length;
    await clickSubmit(page, result, 'browser.native.AddComment.validSubmit.submitControl');
    await page.waitForTimeout(3_000);
    const validRequests = commandRequests(network, 'add-comment').slice(beforeValid);
    if (validRequests.length === 1 && requestContainsWorkItem(validRequests[0], { id: workItemB.id, title: nativeComment.text })) record(result, 'browser.native.AddComment.validSubmit.payload', 'passed', validRequests[0].postData ?? validRequests[0].url);
    else block(result, 'browser.native.AddComment.validSubmit.payload', `expected one add-comment payload for selected B, saw ${validRequests.length}`);

    await expectText(page, result, 'browser.native.AddComment.validSubmit.projection', nativeComment.text);
    await page.reload({ waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(2_000);
    await expectText(page, result, 'browser.native.AddComment.validSubmit.reopen', nativeComment.text);
}

async function assertNavigationAndTheme(page, result) {
    await expectText(page, result, 'browser.navigation.themeLight.label', 'Scene Default Light');
    await expectText(page, result, 'browser.navigation.themeDark.label', 'Scene Default Dark');
    await expectText(page, result, 'browser.navigation.menu.CommentThread', 'Comment thread');

    const renderState = await page.evaluate(() => ({
        styleSheetCount: document.styleSheets.length,
        buttonCount: document.querySelectorAll('button').length,
        bodyBackground: getComputedStyle(document.body).backgroundColor
    }));
    if (renderState.styleSheetCount > 0 && renderState.buttonCount > 0) record(result, 'browser.package.renderHost', 'passed', JSON.stringify(renderState));
    else block(result, 'browser.package.renderHost', JSON.stringify(renderState));

    const dark = page.getByText('Scene Default Dark', { exact: false }).first();
    if (await dark.count() === 0) return;

    const before = renderState.bodyBackground;
    await dark.click();
    await page.waitForTimeout(500);
    const after = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
    if (after !== before) record(result, 'browser.theme.dark.appliedStyle', 'passed', `${before} -> ${after}`);
    else block(result, 'browser.theme.dark.appliedStyle', `body background did not change from ${before}`);
}

// The vector authors a module-level `contribute to Navigation` ("Overview"), a toolbar on the list with an icon-only dialog item and
// an item carrying a placement hint, and screens no entry lists. Each reaches the running application through the Scene host.
async function assertAuthoredToolbarAndHierarchy(page, result) {
    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(2_000);

    const groupsOf = href => page.evaluate(target => {
        const link = document.querySelector(`nav a[href="${target}"]`);
        const names = [];
        for (let node = link?.closest('li')?.parentElement?.closest('li'); node; node = node.parentElement?.closest('li')) {
            names.unshift(node.querySelector(':scope > span, :scope > a, :scope > button')?.textContent?.trim() ?? '');
        }
        return link ? names : undefined;
    }, href);

    const labelled = await page.evaluate(() => Array.from(document.querySelectorAll('nav a')).map(link => `${link.textContent?.trim()}|${link.getAttribute('href')}`));
    const overview = labelled.find(entry => entry.startsWith('Overview|'));
    if (overview) record(result, 'browser.navigation.moduleContribution.label', 'passed', overview);
    else block(result, 'browser.navigation.moduleContribution.label', `no module-level navigation entry "Overview" in [${labelled.join(', ')}]`);

    const overviewGroups = await page.evaluate(() => {
        const link = Array.from(document.querySelectorAll('nav a')).find(candidate => candidate.textContent?.trim() === 'Overview');
        const names = [];
        for (let node = link?.closest('li')?.parentElement?.closest('li'); node; node = node.parentElement?.closest('li')) {
            names.unshift(node.querySelector(':scope > span, :scope > a, :scope > button')?.textContent?.trim() ?? '');
        }
        return link ? names : undefined;
    });
    if (overviewGroups?.join('>') === 'Workspaces') record(result, 'browser.navigation.moduleContribution.hierarchy', 'passed', 'Workspaces');
    else block(result, 'browser.navigation.moduleContribution.hierarchy', `Overview is nested under [${overviewGroups?.join(' > ')}], expected [Workspaces]`);

    const detailsGroups = await groupsOf('#/WorkItemDetails');
    if (detailsGroups?.join('>') === 'Workspaces>Tracking') record(result, 'browser.navigation.unlistedScreen.hierarchy', 'passed', 'Workspaces > Tracking > WorkItemDetails');
    else block(result, 'browser.navigation.unlistedScreen.hierarchy', `WorkItemDetails is nested under [${detailsGroups?.join(' > ')}], expected [Workspaces > Tracking]`);

    const toolbar = page.getByRole('toolbar', { name: 'main' });
    if (await toolbar.count() > 0) record(result, 'browser.toolbar.rendered', 'passed');
    else {
        block(result, 'browser.toolbar.rendered', 'the authored toolbar "main" is not rendered');
        return;
    }

    const create = toolbar.locator('[data-scene-toolbar-item="create"]');
    const mode = await create.getAttribute('data-scene-mode').catch(() => null);
    const text = (await create.textContent().catch(() => null))?.trim();
    const name = await create.getAttribute('aria-label').catch(() => null);
    if (mode === 'icon' && text === '' && name === 'New work item') record(result, 'browser.toolbar.presentation.iconOnly', 'passed', `mode ${mode}, accessible name ${name}`);
    else block(result, 'browser.toolbar.presentation.iconOnly', `expected an icon-only item named "New work item", saw mode ${mode}, text "${text}", name ${name}`);

    const placement = await toolbar.locator('[data-scene-toolbar-item="refresh"]').getAttribute('data-scene-presentation-placement').catch(() => null);
    if (placement === 'secondary') record(result, 'browser.toolbar.presentation.hint', 'passed', placement);
    else block(result, 'browser.toolbar.presentation.hint', `expected the placement hint "secondary", saw ${placement}`);

    await create.click();
    const dialog = page.getByRole('dialog');
    try {
        await dialog.first().waitFor({ timeout: 10_000 });
        record(result, 'browser.toolbar.dialogDestination.opens', 'passed', (await dialog.first().getAttribute('aria-label')) ?? '');
    } catch {
        block(result, 'browser.toolbar.dialogDestination.opens', 'the toolbar dialog item did not open its dialog');
        return;
    }

    await closeDialog(page, result, 'browser.toolbar.dialogDestination.closes');
}

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

async function runBrowserFlow(result) {
    const { chromium } = require('playwright');
    const browser = await chromium.launch({ headless: true });
    try {
        const page = await browser.newPage();
        const network = { requests: [], responses: [] };
        const pageErrors = [];
        page.on('request', request => network.requests.push({ url: request.url(), method: request.method(), postData: request.postData() }));
        page.__pendingRequests = new Map();
        page.on('request', request => page.__pendingRequests.set(request.url(), Date.now()));
        page.on('requestfinished', request => page.__pendingRequests.delete(request.url()));
        page.on('requestfailed', request => page.__pendingRequests.delete(request.url()));
        page.on('response', response => network.responses.push({ url: response.url(), status: response.status() }));
        page.on('pageerror', error => pageErrors.push(error.message));

        await seedData(result);
        await assertTwoItemSelectionLifecycle(page, result, network);
        await navigateByUi(page, result, 'browser.navigation.ui.WorkItemList', 'WorkItemList', 'Work items');
        await assertInvalidThenValidCreate(page, result, network);
        await page.goto(`${baseUrl}/#/WorkItemDetails`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
        await page.waitForTimeout(2_000);
        await assertRenameDialog(page, result, network);
        await assertAddComment(page, result, network);
        await assertNavigationAndTheme(page, result);
        await assertAuthoredToolbarAndHierarchy(page, result);
        await assertGuardedActions(page, result, network);
        await assertListUpdatesFromAnotherClient(page, result);
        await assertResponsiveShell(browser, result, baseUrl);

        const stageRequests = network.requests.filter(request => new URL(request.url).pathname.startsWith('/stage/'));
        if (stageRequests.length === 0) record(result, 'browser.runtime.noStageEndpoints', 'passed');
        else block(result, 'browser.runtime.noStageEndpoints', stageRequests.map(request => request.url).join(','));

        if (pageErrors.length === 0) record(result, 'browser.runtime.noPageErrors', 'passed');
        else block(result, 'browser.runtime.noPageErrors', pageErrors.join(' | '));

        result.commandRequests = network.requests.filter(request => request.method === 'POST' && request.url.includes('/api/'));
        result.failedResponses = network.responses.filter(response => response.status >= 400);
    } finally {
        await browser.close();
    }
}

// Checks the helpers the browser flow relies on, each against a planted wrong answer, without starting a browser.
function selfTest() {
    const renamed = 'Renamed selected B in browser';
    const commentQueryForB = { method: 'GET', url: `${baseUrl}/api/workspaces/tracking/work-item-comments/comments-for-work-item?workItemId=${workItemB.id}` };
    const renamePost = { method: 'POST', url: `${baseUrl}/api/workspaces/tracking/rename-work-item`, postData: JSON.stringify({ workItemId: workItemB.id, title: renamed }) };
    const requests = [
        { method: 'GET', url: `${baseUrl}/api/workspaces/tracking/add-comment` },
        { method: 'POST', url: `${baseUrl}/api/workspaces/tracking/create-work-item`, postData: '{}' },
        { method: 'POST', url: `${baseUrl}/api/workspaces/tracking/add-comment`, postData: '{}' }
    ];
    const unguardedScene = { screens: [{ slotContent: { content: [{ id: 'a', componentName: 'core:action', properties: { command: 'CloseWorkItem', otherwise: 'hidden' } }] } }] };
    const unguardedResult = { assertions: [], remainingBlockers: [] };
    assertSceneDocument(unguardedResult, unguardedScene);
    const guardedScene = { screens: [{ slotContent: { content: [{ id: 'Details.close', componentName: 'core:action', properties: { label: 'Close', alternatives: [{ command: 'CloseWorkItem', condition: { kind: 'comparison', left: { path: 'item.status' }, operator: 'Equal', right: 'open' } }], otherwise: { outcome: 'Hidden' } } }] } }] };
    const guardedResult = { assertions: [], remainingBlockers: [] };
    assertSceneDocument(guardedResult, guardedScene);
    const checks = [
        ['a query carrying B identity matches B', requestContainsWorkItem(commentQueryForB, workItemB), true],
        ['a query carrying B identity does not match A', requestContainsWorkItem(commentQueryForB, workItemA), false],
        ['a rename payload matches the renamed B', requestContainsWorkItem(renamePost, { id: workItemB.id, title: renamed }), true],
        ['only POSTs count as command requests', commandRequests({ requests }, 'add-comment').length, 1],
        ['a create POST is not an add-comment request', commandRequests({ requests }, 'create-work-item').length, 1],
        ['scene ids keep dots, brackets and colons', collectSceneIds({ id: 'Workspaces.Tracking.contribution[0]', children: [{ stableId: 'CommentThread.1-section:table' }] }), ['Workspaces.Tracking.contribution[0]', 'CommentThread.1-section:table']],
        ['a renamed row is found by its new title first', titleCandidates({ title: workItemB.title, currentTitle: renamed }), [renamed, workItemB.title]],
        ['a route with parameters is still the details screen', hashRoute(`${baseUrl}/#/WorkItemDetails?workItemId=${workItemB.id}`), '#/WorkItemDetails'],
        ['commentId does not match the Work Item Id label', fieldLabelPattern('commentId').test('Work Item Id'), false],
        ['an unguarded Close in the scene is a blocker', unguardedResult.remainingBlockers.length, 3],
        ['a Close guarded by status with a hidden fallback passes', guardedResult.remainingBlockers, []]
    ];

    const failures = checks.filter(([, actual, expected]) => JSON.stringify(actual) !== JSON.stringify(expected));
    for (const [name, actual, expected] of failures) console.log(`FAIL ${name}: expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
    console.log(`self-test: ${checks.length - failures.length} of ${checks.length} checks passed`);
    process.exit(failures.length === 0 ? 0 : 1);
}

if (process.argv.includes('--self-test')) selfTest();
else (async () => {
    const result = { name: 'generated-react-browser', baseUrl, status: 'unknown', remainingBlockers: [], assertions: [] };
    let exitCode = 2;
    try {
        if (!applicationDirectory) throw new Error('STAGE_GENERATED_REACT_APPLICATION must name the rendered application directory.');
        const scene = JSON.parse(readFileSync(join(applicationDirectory, 'src', 'stage-scene.json'), 'utf8'));
        declareCreateForm(scene);
        record(result, 'browser.scene.createForm.twoColumn', 'info', String(declaredTwoColumnCreateForm));
        // A caller that renders the published corpus (the cli's generated-React check) requires the two-column form, so a
        // corpus that lost it fails instead of silently skipping the geometry assertions.
        if (process.env.STAGE_GENERATED_REACT_REQUIRE_TWO_COLUMN_FORM && !declaredTwoColumnCreateForm) block(result, 'browser.scene.createForm.twoColumn.required', 'the rendered corpus does not declare the manual two-column CreateWorkItemForm');
        assertSceneDocument(result, scene);
        await runBrowserFlow(result);
        const passed = result.assertions.filter(assertion => assertion.status === 'passed').length;
        result.status = result.remainingBlockers.length > 0 ? 'failed' : 'passed';
        result.summary = `${passed} assertions passed, ${result.remainingBlockers.length} blocked`;
        exitCode = result.remainingBlockers.length > 0 ? 1 : 0;
    } catch (error) {
        result.status = 'could-not-run';
        result.message = error.stack ?? error.message;
        exitCode = 2;
    } finally {
        writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    }

    console.log(result.summary ?? result.message);
    for (const blocker of result.remainingBlockers) console.log(`BLOCKED ${blocker}`);
    process.exit(exitCode);
})();
