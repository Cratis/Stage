// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Instance } from './Instance';
import type { Property } from './Property';

/** The current instances and property contract of a selected read model. */
export interface Detail {
    id: string;
    name: string;
    properties: Property[];
    instances: Instance[];
}
