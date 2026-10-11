// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Property } from './Property';

/** One read model, including those with no committed instances. */
export interface Summary {
    id: string;
    name: string;
    module: string;
    feature: string;
    slice: string;
    properties: Property[];
    instances: number;
}
