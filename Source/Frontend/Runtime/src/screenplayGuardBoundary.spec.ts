// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, readdirSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import ts from 'typescript';

describe('the Stage-owned interaction execution boundary', () => {
    it('imports the native runner only inside the shared Screenplay helper', () => {
        const files = readdirSync('src', { recursive: true, encoding: 'utf8' }).filter(name => /\.tsx?$/.test(name));
        for (const name of files) {
            for (const node of engineImports(name)) assertRunnerIsOwned(node, name);
        }
    });
});

function engineImports(name: string) {
    const source = ts.createSourceFile(name, readFileSync(`src/${name}`, 'utf8'), ts.ScriptTarget.Latest, true);
    return source.statements.filter(ts.isImportDeclaration)
        .filter(node => node.moduleSpecifier.getText(source).slice(1, -1) === '@cratis/scene.engine');
}

function assertRunnerIsOwned(node: ts.ImportDeclaration, name: string) {
    const bindings = node.importClause?.namedBindings;
    if (!bindings) return;
    expect(ts.isNamespaceImport(bindings), name).toBe(false);
    if (!ts.isNamedImports(bindings)) return;
    const imports = bindings.elements.map(entry => (entry.propertyName ?? entry.name).text);
    if (imports.includes('runBindings')) expect(name).toBe('screenplayGuard.ts');
}
