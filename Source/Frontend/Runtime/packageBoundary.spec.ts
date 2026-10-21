// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { dirname, extname, join, resolve } from 'node:path';
import ts from 'typescript';
import { describe, expect, it } from 'vitest';
import runtime from './package.json';
import host from '../package.json';

describe('the runtime package boundary', () => {
    it('uses the host pins as peers instead of installing private runtime dependencies', () => {
        const pins: Record<string, string> = { ...host.dependencies, ...host.devDependencies };
        expect(Object.keys(runtime.peerDependencies).length).toBeGreaterThan(0);
        for (const [name, version] of Object.entries(runtime.peerDependencies)) {
            expect(version, name).toBe(pins[name]);
        }
        expect(runtime).not.toHaveProperty('dependencies');
    });

    it('ships resolvable ESM imports without bundling its peers', () => {
        const modules = readdirSync('dist', { recursive: true, encoding: 'utf8' }).filter(name => name.endsWith('.js'));
        expect(modules.length).toBeGreaterThan(0);
        for (const name of modules) {
            const filename = join('dist', name);
            const source = ts.createSourceFile(filename, readFileSync(filename, 'utf8'), ts.ScriptTarget.Latest, true, ts.ScriptKind.JS);
            function inspect(node: ts.Node) {
                const specifier = ts.isImportDeclaration(node) || ts.isExportDeclaration(node) ? node.moduleSpecifier :
                    ts.isCallExpression(node) && node.expression.kind === ts.SyntaxKind.ImportKeyword ? node.arguments[0] : undefined;
                if (specifier && ts.isStringLiteral(specifier)) {
                    const reference = specifier.text;
                    if (reference.startsWith('.')) {
                        expect(extname(reference), `${filename}: ${reference}`).not.toBe('');
                        expect(existsSync(resolve(dirname(filename), reference)), `${filename}: ${reference}`).toBe(true);
                    } else if (!reference.startsWith('node:')) {
                        const packageName = reference.split('/').slice(0, reference.startsWith('@') ? 2 : 1).join('/');
                        expect(Object.keys(runtime.peerDependencies), reference).toContain(packageName);
                    }
                }
                ts.forEachChild(node, inspect);
            }
            inspect(source);
        }
        for (const entry of [runtime.exports['.'], runtime.exports['./primereact'], runtime.exports['./vite']]) {
            expect(existsSync(entry.import)).toBe(true);
            expect(existsSync(entry.types)).toBe(true);
        }
        expect(readFileSync(runtime.exports['./styles.css'], 'utf8').length).toBeGreaterThan(0);
        expect(readFileSync(runtime.exports['.'].import, 'utf8')).toMatch(/import ['"]\.\/app\.css['"]/);
    });

    it('does not auto-install PrimeReact peers for a default application', () => {
        for (const name of Object.keys(runtime.peerDependencies).filter(name => /prime/i.test(name))) {
            const meta = runtime.peerDependenciesMeta as Record<string, { optional?: boolean } | undefined>;
            expect(meta[name]?.optional, name).toBe(true);
        }
    });

    it('has no static or dynamic PrimeReact or theme import reachable from the root entry', () => {
        const visited = new Set<string>();
        function inspect(filename: string) {
            if (visited.has(filename) || !filename.endsWith('.js')) return;
            visited.add(filename);
            const source = ts.createSourceFile(filename, readFileSync(filename, 'utf8'), ts.ScriptTarget.Latest, true, ts.ScriptKind.JS);
            function visit(node: ts.Node) {
                const specifier = ts.isImportDeclaration(node) || ts.isExportDeclaration(node) ? node.moduleSpecifier :
                    ts.isCallExpression(node) && node.expression.kind === ts.SyntaxKind.ImportKeyword ? node.arguments[0] : undefined;
                if (specifier && ts.isStringLiteral(specifier)) {
                    const reference = specifier.text;
                    expect(reference, filename).not.toMatch(/prime|stageTheme/i);
                    if (reference.startsWith('.')) inspect(resolve(dirname(filename), reference));
                }
                ts.forEachChild(node, visit);
            }
            visit(source);
        }
        inspect(resolve(runtime.exports['.'].import));
    });

    it('has one installed instance of React, Arc and each Scene registry peer throughout the workspace tree', () => {
        const names = Object.keys(runtime.peerDependencies).filter(name => name.startsWith('@cratis/') || name === 'react' || name === 'react-dom');
        const paths = execFileSync('npm', ['ls', ...names, '--all', '--parseable'], { cwd: '..', encoding: 'utf8' }).trim().split('\n');
        const counts = new Map<string, number>();
        for (const path of paths) {
            const manifest: { name: string } = JSON.parse(readFileSync(join(path, 'package.json'), 'utf8'));
            counts.set(manifest.name, (counts.get(manifest.name) ?? 0) + 1);
        }
        for (const name of names) expect(counts.get(name), name).toBe(1);
    });
});
