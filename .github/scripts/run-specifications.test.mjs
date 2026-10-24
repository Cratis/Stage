// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { delimiter, join, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { test } from 'node:test';

const launcher = resolve('.github/scripts/run-specifications.sh');
const positiveTrx = '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><ResultSummary><Counters total="1" /></ResultSummary></TestRun>';

function run(t, args, { exitCode = 0, trx = positiveTrx } = {}) {
    const directory = mkdtempSync(join(tmpdir(), 'stage-specification-launcher-'));
    t.after(() => rmSync(directory, { recursive: true, force: true }));
    const captured = join(directory, 'arguments');
    writeFileSync(join(directory, 'dotnet'), `#!/usr/bin/env bash
printf '%s\\n' "$@" > "$CAPTURE_ARGUMENTS"
while (( $# )); do
    if [[ "$1" == "-result-trx" && "$WRITE_TRX" == true ]]; then
        printf '%s' "$TRX_CONTENT" > "$2"
        break
    fi
    shift
done
exit "$RUNNER_EXIT_CODE"
`, { mode: 0o755 });
    const result = spawnSync('bash', [launcher, ...args], {
        encoding: 'utf8',
        timeout: 10000,
        env: {
            ...process.env,
            PATH: directory + delimiter + process.env.PATH,
            CAPTURE_ARGUMENTS: captured,
            RUNNER_EXIT_CODE: String(exitCode),
            WRITE_TRX: String(trx !== null),
            TRX_CONTENT: trx ?? ''
        }
    });
    assert.ifError(result.error);
    return { ...result, arguments: existsSync(captured) ? readFileSync(captured, 'utf8').trimEnd().split('\n') : [] };
}

function resultPath(t) {
    const directory = mkdtempSync(join(tmpdir(), 'stage-specification-result-'));
    t.after(() => rmSync(directory, { recursive: true, force: true }));
    return join(directory, 'specs.trx');
}

test('rejects an invocation with no project', t => {
    const result = run(t, []);
    assert.equal(result.status, 2);
    assert.match(result.stderr, /Usage: run-specifications.sh/);
});

test('builds and runs a Debug executable with a validated temporary TRX result', t => {
    const result = run(t, ['Source/Contracts/Contracts.csproj']);
    assert.equal(result.status, 0, result.stderr);
    assert.deepEqual(result.arguments.slice(0, -1), [
        'run', '--project', 'Source/Contracts/Contracts.csproj', '--configuration', 'Debug',
        '--', '-noLogo', '-noColor', '-result-trx'
    ]);
    assert.equal(existsSync(result.arguments.at(-1)), false);
});

test('preserves guarded filters and result paths when running already-built output', t => {
    const filter = 'FullyQualifiedName!~excluded&(FullyQualifiedName~first|FullyQualifiedName~second)';
    const trx = resultPath(t);
    const result = run(t, ['some project/Specifications.csproj', '--no-build', '-filterVSTest', filter, '-result-trx', trx]);
    assert.equal(result.status, 0, result.stderr);
    assert.deepEqual(result.arguments, [
        'run', '--project', 'some project/Specifications.csproj', '--configuration', 'Debug', '--no-build',
        '--', '-noLogo', '-noColor', '-filterVSTest', filter, '-result-trx', trx
    ]);
});

for (const project of ['Host', 'SpecRunner']) {
    test(`selects specifications without replacing ${project}'s normal entry point`, t => {
        const path = `Source/${project}/${project}.csproj`;
        const result = run(t, [path, '--no-build', '-list', 'full']);
        assert.equal(result.status, 0, result.stderr);
        assert.deepEqual(result.arguments, [
            'run', '--project', path, '--configuration', 'Debug', '--no-build',
            '--', '--specifications', '-noLogo', '-noColor', '-list', 'full'
        ]);
    });
}

test('preserves the executable failure exit code', t => {
    const result = run(t, ['Source/Contracts/Contracts.csproj'], { exitCode: 7 });
    assert.equal(result.status, 7);
});

for (const [name, trx] of [
    ['empty run', positiveTrx.replace('total="1"', 'total="0"')],
    ['missing results', null],
    ['malformed results', '<TestRun'],
    ['missing counter', '<TestRun />'],
    ['wrong root', positiveTrx.replaceAll('TestRun', 'Other')],
    ['duplicate counters', positiveTrx.replace('<Counters total="1" />', '<Counters total="1" /><Counters total="1" />')],
    ['invalid count', positiveTrx.replace('total="1"', 'total="invalid"')]
]) {
    test(`rejects ${name}`, t => {
        const result = run(t, ['Source/Contracts/Contracts.csproj'], { trx });
        assert.equal(result.status, 1);
        assert.match(result.stderr, /Invalid TRX:/);
    });
}

test('does not accept a stale TRX when the executable produces no result', t => {
    const trx = resultPath(t);
    writeFileSync(trx, positiveTrx);
    const result = run(t, ['Source/Contracts/Contracts.csproj', '-result-trx', trx], { trx: null });
    assert.equal(result.status, 1);
    assert.equal(existsSync(trx), false);
});

test('rejects a missing TRX path before running the executable', t => {
    const result = run(t, ['Source/Contracts/Contracts.csproj', '-result-trx']);
    assert.equal(result.status, 2);
    assert.deepEqual(result.arguments, []);
});
