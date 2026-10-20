// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, screen } from '@testing-library/react';
import { afterEach, chai, describe, it } from 'vitest';
import { StageDataProvider, useStageData } from '../stageData';

chai.should();

const text = (label: string) => screen.getByLabelText(label).textContent ?? '';

function Probe() {
    const data = useStageData();
    return (
        <>
            <output aria-label='key'>{String(data.resolveBinding({ path: 'screen.keys.supplierId' }) ?? '')}</output>
            <button onClick={() => data.selectRow('suppliers', { supplierId: 's1', name: 'Nordic' })}>select</button>
        </>
    );
}

function view(parameters: Record<string, string> = {}) {
    return render(
        <StageDataProvider routes={undefined} parameters={parameters} locale='en' locales={['en']} screen='Suppliers'>
            <Probe />
        </StageDataProvider>,
    );
}

describe('when a row is selected for a key the address does not carry', () => {
    afterEach(() => globalThis.localStorage?.clear());

    it('should have no key before a row is selected, so the details wait instead of asking a query that needs one', () => {
        view();

        text('key').should.equal('');
    });

    it('should read the key from the selected row', () => {
        view();

        act(() => screen.getByRole('button', { name: 'select' }).click());

        text('key').should.equal('s1');
    });

    it('should let the address win over the selected row', () => {
        view({ supplierId: 's2' });

        act(() => screen.getByRole('button', { name: 'select' }).click());

        text('key').should.equal('s2');
    });
});
