// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import type { InteractionHandlers } from '@cratis/scene.react';
import { Button } from '@cratis/components/Common';
import { externalComponent } from '@cratis/scene.blueprint.default';
import { stageLibraryComponent } from './blueprint';
import { actionSubjectOf } from './stageActionSubject';
import type { actionVariant } from './humanize';
import { readableValue } from './readableValue';
import { RowMenuContext, stageRowMenuComponent, useRowMenuSubject, useScreenRowMenuActions } from './stageRowMenuContext';

/** Presentation is opt-in and case-sensitive; it never changes an action's subject. */
export function isRowMenuAction(element: SceneElement): element is ExternalComponent {
    if (!('componentName' in element) || element.componentName !== 'core:action') return false;
    const action = element as ExternalComponent;
    const presentation = action.properties.presentation;
    return !Array.isArray(action.properties.alternatives) && actionSubjectOf(action) === 'item' &&
        presentation !== null && typeof presentation === 'object' &&
        'placement' in presentation && presentation.placement === 'row-menu';
}

export interface RowMenuProps {
    row: Record<string, unknown>;
    label: string;
    actions: ExternalComponent[];
    renderAction: (action: ExternalComponent) => ReactNode;
}

/** Keeps opted-in actions out of their original inline slot without changing other presentations. */
export function StageActionPresentation({ element, children }: { element: ExternalComponent; children: ReactNode }) {
    const row = useRowMenuSubject();
    const groupedActions = useScreenRowMenuActions();
    return groupedActions.some(action => action.id === element.id) && !row ? null : children;
}

/** Names the row's control by the value in its first displayed column. */
export function rowMenuLabel(row: Record<string, unknown>, columns: readonly { property: string }[]): string {
    const property = columns[0]?.property ?? '';
    const value = row[property];
    const label = readableValue(value) ?? String(value ?? '');
    return `Actions for ${label || 'row'}`;
}

/** The extra table column exists only for opted-in item actions. */
export function rowMenuColumnCount(actions: ExternalComponent[]): number {
    return actions.length > 0 ? 1 : 0;
}

/** The optional action column's accessible header. */
export function StageRowMenuHeader({ actions }: { actions: ExternalComponent[] }) {
    return actions.length > 0 ? <th scope='col'>Actions</th> : null;
}

/** Leaves legacy table rows unchanged when no item action opted in. */
export function StageRowMenuCell(props: RowMenuProps) {
    return props.actions.length > 0 ? <td><StageRowMenu {...props} /></td> : null;
}

/** Uses the registered library's popup, or the native action buttons when no library supplies one. */
export function StageRowMenu(props: RowMenuProps) {
    if (props.actions.length === 0) return null;
    const Menu = stageLibraryComponent(stageRowMenuComponent);
    if (Menu) {
        const element = externalComponent('row-menu', stageRowMenuComponent, { ...props });
        return <Menu element={element} slots={{}} />;
    }
    return <span onClick={event => event.stopPropagation()} onDoubleClick={event => event.stopPropagation()} onKeyDown={event => event.stopPropagation()}>
        <RowMenuContext.Provider value={{ row: props.row, target: null }}>{props.actions.map(props.renderAction)}</RowMenuContext.Provider>
    </span>;
}

interface ActionTriggerProps {
    id: string;
    sceneId?: string;
    label: string;
    disabled?: boolean;
    title?: string;
    unavailable?: boolean;
    loading?: boolean;
    onClick?: () => void;
    interactions?: InteractionHandlers;
    variant?: ReturnType<typeof actionVariant>;
}

/** The same availability and command-dialog controller supplies both inline and popup triggers. */
export function StageActionTrigger({ id, sceneId, label, disabled, title, unavailable, loading, onClick, interactions, variant }: ActionTriggerProps) {
    const menu = useContext(RowMenuContext);
    if (!menu?.Item) return <Button type='button' data-scene-id={sceneId} data-stage-availability={unavailable ? 'unavailable' : undefined} disabled={disabled} title={title} loading={loading} variant={variant} onClick={onClick} {...interactions}>{label}</Button>;
    if (!menu.target) return null;
    const Item = menu.Item;
    return createPortal(<Item id={id} label={label} disabled={disabled || loading} title={title} unavailable={unavailable} onClick={onClick} />, menu.target);
}
