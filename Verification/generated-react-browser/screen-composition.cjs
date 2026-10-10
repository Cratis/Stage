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
    title: 'Browser-created work item'
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
// when neither exists, and that is recorded so the run shows the screen was not reachable through the UI.
async function navigateByUi(page, result, path, screen) {
    const target = `#/${screen}`;
    const entries = [
        ['navigation link', page.getByRole('link', { name: screen, exact: true })],
        ['navigation menu item', page.getByRole('menuitem', { name: screen, exact: true })],
        ['navigation button', page.getByRole('button', { name: screen, exact: true })],
        ['navigation text', page.getByRole('navigation').getByText(screen, { exact: true })]
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
        await postJson(routes.createWorkItem, { workItemId: item.id, title: item.title });
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

// The guarded Close action and the guarded double-click alternative are refused by Stage (STAGE-SCENE-ACTION-001,
// STAGE-SCENE-INTERACTION-001); the generated application must not carry or offer either.
function assertSceneDocument(result, scene) {
    const ids = [...new Set(collectSceneIds(scene))];
    const punctuationIds = ids.filter(id => id.includes('.') || id.includes(':'));
    if (punctuationIds.length > 0) record(result, 'browser.scene.identifierFidelity.punctuation', 'passed', punctuationIds.join(','));
    else block(result, 'browser.scene.identifierFidelity.punctuation', 'no punctuation-bearing stable ids found in src/stage-scene.json');

    const guarded = collectComponents(scene).filter(component => component.componentName === 'core:action' &&
        component.properties && ('alternatives' in component.properties || 'otherwise' in component.properties));
    if (guarded.length === 0) record(result, 'browser.guarded.scene.noGuardedActions', 'passed');
    else block(result, 'browser.guarded.scene.noGuardedActions', `guarded actions present: ${guarded.map(_ => _.id).join(',')}`);

    const closeCommand = collectComponents(scene).filter(component => component.properties?.command === 'CloseWorkItem');
    if (closeCommand.length === 0) record(result, 'browser.guarded.scene.noCloseCommand', 'passed');
    else block(result, 'browser.guarded.scene.noCloseCommand', `CloseWorkItem is reachable from: ${closeCommand.map(_ => _.id).join(',')}`);
}

async function assertGuardedActionsFailClosed(page, result) {
    await page.goto(`${baseUrl}/#/WorkItemDetails?workItemId=${workItemA.id}`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(2_000);
    const close = page.getByRole('button', { name: /^close$/i });
    if (await close.count() === 0) record(result, 'browser.guarded.close.absent', 'passed');
    else block(result, 'browser.guarded.close.absent', 'a Close control is offered on the details screen');

    const beforeDoubleClick = page.url();
    await page.getByText(workItemA.title, { exact: false }).first().dblclick();
    await page.waitForTimeout(1_000);
    const closedNotice = await page.getByText('This work item is closed', { exact: false }).count();
    if (closedNotice === 0) record(result, 'browser.guarded.doubleClick.noAlternative', 'passed', `${beforeDoubleClick} -> ${page.url()}`);
    else block(result, 'browser.guarded.doubleClick.noAlternative', 'the guarded double-click otherwise branch ran');
}

async function assertTwoItemSelectionLifecycle(page, result, network) {
    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(3_000);
    await expectText(page, result, 'browser.navigation.WorkItemList', 'WorkItemList');
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

async function assertInvalidThenValidCreate(page, result, network) {
    const button = await recordNativeButton(page, result, 'CreateWorkItem', 'browser.native.CreateWorkItem.control');
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

    const fieldsFilled = [
        await fillField(page, result, 'workItemId', createdWorkItem.id),
        await fillField(page, result, 'title', createdWorkItem.title)
    ].every(Boolean);
    if (!fieldsFilled) {
        await closeDialog(page, result, 'browser.native.CreateWorkItem.dialog.closeAfterMissingFields');
        return;
    }

    const beforeValid = commandRequests(network, 'create-work-item').length;
    await clickSubmit(page, result, 'browser.native.CreateWorkItem.validSubmit.submitControl');
    await page.waitForTimeout(3_000);
    const validRequests = commandRequests(network, 'create-work-item').slice(beforeValid);
    if (validRequests.length === 1 && requestContainsWorkItem(validRequests[0], createdWorkItem)) record(result, 'browser.native.CreateWorkItem.validSubmit.payload', 'passed', validRequests[0].postData ?? validRequests[0].url);
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

    const button = await recordNativeButton(page, result, 'AddComment', 'browser.native.AddComment.control');
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
    await expectText(page, result, 'browser.navigation.menu.CommentThread', 'CommentThread');

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
        await navigateByUi(page, result, 'browser.navigation.ui.WorkItemList', 'WorkItemList');
        await assertInvalidThenValidCreate(page, result, network);
        await page.goto(`${baseUrl}/#/WorkItemDetails`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
        await page.waitForTimeout(2_000);
        await assertRenameDialog(page, result, network);
        await assertAddComment(page, result, network);
        await assertNavigationAndTheme(page, result);
        await assertGuardedActionsFailClosed(page, result);

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
    const guardedScene = { screens: [{ slotContent: { content: [{ id: 'a', componentName: 'core:action', properties: { command: 'CloseWorkItem', otherwise: 'hidden' } }] } }] };
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
        ['a guarded action in the scene is a blocker', guardedResult.remainingBlockers.length, 3]
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
        assertSceneDocument(result, JSON.parse(readFileSync(join(applicationDirectory, 'src', 'stage-scene.json'), 'utf8')));
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
