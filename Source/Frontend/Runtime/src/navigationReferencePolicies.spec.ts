// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { createRequire } from 'node:module';
import ts from 'typescript';
import { describe, expect, it } from 'vitest';
import { arrangementNavigationPolicies, navigationReferencePolicies, type NavigationFieldPolicy } from './navigationReferencePolicies';
import { navigationNonReferenceFields } from './navigationNonReferenceFields';

const policies: Record<string, Record<string, NavigationFieldPolicy>> = { ...navigationReferencePolicies, ...arrangementNavigationPolicies };

function declarations(directory: string): string[] {
    return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
        const path = join(directory, entry.name);
        if (entry.isDirectory()) return entry.name === 'dist' ? [] : declarations(path);
        return entry.name.endsWith('.ts') ? [path] : [];
    });
}

function declaredFields(source: string): Record<string, string[]> {
    const ast = ts.createSourceFile('scene.ts', source, ts.ScriptTarget.Latest, true);
    const types: Record<string, string[]> = {};
    for (const statement of ast.statements) {
        if (ts.isInterfaceDeclaration(statement)) types[statement.name.text] = propertyNames(statement.members, ast);
        if (ts.isTypeAliasDeclaration(statement)) {
            const members = typeLiteralMembers(statement.type);
            if (members.length) types[statement.name.text] = propertyNames(members, ast);
        }
    }
    return types;
}

function typeLiteralMembers(node: ts.Node): ts.TypeElement[] {
    if (ts.isTypeLiteralNode(node)) return [...node.members];
    const members: ts.TypeElement[] = [];
    node.forEachChild(child => { members.push(...typeLiteralMembers(child)); });
    return members;
}

function propertyNames(members: readonly ts.TypeElement[], source: ts.SourceFile): string[] {
    return members.filter(ts.isPropertySignature).map(member => member.name.getText(source).replace(/^['"]|['"]$/g, ''));
}

function unclassifiedFields(types: Record<string, string[]>): string[] {
    return Object.entries(types).flatMap(([name, fields]) => {
        const semantic = policies[name];
        if (semantic) return fields.filter(field => !semantic[field]?.reason).map(field => `${name}.${field}`);
        const nonReference = navigationNonReferenceFields[name];
        if (!nonReference?.reason) return [name];
        const listed = nonReference.fields.split(' ').filter(Boolean);
        return [...fields.filter(field => !listed.includes(field)), ...listed.filter(field => !fields.includes(field))].map(field => `${name}.${field}`);
    });
}

describe('the semantic navigation field inventory', () => {
    it('classifies every property of every pinned scene model interface and object type with an explicit reason', () => {
        const require = createRequire(import.meta.url);
        const root = dirname(require.resolve('@cratis/scene.model/package.json'));
        const types = Object.assign({}, ...declarations(root).map(path => declaredFields(readFileSync(path, 'utf8'))));
        expect(Object.keys(types).length).toBeGreaterThan(150);
        expect(unclassifiedFields(types)).toEqual([]);
    });

    it('rejects future fields on reference, non-reference and new object shapes instead of silently treating them as data', () => {
        const futureTypes = declaredFields(`
            interface Screen { name: string; futureDestination?: string }
            interface QueryBinding { arguments: unknown; query: string; queryId: string; results: unknown; futureDestination?: string }
            type FutureDestination = { destination: string };
        `);
        expect(unclassifiedFields(futureTypes)).toEqual(['Screen.futureDestination', 'QueryBinding.futureDestination', 'FutureDestination']);
    });
});
