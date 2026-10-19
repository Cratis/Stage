// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { SceneElement } from '@cratis/scene.model';
import { dataSourcesOf, queryBindingNames } from './stageQueryBindings';

const element = (id: string, properties: Record<string, unknown> = {}, slots: Record<string, SceneElement[]> = {}) =>
    ({ id, name: id, componentName: 'test:component', properties, slots }) as unknown as SceneElement;

describe('when finding the queries an element tree binds property values to', () => {
    it('should find a query result bound in a nested slot', () => {
        const tree = element('shell', {}, { topbar: [element('bar', {}, { end: [element('cart', { count: { kind: 'queryResult', query: 'AllBasketSizes', path: '0.lineCount' } })] })] });

        expect(queryBindingNames(tree)).toEqual(['AllBasketSizes']);
    });

    it('should name each query once, in the order they are first used', () => {
        const tree = element('shell', {}, { content: [
            element('a', { count: { kind: 'queryResult', query: 'B', path: '0.x' } }),
            element('b', { total: { kind: 'queryResult', query: 'A', path: '0.y' } }),
            element('c', { other: { kind: 'queryResult', query: 'B', path: '0.z' } }),
        ] });

        expect(queryBindingNames(tree)).toEqual(['B', 'A']);
    });

    it('should find a binding inside a list property', () => {
        const tree = element('a', { items: [{ label: 'x', count: { kind: 'queryResult', query: 'Q', path: '0.n' } }] });

        expect(queryBindingNames(tree)).toEqual(['Q']);
    });

    it('should ignore a binding to a literal, a data context or a component output', () => {
        const tree = element('a', { one: { kind: 'literal', value: 1, path: '' }, two: { path: 'name' }, three: { kind: 'componentProperty', componentId: 'x', path: 'y' } });

        expect(queryBindingNames(tree)).toEqual([]);
    });
});

describe('when finding the data declarations of an element tree', () => {
    const data = (id: string, properties: Record<string, unknown>) => ({ id, name: id, componentName: 'core:data', properties, slots: {} }) as unknown as SceneElement;

    it('should find a declaration in any slot, once for each read model', () => {
        const tree = element('shell', {}, { topbar: [data('a', { typeName: 'BasketSize', query: 'AllBasketSizes' })], content: [data('b', { typeName: 'BasketSize', query: 'GetBasketSize', by: 'basketId' }), data('c', { typeName: 'Product', query: 'AllProducts' })] });

        expect(dataSourcesOf(tree).map(source => [source.typeName, source.query])).toEqual([['BasketSize', 'AllBasketSizes'], ['Product', 'AllProducts']]);
    });

    it('should leave out a declaration that names no query', () => {
        expect(dataSourcesOf(element('shell', {}, { content: [data('a', { typeName: 'Product' })] }))).toEqual([]);
    });
});
