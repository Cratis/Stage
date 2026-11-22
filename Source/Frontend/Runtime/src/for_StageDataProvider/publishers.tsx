// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen } from '@testing-library/react';
import { PrimeReactProvider } from '@primereact/core';
import React from 'react';
import { StageDataProvider, useStageData, useStageQuery, type StageQueryRequest } from '../stageData';

/** What the specs' faked query executor answers with: one row carrying the `value` argument it was asked for. */
export const answerFor = (args: Record<string, unknown>) => ({ data: [{ value: args.value }], isSuccess: true, isValid: true, isAuthorized: true, hasExceptions: false });

/** Publishes the rows of a query whose one row carries `value`, under the given scope, name and alias. */
export function Publisher({ value, ...request }: Omit<StageQueryRequest, 'arguments' | 'route'> & { value: string }) {
    useStageQuery({ ...request, route: `/api/${request.scope}`, arguments: { value } });
    return null;
}

/** Shows what the provider's bindings resolve, one output per path. */
export function Probe({ paths }: { paths: string[] }) {
    const data = useStageData();
    return <>
        <output aria-label='results'>{Object.keys(data.queryResults).sort().join(',')}</output>
        <output aria-label='scopes'>{Object.keys(data.queries).sort().join(',')}</output>
        {paths.map(path => <output key={path} aria-label={path}>{JSON.stringify(data.resolveBinding({ path }) ?? null)}</output>)}
    </>;
}

export const resolved = (label: string) => screen.getByLabelText(label).textContent ?? '';

export function renderUnderProvider(ui: React.ReactElement, strict = false) {
    const Mode = strict ? React.StrictMode : React.Fragment;
    const tree = (children: React.ReactElement) => <Mode><PrimeReactProvider>
        <StageDataProvider routes={{ commands: {}, queries: {} }} locale='en' locales={['en']} screen='Products'>{children}</StageDataProvider>
    </PrimeReactProvider></Mode>;
    const view = render(tree(ui));
    return { ...view, rerenderUnderProvider: (next: React.ReactElement) => view.rerender(tree(next)) };
}
