// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ArcConfiguration } from '@cratis/arc.react';
import { ObservableQueryFor, Paging, QueryFor, type QueryResult } from '@cratis/arc/queries';
import { resolveExactQuery } from '@cratis/scene.components';

type StageQuery = QueryFor<unknown> | ObservableQueryFor<unknown>;
type StageQueryType = new () => StageQuery;

/** Resolves a modeled name first; a route only disambiguates that name's generated bindings. */
export function registeredQuery(name: string, route?: string): StageQueryType | undefined {
    let candidate = resolveExactQuery(name);
    if (candidate === 'ambiguous' && route) candidate = resolveExactQuery(`${name}:${route.replace(/^\//, '').split('?')[0]}`);
    return isQuery(candidate) ? candidate : undefined;
}

function isQuery(candidate: unknown): candidate is StageQueryType {
    return typeof candidate === 'function' && (candidate.prototype instanceof QueryFor || candidate.prototype instanceof ObservableQueryFor);
}

/** Applies the same host configuration as Arc's query hooks. */
function configureQuery(query: StageQuery, arc: ArcConfiguration): void {
    query.setMicroservice(arc.microservice);
    query.setApiBasePath(arc.apiBasePath ?? '');
    query.setOrigin(arc.origin ?? '');
    query.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
    query.paging = Paging.noPaging;
}

/** Performs a native Arc query, preserving its typed result. */
export function performQuery(queryType: StageQueryType, args: Record<string, unknown>, arc: ArcConfiguration): Promise<QueryResult<unknown>> {
    const query = new queryType();
    configureQuery(query, arc);
    return query.perform(args);
}

/** Subscribes through Arc's configured transport and returns its native cleanup. */
export function subscribeQuery(queryType: StageQueryType, args: Record<string, unknown>, arc: ArcConfiguration, callback: (result: QueryResult<unknown>) => void) {
    const query = new queryType();
    if (!(query instanceof ObservableQueryFor)) return undefined;
    configureQuery(query, arc);
    return query.subscribe(callback, args);
}
