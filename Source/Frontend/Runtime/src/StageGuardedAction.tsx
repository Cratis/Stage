// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import type { ExternalComponent } from '@cratis/scene.model';
import type { InteractionHandlers } from '@cratis/scene.react';
import { Button } from 'primereact/button';
import { useStageData, useStageQuery } from './stageData';
import { registeredCommand, useStageCommandExecutor } from './commandExecutor';
import { canEvaluate, firstMatch, subjectPath, subjectValue, unevaluableGuardCode, type SubjectReference } from './stageGuards';
import { navigateToScreen } from './stageNavigation';

/** The properties a guarded action carries in the Scene. */
interface GuardedBranch {
    command?: string;
    condition?: unknown;
    arguments?: Record<string, SubjectReference>;
}

interface GuardedOtherwise extends GuardedBranch {
    outcome?: string;
}

export interface StageGuardedActionProps {
    element: ExternalComponent;
    interactions?: InteractionHandlers;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function text(element: ExternalComponent, name: string): string {
    const value = element.properties[name];
    return typeof value === 'string' ? value : '';
}

/** Whether an element is an action authored with `when ... otherwise`. */
export function isGuardedAction(element: ExternalComponent): boolean {
    return element.componentName === 'core:action' && (Array.isArray(element.properties.alternatives) || element.properties.otherwise !== undefined);
}

/**
 * Reads the item a guarded action is about: the single record its nearest `data` declaration reads, which the data
 * source binding carried onto the action as `itemQuery`, `itemRoute` and `itemArguments` - or, when that declaration reads
 * a collection (`itemSelection`), the row the person selected.
 */
function useGuardSubject(element: ExternalComponent) {
    const data = useStageData();
    const query = text(element, 'itemQuery');
    const route = (query ? data.routes?.queries?.[query] : undefined) ?? text(element, 'itemRoute');
    const argumentBindings = isRecord(element.properties.itemArguments) ? element.properties.itemArguments : {};
    const values: Record<string, unknown> = {};
    let ready = true;
    for (const [name, binding] of Object.entries(argumentBindings)) {
        // Scene may already have resolved a binding to an object value, such as a native proxy's Guid.
        const isBinding = isRecord(binding) && (typeof binding.path === 'string' || typeof binding.kind === 'string');
        const value = isBinding ? data.resolveBinding(binding as never) : binding;
        values[name] = value;
        if (value === undefined || value === null || value === '') ready = false;
    }

    // A collection subject is read through the table that shows it: the item is the one row selected on the screen.
    const selection = element.properties.itemSelection === true;
    const selectedRows = Object.values(data.selections).filter((row): row is Record<string, unknown> => row !== undefined);
    const result = useStageQuery({ scope: `${element.id}:item`, name: query || undefined, route: route || undefined, arguments: values, ready: ready && !selection, publish: false });
    if (selection) return { hasSource: true, item: selectedRows.length === 1 ? selectedRows[0] : undefined };
    return { hasSource: !!query || !!route, item: result.rows.length === 1 ? result.rows[0] : undefined };
}

function argumentsFor(branch: GuardedBranch, item: Record<string, unknown>): Record<string, unknown> {
    return Object.fromEntries(Object.entries(branch.arguments ?? {}).map(([name, binding]) => [name, subjectValue(subjectPath(binding) ?? '', item)]));
}

/**
 * Renders an action authored with `when <guard> execute <command> ... otherwise ...`.
 *
 * The guard is evaluated against the item on every render, so the action follows the data: a work item that is
 * closed while the screen is open stops offering Close. The first alternative that holds is the command the action
 * executes; when none holds, the authored `otherwise` decides - `hidden` renders nothing, `execute` offers its
 * command instead. Without an item there is nothing to decide about, so nothing is offered.
 *
 * A guard this runtime cannot evaluate never lets a command through: the action is shown disabled with the
 * diagnostic, which is the same refusal Stage reports when it admits the Scene.
 */
export function StageGuardedAction({ element, interactions }: StageGuardedActionProps) {
    const data = useStageData();
    const subject = useGuardSubject(element);
    const executeCommand = useStageCommandExecutor();
    const [messages, setMessages] = useState<string[]>([]);
    const [running, setRunning] = useState(false);
    const label = text(element, 'label');
    const alternatives = useMemo(() => (Array.isArray(element.properties.alternatives) ? element.properties.alternatives : []) as GuardedBranch[], [element]);
    const otherwise = (isRecord(element.properties.otherwise) ? element.properties.otherwise : undefined) as GuardedOtherwise | undefined;
    const conditions = alternatives.map(alternative => alternative.condition);
    const otherwiseKnown = !otherwise || otherwise.outcome === 'Hidden' || (otherwise.outcome === 'Execute' && !!otherwise.command);

    if (!canEvaluate(conditions) || !otherwiseKnown || !subject.hasSource) {
        const reason = !subject.hasSource
            ? `${unevaluableGuardCode}: “${label}” has no data subject to evaluate its guard against.`
            : `${unevaluableGuardCode}: “${label}” has a guard this Stage cannot evaluate, so it stays unavailable.`;
        return (
            <div className='stage-guarded-action' data-scene-id={element.id}>
                <Button type='button' disabled title={reason} {...interactions}>{label}</Button>
                <p role='alert' className='stage-form-error'>{reason}</p>
            </div>
        );
    }

    if (!subject.item) return null;

    const match = firstMatch(conditions, subject.item);
    const branch: GuardedBranch | undefined = match !== undefined && match >= 0 ? alternatives[match] : otherwise?.outcome === 'Execute' ? otherwise : undefined;
    if (!branch?.command) return null;

    const command = branch.command;
    const commandType = registeredCommand(command);
    const item = subject.item;
    const run = async () => {
        if (!commandType) return;
        setRunning(true);
        const outcome = await executeCommand(command, argumentsFor(branch, item));
        setRunning(false);
        setMessages(outcome.messages);
        if (!outcome.isSuccess) return;
        data.refreshAfterCommand();
        const screen = text(element, 'navigateToScreen');
        const by = text(element, 'navigateByParameter');
        const identity = by ? subjectValue(`item.${by}`, item) : undefined;
        if (screen) navigateToScreen(screen, by && identity !== undefined && identity !== null ? { [by]: String(identity) } : {});
    };

    return (
        <div className='stage-guarded-action' data-scene-id={element.id} data-command={command}>
            <Button
                {...interactions}
                type='button'
                disabled={!commandType || running}
                title={commandType ? undefined : 'This command is not exposed as an API yet'}
                onClick={() => void run()}>
                {label}
            </Button>
            {messages.map(message => <p key={message} role='alert' className='stage-form-error'>{message}</p>)}
        </div>
    );
}
