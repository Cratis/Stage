// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';

/**
 * Where the running application registered its modeled commands and queries.
 */
export interface StageRoutes {
    commands: Record<string, string>;
    queries: Record<string, string>;
}

export const stageRoutes = 'stage/routes';

/**
 * Reads the routes the Stage registered.
 *
 * An element carries the route it is backed by, but an interaction only names a command - so running one needs
 * this lookup. Answering `undefined` until it arrives is deliberate: a dispatcher that guessed a URL would
 * turn a missing registration into a 404 at the worst possible moment instead of a message.
 */
export function useStageRoutes(): StageRoutes | undefined {
    const [routes, setRoutes] = useState<StageRoutes>();

    useEffect(() => {
        const abort = new AbortController();
        fetch(stageRoutes, { signal: abort.signal })
            .then(response => (response.ok ? response.json() as Promise<StageRoutes> : undefined))
            .then(resolved => { if (resolved) setRoutes(resolved); })
            .catch(() => { /* A Stage that serves no routes still renders; nothing can be executed, and that is reported when tried. */ });
        return () => abort.abort();
    }, []);

    return routes;
}
