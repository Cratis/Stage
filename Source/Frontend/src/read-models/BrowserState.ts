// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Detail } from './Detail';
import type { Summary } from './Summary';

/** A completed browser read or an explicit loading/error state, without stale rows. */
export interface BrowserState {
    loading: boolean;
    readModels?: Summary[];
    detail?: Detail;
    message?: string;
}
