// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { field, Guid } from '@cratis/fundamentals';

/** The field-decorated model shape emitted for the generated application's query proxy. */
export class WorkItemSummary {
    workItemId!: Guid;
    title!: string;
    status!: number;
}

// Invoke the decorators directly: the runtime's Vitest transform preserves decorator syntax.
field(Guid)(WorkItemSummary.prototype, 'workItemId');
field(String)(WorkItemSummary.prototype, 'title');
field(Number)(WorkItemSummary.prototype, 'status');
