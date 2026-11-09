// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { applicationChrome, stageActingUserComponent } from '../blueprint';
import { SandboxActingUser } from '../SandboxActingUser';
import { StageSourceProvider, staticStageSource, type StageSource } from '../stageSource';
import { changeSandboxActingUser, readSandboxActingUser, writeSandboxActingUser, type SandboxActingUserState } from '../sandboxActingUser';

const selection: SandboxActingUserState = { enabled: true, users: [{ name: 'alice', displayName: 'Alice Example', personas: ['Editor'] }], current: 'alice' };
const base = staticStageSource({ scene: {} as never, routes: {} as never, strings: {} });
const view = (source: StageSource) => render(<StageSourceProvider source={source}><SandboxActingUser /></StageSourceProvider>);

describe('when using the sandbox acting-user selection', () => {
    it('shows modeled display names and personas and the current name', async () => {
        const page = view({ ...base, actingUser: async () => selection, selectActingUser: async () => {} });
        await waitFor(() => expect(page.getByRole('combobox', { name: 'Acting as' })).toHaveProperty('value', 'alice'));
        expect(page.getByRole('option', { name: 'Alice Example (Editor)' })).toHaveProperty('value', 'alice');
        expect(page.getByRole('option', { name: 'No acting user' })).not.toBeNull();
    });

    it('does not offer a switcher for static and published sources', () => {
        expect(base.actingUser).toBeUndefined();
        expect(view(base).queryByRole('combobox')).toBeNull();
    });

    it('does not offer a switcher when the live host denies or has no modeled users', async () => {
        const page = view({ ...base, actingUser: async () => undefined, selectActingUser: async () => {} });
        await waitFor(() => expect(page.queryByRole('combobox')).toBeNull());
    });

    it('does not offer a switcher when reading the live host fails', async () => {
        const read = vi.fn().mockRejectedValue(new Error('unavailable'));
        const page = view({ ...base, actingUser: read, selectActingUser: async () => {} });
        await waitFor(() => expect(read).toHaveBeenCalledOnce());
        expect(page.queryByRole('combobox')).toBeNull();
    });

    it('keeps generated chrome unchanged unless the live capability is requested', () => {
        const chrome = (showActingUser?: boolean) => JSON.stringify(applicationChrome([], '', [], '', undefined, { showActingUser }));
        expect(chrome()).not.toContain(stageActingUserComponent);
        expect(chrome(true)).toContain(stageActingUserComponent);
    });

    it('hides empty and disabled host state', async () => {
        for (const state of [{ ...selection, users: [] }, { ...selection, enabled: false }]) {
            const fetcher = vi.fn<typeof fetch>().mockResolvedValue(Response.json(state));
            expect(await readSandboxActingUser(new AbortController().signal, fetcher)).toBeUndefined();
        }
    });

    it('keeps the current identity and shows a failure when the switch fails', async () => {
        const page = view({ ...base, actingUser: async () => selection, selectActingUser: async () => { throw new Error('denied'); } });
        const select = await page.findByRole('combobox');
        fireEvent.change(select, { target: { value: '' } });
        await page.findByRole('alert');
        expect(select).toHaveProperty('value', 'alice');
        expect(select).toHaveProperty('disabled', false);
    });

    it('reloads only after a successful host write, including clear', async () => {
        const calls: (string | null)[] = [];
        const source = { ...base, selectActingUser: async (name: string | null) => { calls.push(name); } };
        await changeSandboxActingUser(source, 'alice', () => calls.push('reload'));
        await changeSandboxActingUser(source, null, () => calls.push('reload'));
        expect(calls).toEqual(['alice', 'reload', null, 'reload']);
    });

    it('never reloads on a rejected write', async () => {
        const reload = vi.fn();
        await expect(changeSandboxActingUser({ ...base, selectActingUser: async () => { throw new Error('denied'); } }, 'alice', reload)).rejects.toThrow('denied');
        expect(reload).not.toHaveBeenCalled();
    });

    it('uses a relative endpoint, same-origin credentials and a name-only JSON mutation', async () => {
        const fetcher = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 204 }));
        await writeSandboxActingUser('alice', fetcher);
        expect(fetcher).toHaveBeenCalledWith('stage/sandbox/acting-user', {
            method: 'PUT', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: '{"name":"alice"}',
        });
        await writeSandboxActingUser(null, fetcher);
        expect(fetcher.mock.calls[1][1]?.body).toBe('{"name":null}');
    });

    it('fails a rejected mutation and hides a host-unavailable read', async () => {
        const fetcher = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 404 }));
        await expect(writeSandboxActingUser('alice', fetcher)).rejects.toThrow('404');
        expect(await readSandboxActingUser(new AbortController().signal, fetcher)).toBeUndefined();
    });
});
