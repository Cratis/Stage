// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, useMemo } from 'react';
import type { ReactNode } from 'react';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import { selectableRowsOf, selectedRowsOf } from './StageCommandForm';
import { useStageData } from './stageData';
import { isBoardComponent } from './stageBoardSelection';

/**
 * The row-source scopes of one screen, including tables and board card queries nested in its slots. An outlet belongs to another screen, so its children are not visited.
 *
 * @param element The composed screen.
 * @returns The distinct source scopes, in first-use order, matching the queries and components that publish selectable rows.
 */
export function screenTablesOf(element: SceneElement): string[] {
    const tables = new Set<string>();
    const visit = (node: SceneElement): void => {
        const component = node as ExternalComponent;
        if (component.componentName === 'core:outlet') return;
        rowScopesOf(component).forEach(scope => tables.add(scope));
        for (const children of Object.values(component.slots ?? {})) children.forEach(visit);
    };
    visit(element);
    return [...tables];
}

function rowScopesOf(component: ExternalComponent): string[] {
    if (component.componentName === 'core:table') return [component.id];
    if (!isBoardComponent(component.componentName)) return [];
    const cards = component.properties.cards as { kind?: string; query?: unknown } | undefined;
    const query = cards?.kind === 'queryResult' && typeof cards.query === 'string' ? [`binding:${cards.query}`] : [];
    return [component.id, ...query];
}

const ScreenTablesContext = createContext<ReadonlySet<string> | undefined>(undefined);

/** Limits action selection and hidden form identifiers to the tables and boards of this screen, independently of other screens on the page. */
export function StageScreenTables({ element, outputScope, children }: { element: SceneElement; outputScope?: string; children: ReactNode }) {
    const tables = useMemo(() => new Set(screenTablesOf(element).flatMap(source => outputScope ? [source, `${outputScope}:${source}`] : [source])), [element, outputScope]);
    return <ScreenTablesContext.Provider value={tables}>{children}</ScreenTablesContext.Provider>;
}

/** Preserves the page-wide selection of a confirmation decision and its form after leaving the list screen. */
export function StagePageRows({ children }: { children: ReactNode }) {
    return <ScreenTablesContext.Provider value={undefined}>{children}</ScreenTablesContext.Provider>;
}

/**
 * The rows a person could select and has selected in this screen's tables and boards, each carrying the entity keys the command form recognizes.
 * Only a board's card query admits a `binding:` source; unrelated bindings, `data:` declarations and other screens cannot gate an action or fill its identifier.
 * Without a screen provider, direct consumers retain the page-wide behavior.
 */
export function useScreenRows(): { selectableRows: Record<string, unknown>[]; selectedRows: Record<string, unknown>[] } {
    const tables = useContext(ScreenTablesContext);
    const { queries, selections } = useStageData();
    return useMemo(() => {
        const scopedQueries = tables ? Object.fromEntries(Object.entries(queries).filter(([scope]) => tables.has(scope))) : queries;
        const scopedSelections = tables ? Object.fromEntries(Object.entries(selections).filter(([scope]) => tables.has(scope))) : selections;
        return { selectableRows: selectableRowsOf(scopedQueries), selectedRows: selectedRowsOf(scopedSelections, scopedQueries) };
    }, [queries, selections, tables]);
}
