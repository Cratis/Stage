// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { waitFor } from '@testing-library/react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { httpStageSource } from '../stageSource';

const render = vi.hoisted(() => vi.fn());
vi.mock('react-dom/client', () => ({ createRoot: () => ({ render }) }));
vi.mock('../App', () => ({ App: () => null }));
vi.mock('../sharedModules', () => ({ registerSharedModules: vi.fn() }));
vi.mock('../stagePackages', () => ({
    loadStagePackages: async () => ({ components: {}, catalog: [], screenTemplates: [], dialogTemplates: [] }),
    StagePackagesFailed: class extends Error {},
}));

chai.should();
describe('when the live host cannot load command routes', () => {
    afterEach(() => { vi.restoreAllMocks(); document.getElementById('root')?.remove(); });
    it('should warn and still render the scene instead of replacing it with an error', async () => {
        const root = document.createElement('div');
        root.id = 'root';
        document.body.appendChild(root);
        vi.spyOn(httpStageSource, 'routes').mockRejectedValue(new Error('Routes unavailable'));
        const warning = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
        await import('../main');
        await waitFor(() => render.mock.calls.should.have.lengthOf(1));
        warning.mock.calls.should.have.lengthOf(1);
        chai.expect(root.getAttribute('role')).to.equal(null);
        root.textContent!.should.equal('');
    });
});
