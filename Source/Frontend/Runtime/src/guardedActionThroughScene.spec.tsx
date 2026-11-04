// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, should, vi } from 'vitest';
import { createBrowserDispatcher, InteractionScope, SceneElementView } from '@cratis/scene.react';
import { BindingSourceKind, type BindingExpression, type SceneElement } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core';
import { stageApplicationRegistry } from './App';
import { StageDataProvider, useStageData } from './stageData';
import { bindDataSources } from './stageDataSources';
import { element } from './testElements';
import { commandResult, registerTestCommand } from './testing/commands';
import { queryResult, registerTestQueries } from './testing/queries';

vi.mock('@cratis/scene.react', async importOriginal => {
    const scene = await importOriginal<typeof import('@cratis/scene.react')>();
    return { ...scene, createBrowserDispatcher: vi.fn(scene.createBrowserDispatcher) };
});

const tree = bindDataSources(element('screen', 'core:section', {}, { content: [
    element('subject', 'core:data', { typeName: 'Invoice', query: 'GetInvoice', isCollection: false }),
    element('process', 'core:guarded-action', {
        label: 'Process',
        alternatives: [{ command: 'SendInvoice', condition: { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'draft' }, arguments: { invoiceId: { subject: 'item.id' } } }],
        otherwise: { outcome: 'Execute', command: 'ReopenInvoice', arguments: { invoiceId: { subject: 'item.id' } } },
    }),
] }));
const routes = { queries: { GetInvoice: '/api/invoice' }, commands: { SendInvoice: '/api/send', ReopenInvoice: '/api/reopen' } };

function ThroughScene({ scene = tree }: { scene?: SceneElement }) {
    const data = useStageData();
    return <SceneElementView element={scene} registry={stageApplicationRegistry()} resolveBinding={data.resolveBinding} />;
}

describe('a native guarded action over explicitly numbered enumerations', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });

    async function expectNoExecutableAction() {
        await new Promise(resolve => setTimeout(resolve, 50));
        const button = screen.queryByRole('button', { name: 'Process' });
        if (button) {
            expect((button as HTMLButtonElement).disabled).toBe(true);
            fireEvent.click(button);
        }
    }

    it.each([
        { members: ['open', 'closed'], numbers: [1, 0], right: 'open', status: 1, expected: true },
        { members: ['open', 'closed'], numbers: [1, 0], right: 'open', status: 0, expected: false },
        { members: ['draft', 'issued', 'cancelled'], numbers: [-1, 4, 90], right: 'draft', status: -1, expected: true },
        { members: ['draft', 'issued', 'cancelled'], numbers: [-1, 4, 90], right: 'issued', status: 4, expected: true },
        { members: ['draft', 'issued', 'cancelled'], numbers: [-1, 4, 90], right: 'issued', status: 1, expected: false },
        { members: ['draft', 'issued', 'cancelled'], numbers: [-1, 4, 90], right: 'issued', status: 7, expected: false },
        { members: ['draft', 'issued', 'cancelled'], numbers: [-1, 4, 90], right: 'issued', status: 7, operator: 'NotEqual', expected: false },
        { members: ['draft', 'issued', 'cancelled'], numbers: [-1, 4, 90], right: 'issued', status: 'ISSUED', expected: true },
        { members: ['open', 'closed'], numbers: [1, 1], right: 'open', status: 1, expected: false },
    ])('offers and executes only the named member: %j', async ({ members, numbers, right, status, operator = 'Equal', expected }) => {
        registerTestQueries(routes.queries);
        registerTestCommand('SendInvoice', routes.commands.SendInvoice, { invoiceId: { type: 'string' } });
        const fetched = vi.fn((_input: RequestInfo | URL, init?: RequestInit) => Promise.resolve({ ok: true,
            json: async () => init?.method === 'POST' ? commandResult() : queryResult({ data: { id: 'invoice-1', status } }),
        } as Response));
        vi.stubGlobal('fetch', fetched);
        const scene = bindDataSources(element('screen', 'core:section', {}, { content: [
            element('subject', 'core:data', { typeName: 'Invoice', query: 'GetInvoice', isCollection: false }),
            element('process', 'core:guarded-action', {
                label: 'Process',
                alternatives: [{ command: 'SendInvoice', condition: { kind: 'comparison', left: { subject: 'item.status' }, operator, right, enumeration: members, enumerationNumbers: numbers }, arguments: { invoiceId: { subject: 'item.id' } } }],
                otherwise: { outcome: 'Hidden' },
            }),
        ] }));
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'><ThroughScene scene={scene} /></StageDataProvider></PrimeReactProvider>);
        await waitFor(() => expect(fetched).toHaveBeenCalled());
        if (expected) {
            fireEvent.click(await screen.findByRole('button', { name: 'Process' }));
            await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
            expect(fetched.mock.calls.find(([, init]) => init?.method === 'POST')?.[1]?.body).toBe(JSON.stringify({ invoiceId: 'invoice-1' }));
        } else {
            await expectNoExecutableAction();
            expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
        }
    });
});

describe('a guarded action rendered through Scene and Stage', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });

    it('should preserve executor failure messages in the fallback dispatcher outcome', async () => {
        const createDispatcher = vi.mocked(createBrowserDispatcher);
        registerTestQueries(routes.queries);
        vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({ ok: true,
            json: async () => queryResult({ data: { id: 'invoice-1', status: 'draft' } }),
        } as Response)));
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'><ThroughScene /></StageDataProvider></PrimeReactProvider>);
        const executeCommand = createDispatcher.mock.calls.at(-1)![0]!.executeCommand!;

        const outcome = await executeCommand('UnregisteredGuardedCommand', { invoiceId: 'invoice-1' });

        expect(outcome).toEqual({
            isSuccess: false,
            response: undefined,
            validationErrors: [],
            exceptionMessages: ['The modeled command “UnregisteredGuardedCommand” is not registered by this Stage.'],
        });
    });

    it('should show the command failure messages in Scene’s alert without an interaction scope', async () => {
        registerTestQueries(routes.queries);
        registerTestCommand('SendInvoice', routes.commands.SendInvoice, { invoiceId: { type: 'string' } });
        vi.stubGlobal('fetch', vi.fn((_input: RequestInfo | URL, init?: RequestInit) => Promise.resolve({ ok: true,
            json: async () => init?.method === 'POST'
                ? commandResult({ isSuccess: false, hasExceptions: true, exceptionMessages: ['The invoice is locked.'] })
                : queryResult({ data: { id: 'invoice-1', status: 'draft' } }),
        } as Response)));
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'><ThroughScene /></StageDataProvider></PrimeReactProvider>);

        fireEvent.click(await screen.findByRole('button', { name: 'Process' }));

        const alert = await screen.findByRole('alert');
        should();
        alert.textContent!.should.contain('The invoice is locked.');
        alert.textContent!.should.not.contain('The command did not succeed.');
    });

    it.each([
        { status: 'draft', commandRoute: '/api/send', fields: {} },
        { status: 'sent', commandRoute: '/api/reopen', fields: {} },
        { status: 'draft', commandRoute: '/api/send', fields: { path: 'unbound' } },
        { status: 'sent', commandRoute: '/api/reopen', fields: { path: 'unbound' } },
    ])('should keep the queried $status subject with $fields visible and execute exactly the selected command', async ({ status, commandRoute, fields }) => {
        registerTestQueries(routes.queries);
        for (const [name, route] of Object.entries(routes.commands)) registerTestCommand(name, route, { invoiceId: { type: 'string' } });
        const fetched = vi.fn((_input: RequestInfo | URL, init?: RequestInit) => Promise.resolve({ ok: true,
            json: async () => init?.method === 'POST' ? commandResult() : queryResult({ data: { id: 'invoice-1', status, ...fields } }),
        } as Response));
        vi.stubGlobal('fetch', fetched);
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'><ThroughScene /></StageDataProvider></PrimeReactProvider>);

        fireEvent.click(await screen.findByRole('button', { name: 'Process' }));

        await waitFor(() => expect(fetched.mock.calls.filter(([, init]) => init?.method === 'POST')).toHaveLength(1));
        const [url, request] = fetched.mock.calls.find(([, init]) => init?.method === 'POST')!;
        expect(String(url)).toBe(`http://localhost:3000${commandRoute}`);
        expect(request?.body).toBe(JSON.stringify({ invoiceId: 'invoice-1' }));
    });

    it.each([
        { status: 'draft', commandRoute: '/api/send', fields: {} },
        { status: 'sent', commandRoute: '/api/reopen', fields: {} },
        { status: 'draft', commandRoute: '/api/send', fields: { path: 'unbound' } },
        { status: 'sent', commandRoute: '/api/reopen', fields: { path: 'unbound' } },
    ])('should keep the selected $status table row with $fields visible and execute exactly the selected command', async ({ status, commandRoute, fields }) => {
        registerTestQueries(routes.queries);
        for (const [name, route] of Object.entries(routes.commands)) registerTestCommand(name, route, { invoiceId: { type: 'string' } });
        const fetched = vi.fn((_input: RequestInfo | URL, init?: RequestInit) => Promise.resolve({ ok: true,
            json: async () => init?.method === 'POST' ? commandResult() : queryResult({ data: [{ id: 'invoice-1', status, ...fields }] }),
        } as Response));
        vi.stubGlobal('fetch', fetched);
        const collection = bindDataSources(element('screen', 'core:section', {}, { content: [
            element('subject', 'core:data', { typeName: 'Invoice', query: 'GetInvoice', isCollection: true }),
            element('process', 'core:guarded-action', {
                label: 'Process',
                alternatives: [{ command: 'SendInvoice', condition: { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'draft' }, arguments: { invoiceId: { subject: 'item.id' } } }],
                otherwise: { outcome: 'Execute', command: 'ReopenInvoice', arguments: { invoiceId: { subject: 'item.id' } } },
            }),
            element('invoices', 'core:table', { typeName: 'Invoice' }, { columns: [element('id', 'core:column', { property: 'id', label: 'Invoice' })] }),
        ] }));
        render(<PrimeReactProvider><StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'><ThroughScene scene={collection} /></StageDataProvider></PrimeReactProvider>);

        const row = await screen.findByText('invoice-1');
        should();
        (screen.queryByRole('button', { name: 'Process' }) === null).should.be.true;
        fireEvent.click(row);
        fireEvent.click(await screen.findByRole('button', { name: 'Process' }));

        await waitFor(() => fetched.mock.calls.filter(([, init]) => init?.method === 'POST').should.have.lengthOf(1));
        const [url, request] = fetched.mock.calls.find(([, init]) => init?.method === 'POST')!;
        String(url).should.equal(`http://localhost:3000${commandRoute}`);
        JSON.parse(request!.body as string).should.deep.equal({ invoiceId: 'invoice-1' });
    });

    it('should resolve an authored query-argument binding and use Stage’s queried subject instead of an authored item binding', async () => {
        const item = { id: 'invoice-bound', status: 'sent', path: 'unbound' };
        const itemBinding: BindingExpression = { kind: BindingSourceKind.DataContext, path: 'invoice' };
        const argumentBinding: BindingExpression = { kind: BindingSourceKind.DataContext, path: 'invoice.id' };
        registerTestQueries(routes.queries);
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => queryResult({ data: item }) });
        vi.stubGlobal('fetch', fetched);
        const executeCommand = vi.fn().mockResolvedValue({ isSuccess: true });
        const action = element('bound-process', 'core:guarded-action', {
            label: 'Process bound invoice', item: itemBinding,
            itemQuery: 'GetInvoice', itemArguments: { invoiceId: argumentBinding },
            alternatives: [{ command: 'SendInvoice', condition: { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'draft' }, arguments: { invoiceId: { subject: 'item.id' } } }],
            otherwise: { outcome: 'Execute', command: 'ReopenInvoice', arguments: { invoiceId: { subject: 'item.id' } } },
        });
        // Stage owns the subject: the authored item is draft, but the bound query returns sent.
        render(<StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'>
            <InteractionScope dispatcher={createBrowserDispatcher({ executeCommand })} attachments={[]}>
                <SceneElementView element={action} registry={stageApplicationRegistry()} dataContext={{ invoice: { ...item, status: 'draft' } }} />
            </InteractionScope>
        </StageDataProvider>);

        fireEvent.click(await screen.findByRole('button', { name: 'Process bound invoice' }));

        should();
        fetched.mock.calls.should.have.lengthOf(1);
        String(fetched.mock.calls[0][0]).should.equal('http://localhost:3000/api/invoice?invoiceId=invoice-bound');
        await waitFor(() => executeCommand.mock.calls.should.have.lengthOf(1));
        executeCommand.mock.calls[0].should.deep.equal(['ReopenInvoice', { item, invoiceId: 'invoice-bound' }]);
    });

    it('should refuse an authored item binding without a Stage query or selection', () => {
        const itemBinding: BindingExpression = { kind: BindingSourceKind.DataContext, path: 'invoice' };
        const executeCommand = vi.fn().mockResolvedValue({ isSuccess: true });
        const action = element('bound-process', 'core:guarded-action', {
            label: 'Process bound invoice', item: itemBinding,
            alternatives: [{ command: 'SendInvoice', condition: { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'draft' }, arguments: { invoiceId: { subject: 'item.id' } } }],
        });
        render(<StageDataProvider routes={routes} locale='en' locales={['en']} screen='Invoice'>
            <InteractionScope dispatcher={createBrowserDispatcher({ executeCommand })} attachments={[]}>
                <SceneElementView element={action} registry={stageApplicationRegistry()} dataContext={{ invoice: { id: 'invoice-bound', status: 'draft' } }} />
            </InteractionScope>
        </StageDataProvider>);

        should();
        screen.getByRole('alert').textContent!.should.contain('STAGE-SCENE-ACTION-001');
        (screen.queryByRole('button', { name: 'Process bound invoice' }) === null).should.be.true;
        executeCommand.mock.calls.should.have.lengthOf(0);
    });
});
