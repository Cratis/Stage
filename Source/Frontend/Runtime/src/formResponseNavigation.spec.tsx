// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from './testing/render';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { BindingSourceKind, FormGenerationMode, InteractionActionKind } from '@cratis/scene.model';
import type { NavigateAction } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core/config';
import { App, type StageSceneApplication } from './App';
import { StageSourceProvider, staticStageSource } from './stageSource';
import { stageTheme } from './stageTheme';
import { element } from './testElements';
import { commandResult } from './testing/commands';
import { queryResult } from './testing/queries';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { registerStageQueries } from '../../src/host/queryRegistration';

const returnedId = '49f687cc-9e87-494e-8c70-8f779c919e17';
const schema = JSON.stringify({ type: 'object', required: ['title'], properties: { title: { type: 'string' } } });
const routes = { commands: { RegisterProject: '/api/register-project' }, queries: { ProjectById: '/api/project' }, commandSchemas: { RegisterProject: schema } };
const onSubmit: NavigateAction[] = [{ kind: InteractionActionKind.Navigate, screen: 'Details', arguments: [{ name: 'projectId', value: { kind: BindingSourceKind.CommandResponse, path: 'registeredId' } }] }];
const scene: StageSceneApplication = {
    layouts: [], screenTemplates: [],
    responseContracts: {
        commands: { RegisterProject: { kind: 'value', type: { kind: 'object', properties: { registeredId: { kind: 'string' } } } } },
        commandInputs: { RegisterProject: { title: { kind: 'string' } } },
        screenInputs: { Details: { projectId: { kind: 'string' } } },
    },
    screens: [
        {
            name: 'Overview', layout: 'AppShell', contributions: [], slotContent: {},
            forms: [{
                name: 'Register', forCommand: 'RegisterProject', generationMode: FormGenerationMode.Auto,
                fields: [{ name: 'title', label: 'Title' }],
                onSubmit,
            }],
        },
        {
            name: 'Details', layout: 'AppShell', forms: [], contributions: [],
            slotContent: { content: [element('details-data', 'core:data', { typeName: 'Project', query: 'ProjectById', by: 'projectId', isCollection: false })] },
        },
    ],
};

describe('an authored response continuation through the application shell', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.history.replaceState(null, '', '/'); globalThis.localStorage?.clear(); });

    it('carries the response contract and continuation to native submission and the keyed destination query', async () => {
        registerStageCommands(routes);
        registerStageQueries(routes);
        const fetch = vi.fn((input: RequestInfo | URL, _init?: RequestInit) => Promise.resolve({ ok: true, status: 200, json: async () => String(input).endsWith('/register-project')
            ? commandResult({ response: { registeredId: returnedId } })
            : queryResult({ data: { projectId: returnedId } }) } as Response));
        vi.stubGlobal('fetch', fetch);
        const source = staticStageSource({ scene, routes, strings: { en: {} } });
        render(<PrimeReactProvider theme={stageTheme}><StageSourceProvider source={source}><App /></StageSourceProvider></PrimeReactProvider>);

        fireEvent.change(await screen.findByLabelText('Title'), { target: { value: 'My project' } });
        fireEvent.click(screen.getByRole('button', { name: 'Submit' }));

        await waitFor(() => expect(globalThis.location.hash).toEqual(`#/Details?projectId=${returnedId}`));
        await waitFor(() => expect(fetch.mock.calls.some(([url]) => String(url).includes(`/api/project?projectId=${returnedId}`))).toBe(true));
        const submissions = fetch.mock.calls.filter(([url]) => String(url).endsWith('/register-project'));
        expect(submissions).toHaveLength(1);
        expect(JSON.parse(submissions[0][1]!.body as string)).toEqual({ title: 'My project' });
    });
});
