// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** What executing a modeled command came back with. */
export interface StageCommandOutcome {
    isSuccess: boolean;
    response?: unknown;
    validationErrors: { member: string; message: string }[];
    /** Why the command did not run or failed, as a person should read it. Empty on success. */
    messages: string[];
}
