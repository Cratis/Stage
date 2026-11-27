// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState, type ReactNode } from 'react';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '@cratis/arc.react/queries';

/**
 * Gives a spec its own Arc query cache, so a decision one spec observed is never replayed to the next one
 * through the cache every Arc hook shares when no `Arc` provider is mounted.
 */
export function FreshQueries({ children }: { children: ReactNode }) {
    const [cache] = useState(() => new QueryInstanceCache(0));
    useEffect(() => () => cache.dispose(), [cache]);
    return <QueryInstanceCacheContext.Provider value={cache}>{children}</QueryInstanceCacheContext.Provider>;
}
