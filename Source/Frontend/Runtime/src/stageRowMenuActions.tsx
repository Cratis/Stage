// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useContext, useRef, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import type { InteractionHandlers } from '@cratis/scene.react';
import { Button } from '@cratis/components/Common';
import { Menu, type MenuItemSelectEvent, type MenuRootOpenChangeEvent } from '@primereact/ui/menu';
import { actionSubjectOf } from './stageActionSubject';
import type { actionVariant } from './humanize';
import { readableValue } from './readableValue';
import { RowMenuContext, useRowMenuSubject, useScreenRowMenuActions } from './stageRowMenuContext';

/** Presentation is opt-in and case-sensitive; it never changes an action's subject. */
export function isRowMenuAction(element: SceneElement): element is ExternalComponent {
    if (!('componentName' in element) || element.componentName !== 'core:action') return false;
    const action = element as ExternalComponent;
    const presentation = action.properties.presentation;
    return !Array.isArray(action.properties.alternatives) && actionSubjectOf(action) === 'item' &&
        presentation !== null && typeof presentation === 'object' &&
        'placement' in presentation && presentation.placement === 'row-menu';
}

interface RowMenuProps {
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
    return `Actions for ${readableValue(row[property]) || 'row'}`;
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

/** Controllers stay mounted outside the popup so closing it cannot unmount their command dialog. */
export function StageRowMenu({ row, label, actions, renderAction }: RowMenuProps) {
    const [target, setTarget] = useState<HTMLDivElement | null>(null);
    const [activated, setActivated] = useState(false);
    const host = useRef<HTMLSpanElement>(null);
    const restoreFocus = useCallback(() => { host.current?.querySelector<HTMLButtonElement>('button')?.focus(); }, []);
    if (actions.length === 0) return null;
    return <span ref={host} onClick={event => event.stopPropagation()} onDoubleClick={event => event.stopPropagation()} onKeyDown={event => event.stopPropagation()}>
        <Menu.Root onOpenChange={(event: MenuRootOpenChangeEvent) => { if (event.value) setActivated(true); }}>
            <Menu.Trigger aria-label={label} aria-haspopup='menu'>…</Menu.Trigger>
            <Menu.Portal><Menu.Positioner><Menu.Popup><Menu.List aria-label={label}><div ref={setTarget} /></Menu.List></Menu.Popup></Menu.Positioner></Menu.Portal>
            <RowMenuContext.Provider value={{ row, target, restoreFocus }}>{activated && actions.map(renderAction)}</RowMenuContext.Provider>
        </Menu.Root>
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
    if (!menu) return <Button type='button' data-scene-id={sceneId} data-stage-availability={unavailable ? 'unavailable' : undefined} disabled={disabled} title={title} loading={loading} variant={variant} onClick={onClick} {...interactions}>{label}</Button>;
    if (!menu.target) return null;
    return createPortal(<Menu.Item value={id} data-scene-id={id} data-stage-availability={unavailable ? 'unavailable' : undefined} disabled={disabled || loading} title={title} onSelect={(event: MenuItemSelectEvent) => {
        event.originalEvent.stopPropagation();
        onClick?.();
    }}>{label}</Menu.Item>, menu.target);
}
