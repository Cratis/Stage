// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExternalComponent } from '@cratis/scene.model';
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from './testing/render';
import { StageActionTrigger, StageRowMenu } from './stageRowMenuActions';
import { useRowMenuSubject } from './stageRowMenuContext';
import { element } from './testElements';

function NativeAction({ action, execute }: { action: ExternalComponent; execute: (row: Record<string, unknown> | undefined) => void }) {
    const row = useRowMenuSubject();
    return <StageActionTrigger id={action.id} label={`Rename ${row?.name}`} onClick={() => execute(row)} />;
}

describe('row actions without an optional component library', () => {
    it('keeps native action buttons usable and bound to their own row', () => {
        const execute = vi.fn();
        const action = element('rename', 'core:action', {});
        const rows = [{ id: 'fjord', name: 'Fjord' }, { id: 'mountain', name: 'Mountain' }];
        render(<>{rows.map(row => <StageRowMenu key={row.id} row={row} label={`Actions for ${row.name}`} actions={[action]} renderAction={item => <NativeAction key={item.id} action={item} execute={execute} />} />)}</>);

        expect(screen.queryByRole('menu')).toBeNull();
        expect(screen.getByRole('button', { name: 'Rename Fjord' })).toBeDefined();
        fireEvent.click(screen.getByRole('button', { name: 'Rename Mountain' }));
        expect(execute).toHaveBeenCalledExactlyOnceWith(rows[1]);
    });
});
