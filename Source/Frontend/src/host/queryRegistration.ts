// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryFor } from '@cratis/arc/queries';
import { registerQuery } from '@cratis/scene.components';
import type { StageRoutes } from '@cratis/stage.runtime';

/** The live host has routes rather than generated proxies; execution still belongs to Arc. */
export function registerStageQueries(routes: StageRoutes): void {
    for (const [name, registeredRoute] of Object.entries(routes.queries ?? {})) {
        const [path, parameters = ''] = registeredRoute.split('?');
        const query = new URLSearchParams(parameters);
        const live = query.get('waitForFirstResult') === 'true';
        query.delete('waitForFirstResult');
        const route = query.size > 0 ? `${path}?${query}` : path;
        const required = [...path.matchAll(/\{([^}:?]+)[^}]*\}/g)].map(match => match[1]);
        if (live) {
            registerQuery(name, class extends ObservableQueryFor<unknown> {
                readonly route = route;
                constructor() { super(Object, false); }
                readonly defaultValue = undefined;
                readonly requiredRequestParameters = required;
                readonly parameterDescriptors = [];
            });
        } else {
            registerQuery(name, class extends QueryFor<unknown> {
                readonly route = route;
                constructor() { super(Object, false); }
                readonly defaultValue = undefined;
                readonly requiredRequestParameters = required;
                readonly parameterDescriptors = [];
            });
        }
    }
}
