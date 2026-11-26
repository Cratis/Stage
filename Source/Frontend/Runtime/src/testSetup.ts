// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach } from 'vitest';
import { cleanup, configure } from '@testing-library/react';
import { Globals } from '@cratis/arc';
import { QueryTransportMethod } from '@cratis/arc/queries';
import { registerQueryRouteResolver } from './queryExecutor';
import { clearBindings } from '@cratis/scene.components';

// The specs run in parallel and each draws a whole application shell; a loaded runner needs more than the one second a query waits by default.
configure({ asyncUtilTimeout: 5000 });

// Arc's availability client observes `/.cratis/availability`; the specs answer it through a stubbed fetch, so they poll rather than open a socket.
Globals.queryTransportMethod = QueryTransportMethod.Polling;
Globals.queryPollingOptions = { ...Globals.queryPollingOptions, intervalMs: 1000, minIntervalMs: 1000, jitterRatio: 0 };

afterEach(() => {
    cleanup();
    clearBindings();
    registerQueryRouteResolver(undefined);
});
