// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { stageEndpoints } from './stageSource';

export type StageModelReferenceKind = 'module' | 'feature' | 'slice' | 'screen';

/** The identifier the source model gave to one element of a published application. */
export interface StageModelReference {
    kind: StageModelReferenceKind;
    /** The dot-separated qualified name, for example `Shop.Orders.PlaceOrder`. */
    name: string;
    id: string;
}

/** What a published application knows about its own model, served by `stage/model-references`. */
export interface StageModelReferences {
    applicationName: string;
    publishedName: string;
    applicationVersion: string;
    publicationId: string;
    /** Empty when the source model supplied no identifiers. */
    references: StageModelReference[];
}

/** Reads the model references of a published application; a sandbox host has none and answers `undefined`. */
export async function fetchModelReferences(signal?: AbortSignal, fetcher: typeof fetch = fetch): Promise<StageModelReferences | undefined> {
    const response = await fetcher(stageEndpoints.modelReferences, { signal });
    return response.ok ? await response.json() as StageModelReferences : undefined;
}

/** The identifier the source model gave to the named element, if it gave one. */
export function modelReferenceFor(references: StageModelReferences | undefined, kind: StageModelReferenceKind, name: string): string | undefined {
    return references?.references.find(reference => reference.kind === kind && reference.name === name)?.id;
}
