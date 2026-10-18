// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { Command } from '@cratis/arc/commands';
import { BindingSourceKind, InteractionActionKind, InteractionTriggerKind } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core/config';
import { App, type StageSceneApplication } from '../../App';
import { StageSourceProvider, staticStageSource } from '../../stageSource';
import { element } from '../../testElements';
import { commandResult } from '../../testing/commands';
import { registerStageCommands } from '../commandRegistration';

chai.should();
describe('when an interaction executes a command with a server-generated identifier', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); globalThis.history.replaceState(null, '', '#'); });
    it('should submit only caller inputs and preserve the generated identifier on success', async () => {
        const routes = { commands: { RegisterIdentifier: '/api/projects/registration/register/register-identifier' }, queries: {}, commandSchemas: {
            RegisterIdentifier: JSON.stringify({ type: 'object', properties: { name: { type: 'string' } }, required: ['name'] }),
        }, commandResponseKinds: { RegisterIdentifier: 'string' } };
        registerStageCommands(routes);
        const execution = vi.spyOn(Command.prototype, 'execute');
        const identifier = '11111111-1111-4111-8111-111111111111';
        const bodies: unknown[] = [];
        vi.stubGlobal('fetch', vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
            bodies.push(JSON.parse(String(init?.body)));
            return new Response(JSON.stringify(commandResult({ response: identifier })));
        }));
        const button = element('register-generated', 'core:button', { label: 'Register identifier' });
        const action = { kind: InteractionActionKind.ExecuteCommand, command: 'RegisterIdentifier',
            arguments: [{ name: 'name', value: { kind: BindingSourceKind.Literal, path: '', value: 'Apollo' } }],
        };
        button.behaviors = [{ name: 'Register', bindings: [{ trigger: { kind: InteractionTriggerKind.Click }, actions: [action] }] }];
        const scene: StageSceneApplication = { layouts: [], screenTemplates: [], screens: [{
            name: 'Projects', layout: 'AppShell', forms: [], contributions: [], behaviors: [], slotContent: { content: [button] },
        }] };
        const source = staticStageSource({ scene, routes, strings: {} });
        render(<PrimeReactProvider><StageSourceProvider source={source}><App /></StageSourceProvider></PrimeReactProvider>);
        fireEvent.click(await screen.findByRole('button', { name: 'Register identifier' }));
        await waitFor(() => bodies.should.have.lengthOf(1));
        bodies.should.deep.equal([{ name: 'Apollo' }]);
        const result = await execution.mock.results[0].value;
        result.isSuccess.should.equal(true);
        result.response.should.equal(identifier);
    });
});
