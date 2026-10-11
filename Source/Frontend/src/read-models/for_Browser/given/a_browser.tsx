// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { PrimeReactProvider } from '@primereact/core/config';
import { vi } from 'vitest';
import { Browser } from '../../Browser';
import type { Summary } from '../../Summary';
import type { Detail } from '../../Detail';
import { stageTheme } from '../../../stageTheme';

export const model: Summary = {
    id: 'sm:model', name: 'ProjectSummary', module: 'Projects', feature: 'Registration', slice: 'ProjectLookup',
    properties: [{ name: 'name', type: 'String' }, { name: 'tags', type: 'String[]' }, { name: 'address', type: 'Address' }], instances: 1,
};
export const detail: Detail = {
    id: model.id, name: model.name, properties: model.properties,
    instances: [{ key: 'project-one', values: { name: 'Screenplay', tags: ['one', 'two'], address: { city: 'Oslo' } } }],
};

export function response(body: unknown, status = 200): Response {
    return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

export function serve(models: Summary[] = [model], selected = detail) {
    const fetch = vi.fn((input: RequestInfo | URL) => Promise.resolve(
        String(input).endsWith('/stage/read-models') || String(input) === 'stage/read-models'
            ? response({ readModels: models }) : response(selected)));
    vi.stubGlobal('fetch', fetch);
    return fetch;
}

export function renderBrowser() {
    return render(<PrimeReactProvider theme={stageTheme}><Browser /></PrimeReactProvider>);
}
