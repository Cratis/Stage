// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function deferResponse() {
    let complete!: (value: Response) => void;
    const pending = new Promise<Response>(resolve => { complete = resolve; });
    return { pending, complete };
}
