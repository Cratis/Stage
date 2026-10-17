// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, chai, describe, it, vi } from 'vitest';
import { ArcContext } from '@cratis/arc.react';
import { registerCommand } from '@cratis/scene.components';
import { BindingSourceKind, InteractionActionKind, InteractionTriggerKind } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core/config';
import { App, type StageSceneApplication } from '../../App';
import { StageSourceProvider, staticStageSource } from '../../stageSource';
import { element } from '../../testElements';
import { commandResult } from '../../testing/commands';
import { RegisterProject } from '../given/RegisterProject';

chai.should();

describe('when executing an interaction in a rendered application with host configuration', () => {
    let requests: { url: string; headers: Headers; body: string }[];

    beforeEach(() => {
        registerCommand('RegisterProject', RegisterProject);
        requests = [];
        vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
            requests.push({ url: String(input), headers: new Headers(init?.headers), body: String(init?.body) });
            return new Response(JSON.stringify(commandResult()), { status: 200 });
        }));
    });
    afterEach(() => { vi.unstubAllGlobals(); globalThis.history.replaceState(null, '', '#'); });

    it('should reach the configured API route and carry the microservice and identity headers', async () => {
        const button = element('register', 'core:button', { label: 'Register project' });
        const action = { kind: InteractionActionKind.ExecuteCommand, command: 'RegisterProject', arguments: [{ name: 'name', value: { kind: BindingSourceKind.Literal, path: '', value: 'Apollo' } }] };
        button.behaviors = [{ name: 'Register', bindings: [{
            trigger: { kind: InteractionTriggerKind.Click },
            actions: [action],
        }] }];
        const scene: StageSceneApplication = {
            layouts: [], screenTemplates: [], screens: [{
                name: 'Projects', layout: 'AppShell', forms: [], contributions: [], behaviors: [], slotContent: { content: [button] },
            }],
        };
        // Generated apps use static content and register proxies through bindings.ts; no live-host route synthesis occurs here.
        const source = staticStageSource({ scene, routes: { commands: {}, queries: {} }, strings: {} });
        render(<ArcContext.Provider value={{ microservice: 'projects', origin: 'https://example.test', apiBasePath: '/tenant-api', httpHeadersCallback: () => ({ 'X-Identity': 'alice' }) }}>
            <PrimeReactProvider><StageSourceProvider source={source}><App /></StageSourceProvider></PrimeReactProvider>
        </ArcContext.Provider>);
        fireEvent.click(await screen.findByRole('button', { name: 'Register project' }));

        await waitFor(() => requests.should.have.lengthOf(1));
        requests[0].url.should.equal('https://example.test/tenant-api/api/projects/register');
        requests[0].headers.get('X-Identity')!.should.equal('alice');
        requests[0].headers.get('x-cratis-microservice')!.should.equal('projects');
        JSON.parse(requests[0].body).should.deep.equal({ name: 'Apollo' });
    });
});
