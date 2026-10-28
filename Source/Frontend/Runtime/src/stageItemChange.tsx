// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useMemo, useRef, useState, type ComponentType } from 'react';
import type { BindingExpression, ExternalComponent } from '@cratis/scene.model';
import { InteractionTriggerKind } from '@cratis/scene.model';
import { createBindingResolver, resolveBehaviors, runBindings } from '@cratis/scene.engine';
import { attachmentFor, useInteractionScope, type ComponentBindingOutputs, type RegisteredComponentProps } from '@cratis/scene.react';
import { defaultBlueprintName, ComponentName, KanbanBoard } from '@cratis/scene.blueprint.default';
import { componentRegistryKey } from '@cratis/scene.react';
import { useStageData } from './stageData';

/** The binding path `with <input> from value` reads: the value the item changed to. */
export const changedValuePath = 'value';

/** The binding path `with <input> from previous` reads: the value the item had before it changed. */
export const previousValuePath = 'previous';

/** What a component reports for one of its items. */
export interface ReportedItem {
    id: unknown;
    column: unknown;
}

/** The item the user changed, as `item`, `value` and `previous` mean it to a binding. */
export interface ItemChange {
    /** The item's row, with the changed property already holding its new value. */
    item: Record<string, unknown>;

    /** The value the item changed to. */
    value: unknown;

    /** The value the item had before. */
    previous: unknown;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

/**
 * Finds the one item whose value differs between two reports of a component's items.
 *
 * @param before What the component reported before.
 * @param after What it reports now.
 * @returns The changed item and the value it had, or `undefined` when nothing changed or the report is not a list of items.
 */
export function changedItem(before: unknown, after: unknown): { item: ReportedItem; previous: unknown } | undefined {
    if (!Array.isArray(before) || !Array.isArray(after)) return undefined;
    const earlier = new Map(before.filter(isRecord).map(entry => [JSON.stringify(entry.id), entry.column]));
    for (const entry of after.filter(isRecord)) {
        const key = JSON.stringify(entry.id);
        if (earlier.has(key) && earlier.get(key) !== entry.column) return { item: { id: entry.id, column: entry.column }, previous: earlier.get(key) };
    }

    return undefined;
}

/**
 * Builds the change a binding reads from the rows a component presents.
 *
 * The component's own report names an item by its identifier and the value it changed to. The row the component was
 * given for that item is what the document binds to, so the row is found by `idField` and the property that holds
 * the value (`valueField`) takes the new value.
 *
 * @param rows The rows the component was given.
 * @param changed The item the component reported as changed.
 * @param idField The row property that identifies an item.
 * @param valueField The row property the user changes.
 * @returns The change, or `undefined` when the component presents no row for the item.
 */
export function itemChangeOf(rows: unknown, changed: { item: ReportedItem; previous: unknown }, idField: string, valueField: string): ItemChange | undefined {
    if (!Array.isArray(rows)) return undefined;
    const row = rows.filter(isRecord).find(candidate => JSON.stringify(candidate[idField]) === JSON.stringify(changed.item.id));
    if (row === undefined) return undefined;
    return { item: { ...row, [valueField]: changed.item.column }, value: changed.item.column, previous: changed.previous };
}

/**
 * Resolves what a change's bindings read: `item.<field>` from the changed row, `value` and `previous` from the
 * change, and a guarded `when ... otherwise` against the changed row. Everything else is the screen's own.
 *
 * @param change The change.
 * @param fallback Resolves any other binding.
 * @returns The resolver the interaction engine runs the bindings with.
 */
export function resolveChange(change: ItemChange, fallback: (binding: BindingExpression) => unknown): (binding: BindingExpression) => unknown {
    const resolveItem = createBindingResolver({ dataContext: { item: change.item } });
    return binding => {
        if (binding.path === changedValuePath) return change.value;
        if (binding.path === previousValuePath) return change.previous;
        if (binding.path === 'item' || binding.path?.startsWith('item.')) return resolveItem(binding);
        return fallback(binding);
    };
}

/** What a component that lets the user change its items needs to run the document's `on change`. */
export interface ItemChangeBinding {
    /** The element the bindings are attached to. */
    element: ExternalComponent;

    /** The property of the element that holds the rows the component presents. */
    rowsProperty: string;

    /** The row property that identifies an item. */
    idField: string;

    /** The row property the user changes. */
    valueField: string;
}

/** What the wrapped component is given in place of its own handlers and outputs. */
export interface ItemChangeHandlers {
    /** Whether the document declares an `on change` for the component. */
    declared: boolean;

    /** Replaces the component's `onChange`: remembers that the next report is the user's. */
    onChange: () => void;

    /** Wraps the outputs the component publishes, running the bindings when the user's change is reported. */
    outputs: ComponentBindingOutputs | undefined;

    /** Changes whenever the component must be shown the data again, because the change was not applied. Use it as the component's `key`. */
    revision: number;
}

/**
 * Runs a component's `on change` for the item the user changed.
 *
 * A component that lets the user change an item (a card dropped in another column) says so through its `onChange`
 * handler and then reports its items through its `value` output. The handler alone carries no item, so the change is
 * the difference between the report before and after. A report that follows no `onChange` is the data changing
 * under the component, which is not the user and so runs nothing.
 *
 * The bindings are the element's own, resolved and run by the Scene interaction engine - the same engine that runs
 * every other interaction. Once they have run the data is read again. A change that no command applied (the
 * `otherwise` branch only notified, or the command was refused) is shown as the data says it is rather than as the
 * user left it: the `revision` changes, and the component, keyed by it, starts again from the data.
 *
 * @param binding What the component is and which of its row properties identify and change an item.
 * @param outputs The outputs the component publishes.
 * @returns The handlers to give the component.
 */
export function useItemChange(binding: ItemChangeBinding, outputs: ComponentBindingOutputs | undefined): ItemChangeHandlers {
    const scope = useInteractionScope();
    const data = useStageData();
    const { element, rowsProperty, idField, valueField } = binding;
    const bindings = useMemo(
        () => resolveBehaviors(attachmentFor(element.id, element.behaviors), InteractionTriggerKind.Change),
        [element.behaviors, element.id]);
    const reported = useRef<unknown>(undefined);
    const userChange = useRef(false);
    const [revision, setRevision] = useState(0);
    const latest = useRef({ scope, data, element, bindings });
    latest.current = { scope, data, element, bindings };

    const run = useCallback((before: unknown, after: unknown) => {
        const { scope: current, data: stage, element: owner, bindings: toRun } = latest.current;
        const changed = changedItem(before, after);
        if (!current || toRun.length === 0 || changed === undefined) return;
        const change = itemChangeOf(owner.properties[rowsProperty], changed, idField, valueField);
        if (change === undefined) return;
        const context = { ...current.context, subject: () => change.item, resolve: resolveChange(change, current.context.resolve) };
        let applied = false;
        let refused = false;
        const dispatcher = {
            ...current.dispatcher,
            executeCommand: async (command: string, args: Record<string, unknown>) => {
                const outcome = await current.dispatcher.executeCommand(command, args);
                if (outcome?.isSuccess) applied = true; else refused = true;
                return outcome;
            },
        };
        void runBindings(toRun, dispatcher, context).then(result => {
            if (result.findings.length > 0) current.onFindings?.(result.findings);
            stage.refreshAfterCommand();
            if (!applied || refused) setRevision(current => current + 1);
        });
    }, [rowsProperty, idField, valueField]);

    const onChange = useCallback(() => { userChange.current = true; }, []);
    const wrapped = useMemo<ComponentBindingOutputs | undefined>(() => outputs === undefined ? undefined : {
        ...outputs,
        setOutput: (name: string, value: unknown) => {
            outputs.setOutput(name, value);
            if (name !== 'value') return;
            const before = reported.current;
            reported.current = value;
            if (!userChange.current) return;
            userChange.current = false;
            run(before, value);
        },
    }, [outputs, run]);

    return { declared: bindings.length > 0, onChange, outputs: wrapped, revision };
}

type Kanban = ComponentType<RegisteredComponentProps>;

/**
 * The default blueprint's kanban board, running the document's `on change` for the card the user moved.
 *
 * The board is the blueprint's own; this only gives it what the document's interaction needs: the card, as its row
 * with the column it was dropped in, and that column as `value`.
 */
export function StageKanbanBoard(props: RegisteredComponentProps) {
    const element = props.element as ExternalComponent;
    const cardColumn = typeof element.properties.cardColumn === 'string' ? element.properties.cardColumn : 'column';
    const cardId = typeof element.properties.cardId === 'string' ? element.properties.cardId : 'id';
    const change = useItemChange({ element, rowsProperty: 'cards', idField: cardId, valueField: cardColumn }, props.bindingOutputs);
    const Board = KanbanBoard as Kanban;
    if (!change.declared) return <Board {...props} />;
    return <Board key={change.revision} {...props} interactions={{ ...props.interactions, onChange: change.onChange }} bindingOutputs={change.outputs} />;
}

/** The registry key the default blueprint's kanban board is registered under, which the Stage's own board replaces. */
export const kanbanBoardKey = componentRegistryKey(defaultBlueprintName, ComponentName.KanbanBoard);
