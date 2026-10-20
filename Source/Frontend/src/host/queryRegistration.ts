// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryFor } from '@cratis/arc/queries';
import { registerQuery } from '@cratis/scene.components';
import { registerQueryRouteResolver, type StageRoutes } from '@cratis/stage.runtime';

/** The live host has routes rather than generated proxies; execution still belongs to Arc. */
export function registerStageQueries(routes: StageRoutes): void {
    const proxies = new Map<string, ReturnType<typeof createRouteQuery>>();
    const resolveRoute = (route: string) => {
        let proxy = proxies.get(route);
        if (!proxy) {
            proxy = createRouteQuery(route);
            proxies.set(route, proxy);
        }
        return proxy;
    };
    registerQueryRouteResolver(resolveRoute);
    for (const [name, route] of Object.entries(routes.queries ?? {})) registerQuery(name, resolveRoute(route));
}

function createRouteQuery(registeredRoute: string) {
    const [path, parameters = ''] = registeredRoute.split('?');
    const query = new URLSearchParams(parameters);
    const live = query.get('waitForFirstResult') === 'true';
    query.delete('waitForFirstResult');
    const route = query.size > 0 ? `${path}?${query}` : path;
    const required = [...path.matchAll(/\{([^}:?]+)[^}]*\}/g)].map(match => match[1]);
    if (live) {
        return class extends ObservableQueryFor<unknown> {
            readonly route = route;
            constructor() { super(Object, false); }
            readonly defaultValue = undefined;
            readonly requiredRequestParameters = required;
            readonly parameterDescriptors = [];
        };
    } else {
        return class extends QueryFor<unknown> {
            readonly route = route;
            constructor() { super(Object, false); }
            readonly defaultValue = undefined;
            readonly requiredRequestParameters = required;
            readonly parameterDescriptors = [];
        };
    }
}
