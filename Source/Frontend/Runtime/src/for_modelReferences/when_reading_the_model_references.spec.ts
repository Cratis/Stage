// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import { fetchModelReferences, modelReferenceFor, type StageModelReferences } from '../modelReferences';

const document: StageModelReferences = {
    applicationName: 'Shop',
    publishedName: 'my-shop',
    applicationVersion: '1.2.0',
    publicationId: 'pub-1',
    references: [{ kind: 'slice', name: 'Shop.Orders.PlaceOrder', id: 'studio-slice-9' }],
};

describe('when reading the model references', () => {
    it('asks the host for the references document', async () => {
        const fetcher = vi.fn(async () => new Response(JSON.stringify(document)));
        const result = await fetchModelReferences(undefined, fetcher as unknown as typeof fetch);
        expect(fetcher).toHaveBeenCalledWith('stage/model-references', { signal: undefined });
        expect(result).toEqual(document);
    });

    it('answers nothing from a host that has none', async () => {
        const fetcher = vi.fn(async () => new Response('', { status: 404 }));
        expect(await fetchModelReferences(undefined, fetcher as unknown as typeof fetch)).toBeUndefined();
    });

    it('finds the identifier of a named element', () => {
        expect(modelReferenceFor(document, 'slice', 'Shop.Orders.PlaceOrder')).toBe('studio-slice-9');
    });

    it('finds nothing for an element the source model did not name', () => {
        expect(modelReferenceFor(document, 'module', 'Shop')).toBeUndefined();
        expect(modelReferenceFor(undefined, 'module', 'Shop')).toBeUndefined();
    });
});
