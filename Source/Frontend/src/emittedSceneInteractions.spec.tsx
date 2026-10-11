// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from './stageTheme';
import { App } from './App';
import { doubleClickWindow } from './stageRowInteractions';
import scene from './fixtures/screen-composition-scene.json';
import sceneText from './fixtures/screen-composition-scene.json?raw';

// The screen composition corpus's scene exactly as `src/stage-scene.json` carries it into a generated application
// (the Conformance spec `when_carrying_the_authored_screens_vector` pins its bytes). What a generated application
// does with that file is the point: a document whose enumeration names do not match Scene's own parses and then
// matches nothing, so every authored interaction is silently inert.
const items = [
    { workItemId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', title: 'Alpha closed', status: 1 },
    { workItemId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', title: 'Bravo open', status: 0 },
];

function respond(payload: unknown): Promise<Response> {
    return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(payload) } as Response);
}

// The same hash `when_carrying_the_authored_screens_vector` pins on the .NET side: the fixture below is the planned
// artifact byte for byte, so these behaviours are those of what a generated application is actually given.
const authoredSceneSha256 = 'e524a689c963763badb0ce485df5a9660115c9cd57429509e204ec34ec05578b';

describe('the emitted screens scene in a generated application', () => {
    it('is the scene the conformance corpus pins', async () => {
        const digest = await globalThis.crypto.subtle.digest('SHA-256', new TextEncoder().encode(sceneText));
        expect([...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, '0')).join('')).toBe(authoredSceneSha256);
    });

    beforeEach(() => {
        globalThis.history.replaceState(null, '', '#/WorkItemList');
        vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'stage/scene') return respond(scene);
            if (url === 'stage/routes') return respond({ commands: { CloseWorkItem: '/api/close' }, queries: { AllWorkItems: '/api/all-work-items', GetWorkItem: '/api/get-work-item' } });
            if (url.includes('api/all-work-items')) return respond({ data: items });
            return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) } as Response);
        }));
    });

    afterEach(() => {
        globalThis.history.replaceState(null, '', '#');
        vi.unstubAllGlobals();
    });

    function doubleClick(row: HTMLElement) {
        fireEvent.click(row);
        fireEvent.click(row);
        fireEvent.doubleClick(row);
    }

    it('opens the details of an open work item on a double click', async () => {
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);
        doubleClick((await screen.findByText('Bravo open')).closest('tr')!);

        await waitFor(() => expect(globalThis.location.hash).toBe('#/WorkItemDetails?workItemId=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'));
    });

    it('says a closed work item is closed on a double click, and neither opens it nor selects it', async () => {
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);
        doubleClick((await screen.findByText('Alpha closed')).closest('tr')!);

        expect(await screen.findByText('Info: This work item is closed')).toBeDefined();
        await new Promise(resolve => setTimeout(resolve, doubleClickWindow + 100));
        expect(globalThis.location.hash).toBe('#/WorkItemList');
    });
});
