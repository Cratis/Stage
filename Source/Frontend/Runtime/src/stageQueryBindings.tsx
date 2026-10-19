// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import type { ExternalComponent, SceneElement } from '@cratis/scene.model';
import { dataComponent } from './stageDataSources';
import { useStageData, useStageQuery } from './stageData';

/**
 * The names of the queries an element tree binds property values to - `{ kind: 'queryResult', query, path }` - wherever it is
 * placed: a count in the shell's topbar, a figure in a template, a value in a screen.
 *
 * @param element The composed screen, shell included.
 * @returns The distinct query names, in first-use order.
 */
export function queryBindingNames(element: SceneElement): string[] {
    const names = new Set<string>();
    const visitValue = (value: unknown): void => {
        if (Array.isArray(value)) {
            value.forEach(visitValue);
            return;
        }

        if (value === null || typeof value !== 'object') return;
        const record = value as Record<string, unknown>;
        if (record.kind === 'queryResult' && typeof record.query === 'string' && record.query.length > 0) {
            names.add(record.query);
            return;
        }

        Object.values(record).forEach(visitValue);
    };
    const visit = (node: SceneElement): void => {
        const component = node as ExternalComponent;
        if (component.properties) Object.values(component.properties).forEach(visitValue);
        for (const children of Object.values(component.slots ?? {})) children.forEach(visit);
    };

    visit(element);
    return [...names];
}

/** A modeled `data` declaration: the read model a screen shows, the query that reads it and the parameter that query is read by. */
export interface StageDataSource {
    typeName: string;
    query: string;
    by?: string;
    route?: string;
}

/**
 * The `data` declarations of an element tree - `data BasketSize via query GetBasketSize by id` - that a table or summary shows a read model through.
 *
 * @param element The composed screen.
 * @returns One source for each read model, in first-use order.
 */
export function dataSourcesOf(element: SceneElement): StageDataSource[] {
    const sources = new Map<string, StageDataSource>();
    const text = (component: ExternalComponent, name: string) => {
        const value = component.properties?.[name];
        return typeof value === 'string' && value.length > 0 ? value : undefined;
    };
    const visit = (node: SceneElement): void => {
        const component = node as ExternalComponent;
        if (component.componentName === dataComponent) {
            const typeName = text(component, 'typeName');
            const query = text(component, 'query');
            if (typeName && query && !sources.has(typeName)) sources.set(typeName, { typeName, query, by: text(component, 'by'), route: text(component, 'route') });
        }

        for (const children of Object.values(component.slots ?? {})) children.forEach(visit);
    };

    visit(element);
    return [...sources.values()];
}

/** Reads the query a `data` declaration names and publishes its rows under the read model's name, which is where a summary looks them up. */
function BoundData({ source }: { source: StageDataSource }) {
    const { routes, routesReady, parameters } = useStageData();
    const argument = source.by ? parameters[source.by] : undefined;
    const queryArguments = useMemo(() => source.by && argument ? { [source.by]: argument } : {}, [argument, source.by]);
    useStageQuery({
        scope: `data:${source.typeName}`,
        name: source.query,
        alias: source.typeName,
        route: routes?.queries[source.query] ?? source.route,
        arguments: queryArguments,
        ready: routesReady && (!source.by || !!argument),
        list: true,
    });
    return null;
}

/** Reads one query and publishes its rows, so every binding to it - wherever on the page - resolves against them. */
function BoundQuery({ name }: { name: string }) {
    const { routes, routesReady } = useStageData();
    useStageQuery({ scope: `binding:${name}`, name, route: routes?.queries[name], ready: routesReady, list: true });
    return null;
}

/**
 * Runs the queries that element properties are bound to.
 *
 * A table reads its own read model; a property bound to a query result - the basket count of a storefront's topbar - and a summary
 * of a `data` declaration have no table to read it, so without this they would stay empty. Each distinct query is read once, follows
 * the refresh a command triggers (so the count changes after an add to basket), and publishes its rows next to every other query's;
 * a `data` declaration's rows are published under the read model's name, as a list.
 *
 * @param props.element The composed screen.
 * @returns Nothing visible.
 */
export function StageQueryBindings({ element }: { element: SceneElement }) {
    const names = useMemo(() => queryBindingNames(element), [element]);
    const sources = useMemo(() => dataSourcesOf(element), [element]);
    return (
        <>
            {names.map(name => <BoundQuery key={name} name={name} />)}
            {sources.map(source => <BoundData key={source.typeName} source={source} />)}
        </>
    );
}
