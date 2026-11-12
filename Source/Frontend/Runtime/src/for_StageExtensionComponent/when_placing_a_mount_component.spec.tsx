// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { mountComponent, type ExtensionContext } from '@cratis/scene.extensions';
import { aHost, aPlacedComponent, placement } from './given/a_placed_component';

describe('when placing a mount extension component', () => {
    it('gives the component a host element, its properties and the context, and unmounts it with the screen', async () => {
        const unmount = vi.fn();
        const update = vi.fn();
        let received: { host: HTMLElement; properties: unknown; context: ExtensionContext } | undefined;
        const importer = async () => ({
            Chart: mountComponent((host, properties, context) => {
                received = { host, properties, context };
                host.textContent = `hello ${context.user.name}`;
                return { update, unmount };
            })
        });
        const view = aPlacedComponent(placement({ kind: 'mount' }), { fetcher: aHost().fetcher, importer });
        await waitFor(() => expect(received).toBeDefined());
        expect(received!.properties).toEqual({ series: 'sales' });
        expect(received!.context.settings).toEqual({ region: 'eu' });
        expect(received!.host.textContent).toBe('hello Ada');
        view.unmount();
        expect(unmount).toHaveBeenCalledOnce();
    });

    it('delivers a trigger the screen sends to the component through its context', async () => {
        const handler = vi.fn();
        const importer = async () => ({
            Chart: mountComponent((_host, _properties, context) => {
                context.on('refresh', handler);
                return { update: () => undefined, unmount: () => undefined };
            })
        });
        const view = aPlacedComponent(placement({ kind: 'mount' }), { fetcher: aHost().fetcher, importer });
        await waitFor(() => expect(view.queryByRole('region')).toBeNull());
        const host = view.container.querySelector('[data-stage-extension-host]')!;
        host.dispatchEvent(new CustomEvent('stage-extension-trigger', { detail: { name: 'refresh', values: { force: true } } }));
        expect(handler).toHaveBeenCalledWith({ force: true });
    });
});
