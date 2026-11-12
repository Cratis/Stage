// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { mountComponent, reactComponent } from '@cratis/scene.extensions';
import { aHost, aPlacedComponent, placement } from './given/a_placed_component';

describe('when a component cannot be shown', () => {
    it('says the module does not export it', async () => {
        const view = aPlacedComponent(placement(), { fetcher: aHost().fetcher, importer: async () => ({}) });
        const alert = await view.findByRole('alert');
        expect(alert.textContent).toContain('does not export a component named Chart');
        expect(view.getByRole('region', { name: 'ledger.Chart' }).getAttribute('aria-busy')).toBe('false');
    });

    it('says which kind is wrong when the model and the extension disagree', async () => {
        const importer = async () => ({ Chart: mountComponent(() => ({ update: () => undefined, unmount: () => undefined })) });
        const view = aPlacedComponent(placement(), { fetcher: aHost().fetcher, importer });
        expect((await view.findByRole('alert')).textContent).toContain('placed as a react component, but the extension exports it as a mount component');
    });

    it('says the module failed to load', async () => {
        const importer = async () => { throw new Error('Failed to fetch module'); };
        const view = aPlacedComponent(placement(), { fetcher: aHost().fetcher, importer });
        expect((await view.findByRole('alert')).textContent).toContain('Failed to fetch module');
    });

    it('shows the placeholder the package supplies in a frame that cannot run script', () => {
        const view = aPlacedComponent(placement({ placeholder: 'stage/extensions/packages/abc/placeholders/Chart' }), { fetcher: aHost().fetcher, importer: () => new Promise(() => undefined) });
        const frame = view.getByTitle('ledger.Chart');
        expect(frame.getAttribute('sandbox')).toBe('');
        expect(frame.getAttribute('src')).toBe('stage/extensions/packages/abc/placeholders/Chart');
    });

    it('does not run an exported value that is not a component definition', async () => {
        const view = aPlacedComponent(placement(), { fetcher: aHost().fetcher, importer: async () => ({ Chart: () => null, other: reactComponent(() => null) }) });
        expect((await view.findByRole('alert')).textContent).toContain('reactComponent or mountComponent');
    });
});
