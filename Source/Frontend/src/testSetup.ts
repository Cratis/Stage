// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach } from 'vitest';
import { cleanup, configure } from '@testing-library/react';

// The specs run in parallel and each draws a whole application shell; a loaded runner needs more than the one second a query waits by default.
configure({ asyncUtilTimeout: 5000 });

afterEach(cleanup);
