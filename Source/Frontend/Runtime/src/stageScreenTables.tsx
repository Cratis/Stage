// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, useMemo } from 'react';
import type { ReactNode } from 'react';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import { selectableRowsOf, selectedRowsOf } from './StageCommandForm';
import { useStageData } from './stageData';

/**
 * The table ids of one screen, including tables nested in its slots. An outlet belongs to another screen, so its children are not visited.
 *
 * @param element The composed screen.
 * @returns The distinct table ids, in first-use order, matching the scopes under which tables publish their queries and selections.
 */
export function screenTablesOf(element: SceneElement): string[] {
    const tables = new Set<string>();
    const visit = (node: SceneElement): void => {
        const component = node as ExternalComponent;
        if (component.componentName === 'core:outlet') return;
        if (component.componentName === 'core:table') tables.add(component.id);
        for (const children of Object.values(component.slots ?? {})) children.forEach(visit);
    };
    visit(element);
    return [...tables];
}

const ScreenTablesContext = createContext<ReadonlySet<string> | undefined>(undefined);

/** Limits action selection and hidden form identifiers to the tables of this screen, independently of other screens on the page. */
export function StageScreenTables({ element, children }: { element: SceneElement; children: ReactNode }) {
    const tables = useMemo(() => new Set(screenTablesOf(element)), [element]);
    return <ScreenTablesContext.Provider value={tables}>{children}</ScreenTablesContext.Provider>;
}

/**
 * The rows a person could select and has selected in this screen's tables, each carrying the entity keys the command form recognizes.
 * Queries published for `data:` declarations or `binding:` values and tables on other screens cannot gate an action or fill its identifier.
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
