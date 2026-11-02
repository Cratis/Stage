// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { chai, describe, it } from 'vitest';
import { render } from './testing/render';
import { RegisteredAction } from './testing/commands';
import { StageDataProvider } from './stageData';
import { element } from './testElements';

chai.should();

async function openDialog() {
    const schema = JSON.stringify({ type: 'object', required: ['name'], properties: { name: { type: 'string' } } });
    render(<StageDataProvider routes={{ commands: { Schedule: '/api/schedule' }, queries: {} }} locale='en' locales={['en']} screen='Schedule'>
        <RegisteredAction element={element('schedule', 'core:action', { command: 'Schedule', label: 'Schedule', schema, generationMode: 'auto' })} slots={{}} />
    </StageDataProvider>);
    fireEvent.click(screen.getByRole('button', { name: 'Schedule' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText('Name', { exact: false }), { target: { value: 'Keep my input' } });
    return dialog;
}

describe('when dismissing a command dialog with entered input', () => {
    it('should keep the dialog and input after clicking outside', async () => {
        await openDialog();

        fireEvent.pointerDown(document.body, { pointerType: 'mouse', button: 0 });
        fireEvent.mouseDown(document.body, { button: 0 });
        fireEvent.pointerUp(document.body, { pointerType: 'mouse', button: 0 });
        fireEvent.mouseUp(document.body, { button: 0 });
        fireEvent.click(document.body);

        const dialog = screen.getByRole('dialog');
        (within(dialog).getByLabelText('Name', { exact: false }) as HTMLInputElement).value.should.equal('Keep my input');
    });

    it('should close on Escape while keeping the close button visible', async () => {
        const dialog = await openDialog();
        within(dialog).getByRole('button', { name: 'Close' }).hidden.should.be.false;

        fireEvent.keyDown(within(dialog).getByLabelText('Name', { exact: false }), { key: 'Escape', code: 'Escape' });

        await waitFor(() => chai.should().not.exist(screen.queryByRole('dialog')));
    });

    it('should close using the close button', async () => {
        const dialog = await openDialog();

        fireEvent.click(within(dialog).getByRole('button', { name: 'Close' }));

        await waitFor(() => chai.should().not.exist(screen.queryByRole('dialog')));
    });
});
