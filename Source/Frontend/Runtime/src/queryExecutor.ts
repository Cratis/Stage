// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ArcConfiguration } from '@cratis/arc.react';
import { ObservableQueryFor, Paging, QueryFor, type QueryResult } from '@cratis/arc/queries';
import { resolveExactQuery } from '@cratis/scene.components';
import { apiBasePathFor } from './hostedBasePath';

type StageQuery = QueryFor<unknown> | ObservableQueryFor<unknown>;
type StageQueryType = new () => StageQuery;

let routeResolver: ((route: string) => StageQueryType) | undefined;

/** Installs the live host's proxy factory; generated applications leave name resolution strict. */
export function registerQueryRouteResolver(resolver: typeof routeResolver): void {
    routeResolver = resolver;
}

/** Resolves modeled bindings first, then an attached route through the live host's factory. */
export function registeredQuery(name: string, route?: string): StageQueryType | undefined {
    let candidate = resolveExactQuery(name);
    if (candidate === 'ambiguous' && route) candidate = resolveExactQuery(`${name}:${route.replace(/^\//, '').split('?')[0]}`);
    return isQuery(candidate) ? candidate : route ? routeResolver?.(route) : undefined;
}

function isQuery(candidate: unknown): candidate is StageQueryType {
    return typeof candidate === 'function' && (candidate.prototype instanceof QueryFor || candidate.prototype instanceof ObservableQueryFor);
}

/** Applies the same host configuration as Arc's query hooks. */
function configureQuery(query: StageQuery, arc: ArcConfiguration): void {
    query.setMicroservice(arc.microservice);
    // Without the hosted fallback a Stage served under a prefix (Studio's Play) would query the root of the proxy's host.
    query.setApiBasePath(apiBasePathFor(arc.apiBasePath));
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
