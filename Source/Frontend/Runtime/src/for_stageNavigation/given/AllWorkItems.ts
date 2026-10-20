// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor } from '@cratis/arc/queries';
import { WorkItemSummary } from './WorkItemSummary';

/** A native query proxy with the generated model's UUID field metadata. */
export class AllWorkItems extends ObservableQueryFor<WorkItemSummary[]> {
    readonly route = '/api/all-work-items';
    readonly queryName = 'AllWorkItems';
    readonly defaultValue = [];
    readonly requiredRequestParameters = [];
    readonly parameterDescriptors = [];

    constructor() { super(WorkItemSummary, true); }
}
