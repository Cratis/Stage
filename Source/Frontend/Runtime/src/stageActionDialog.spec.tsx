// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { StageActionDialog, useCloseActionDialog } from './StageActionDialog';

function Done() {
    const close = useCloseActionDialog();
    return <button type='button' onClick={close}>Done</button>;
}

describe('when the form in an action dialog succeeds', () => {
    const open = () => {
        render(<PrimeReactProvider><StageActionDialog id='add' label='Add to basket'><Done /></StageActionDialog></PrimeReactProvider>);
        fireEvent.click(screen.getByRole('button', { name: 'Add to basket' }));
    };

    it('should show the form when the action is pressed', () => {
        open();

        expect(screen.getByRole('button', { name: 'Done' })).toBeDefined();
    });

    it('should close the dialog from its content', async () => {
        open();
        fireEvent.click(screen.getByRole('button', { name: 'Done' }));

        await waitFor(() => expect(screen.queryByRole('button', { name: 'Done' })).toBeNull());
    });
});
