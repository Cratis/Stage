// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const { test } = require('node:test');
const { trackFailedRequests } = require('./failed-requests.cjs');

const moduleUrl = 'http://127.0.0.1:19392/stage/packages/files/@cratis/scene.blueprint.crm/dist/esm/component.js';

test('records the failed module URL and browser failure text even when the entry module returned 200', () => {
    const page = new EventEmitter();
    const failures = trackFailedRequests(page);

    page.emit('response', { status: () => 200, url: () => moduleUrl.replace('component.js', 'index.js') });
    page.emit('requestfailed', { url: () => moduleUrl, failure: () => ({ errorText: 'net::ERR_ABORTED' }) });

    assert.deepEqual(failures, [`REQUEST FAILED ${moduleUrl}: net::ERR_ABORTED`]);
});

test('records HTTP errors but ignores successful responses and redirects', () => {
    const page = new EventEmitter();
    const failures = trackFailedRequests(page);

    for (const status of [200, 304, 399, 400, 404, 500]) {
        page.emit('response', { status: () => status, url: () => moduleUrl });
    }

    assert.deepEqual(failures, [400, 404, 500].map(status => `HTTP ${status} ${moduleUrl}`));
});

test('retains failures across page navigations and tolerates missing browser failure details', () => {
    const page = new EventEmitter();
    const failures = trackFailedRequests(page);

    page.emit('requestfailed', { url: () => moduleUrl, failure: () => null });
    page.emit('framenavigated');
    page.emit('requestfailed', { url: () => moduleUrl, failure: () => ({ errorText: 'net::ERR_CONNECTION_RESET' }) });

    assert.deepEqual(failures, [
        `REQUEST FAILED ${moduleUrl}: unknown failure`,
        `REQUEST FAILED ${moduleUrl}: net::ERR_CONNECTION_RESET`,
    ]);
});
