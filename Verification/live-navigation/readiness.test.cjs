// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const assert = require('node:assert/strict');
const { spawnSync } = require('node:child_process');
const { copyFileSync, existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } = require('node:fs');
const { join, resolve } = require('node:path');
const { test } = require('node:test');

function run(context, runner, states) {
    const repository = resolve(__dirname, '../..');
    const work = join(repository, '.ai-work');
    mkdirSync(work, { recursive: true });
    const root = mkdtempSync(join(work, 'stage-readiness-test-'));
    context.after(() => rmSync(root, { recursive: true, force: true }));
    const here = join(root, 'Verification', runner);
    const bin = join(root, 'bin');
    mkdirSync(here, { recursive: true });
    mkdirSync(bin);
    mkdirSync(join(root, 'Verification/generated-react-browser/node_modules/playwright'), { recursive: true });
    copyFileSync(join(repository, 'Verification', runner, 'run.sh'), join(here, 'run.sh'));
    writeFileSync(join(here, 'package.json'), '{}');
    writeFileSync(join(root, 'states.json'), JSON.stringify(states));
    writeFileSync(join(root, 'polls'), '0');
    writeFileSync(join(bin, 'curl'), `#!${process.execPath}
const fs = require('node:fs');
const root = process.env.STAGE_TEST_ROOT;
const polls = Number(fs.readFileSync(root + '/polls', 'utf8'));
const states = JSON.parse(fs.readFileSync(root + '/states.json', 'utf8'));
const state = states[Math.min(polls, states.length - 1)];
fs.writeFileSync(root + '/polls', String(polls + 1));
if (state === 'unreachable') process.exit(7);
process.stdout.write(state === 'malformed' ? '{' : JSON.stringify({ state }));
`, { mode: 0o755 });
    writeFileSync(join(bin, 'node'), `#!/usr/bin/env bash
case "$1" in
    -pe) exec "$STAGE_TEST_NODE" "$@" ;;
    -e) case "$2" in *.length*) echo 1 ;; *) echo Orders ;; esac ;;
    */prepare.cjs) exit 0 ;;
    */scenario.cjs) echo scenario >> "$STAGE_TEST_ROOT/scenarios" ;;
    *) exit 99 ;;
esac
`, { mode: 0o755 });
    writeFileSync(join(bin, 'docker'), `#!/usr/bin/env bash
if [ "$1" = logs ]; then echo "fixture container logs"; fi
`, { mode: 0o755 });
    for (const command of ['sleep', 'npm']) {
        writeFileSync(join(bin, command), '#!/usr/bin/env bash\nexit 0\n', { mode: 0o755 });
    }
    const result = spawnSync('bash', [join(here, 'run.sh'), 'fixture-image', join(root, 'results')], {
        encoding: 'utf8',
        timeout: 60000,
        env: {
            ...process.env,
            PATH: `${bin}:${process.env.PATH}`,
            TMPDIR: root,
            STAGE_TEST_NODE: process.execPath,
            STAGE_TEST_ROOT: root,
        },
    });
    assert.ifError(result.error);
    const scenarioFile = join(root, 'scenarios');
    return {
        ...result,
        polls: Number(readFileSync(join(root, 'polls'), 'utf8')),
        scenarios: existsSync(scenarioFile) ? readFileSync(scenarioFile, 'utf8').trim().split('\n').length : 0,
    };
}

for (const runner of ['live-navigation', 'live-blueprints']) {
    test(`${runner} waits through HTTP 200 loading before running its scenario`, context => {
        const result = run(context, runner, ['loading', 'loading', 'ready']);
        assert.equal(result.status, 0, result.stderr);
        assert.equal(result.polls, 3);
        assert.equal(result.scenarios, 1);
    });

    test(`${runner} fails immediately with container logs on unsupported`, context => {
        const result = run(context, runner, ['unsupported', 'ready']);
        assert.equal(result.status, 2, result.stderr);
        assert.equal(result.polls, 1);
        assert.equal(result.scenarios, 0);
        assert.match(result.stdout, /fixture container logs/);
        assert.match(result.stdout, /the Stage reported unsupported/);
    });

    test(`${runner} never runs a scenario if loading exhausts the polling budget`, context => {
        const result = run(context, runner, ['loading']);
        assert.equal(result.status, 2, result.stderr);
        assert.equal(result.polls, 90);
        assert.equal(result.scenarios, 0);
        assert.match(result.stdout, /fixture container logs/);
        assert.match(result.stdout, /the Stage did not become ready/);
    });

    test(`${runner} retries unreachable and malformed status responses`, context => {
        const result = run(context, runner, ['unreachable', 'malformed', 'ready']);
        assert.equal(result.status, 0, result.stderr);
        assert.equal(result.polls, 3);
        assert.equal(result.scenarios, 1);
    });
}
