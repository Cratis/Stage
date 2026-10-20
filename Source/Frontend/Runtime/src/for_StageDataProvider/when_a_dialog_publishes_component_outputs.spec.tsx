// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen, waitFor } from '@testing-library/react';
import { useEffect } from 'react';
import { chai, describe, it } from 'vitest';
import { StageDataProvider, useStageData } from '../stageData';
import { useComponentOutputsPublisher } from '../stageComponentOutputs';

/** A rendered tree as Scene reports it: every output of its components, handed over whenever one changes. */
function Tree({ outputs }: { outputs: Record<string, Record<string, unknown>> }) {
    const publish = useComponentOutputsPublisher();
    useEffect(() => publish(outputs), [outputs, publish]);
    return null;
}

function Resolved({ source, label }: { source: string; label: string }) {
    const value = useStageData().resolveArgument(source);
    return <output aria-label={label}>{value === undefined ? '' : String(value)}</output>;
}

chai.should();

const text = (label: string) => screen.getByLabelText(label).textContent ?? '';
const screenOutputs = { orders: { selectedItem: 'order-7' } };
const dialogOutputs = { search: { value: 'acme' } };

function view(dialogOpen: boolean) {
    return (
        <StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Orders'>
            <Tree outputs={screenOutputs} />
            {dialogOpen && <Tree outputs={dialogOutputs} />}
            <Resolved source='component.orders.selectedItem' label='screen' />
            <Resolved source='component.search.value' label='dialog' />
        </StageDataProvider>
    );
}

describe('when a dialog publishes component outputs', () => {
    it('should keep what the screen published while the dialog is open', async () => {
        render(view(true));

        await waitFor(() => text('dialog').should.equal('acme'));
        text('screen').should.equal('order-7');
    });

    it('should keep what the screen published once the dialog closes, and forget what the dialog published', async () => {
        const rendered = render(view(true));
        await waitFor(() => text('dialog').should.equal('acme'));

        rendered.rerender(view(false));

        await waitFor(() => text('dialog').should.equal(''));
        text('screen').should.equal('order-7');
    });
});
