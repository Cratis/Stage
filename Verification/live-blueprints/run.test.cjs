// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const assert = require('node:assert/strict');
const { spawnSync } = require('node:child_process');
const { copyFileSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } = require('node:fs');
const { join, resolve } = require('node:path');
const test = require('node:test');

const models = ['crm', 'hr', 'ticketing', 'approvals'];
// These stand-ins never start Docker, contact a server or launch a browser. Delayed readiness and probes expose
// both overlapping network setup and a fast sibling's teardown while another probe is still importing modules.
const tool = `#!${process.execPath}
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const args = process.argv.slice(2);
const command = path.basename(process.argv[1]);
const record = (kind, index) => fs.appendFileSync(process.env.FAKE_EVENTS, JSON.stringify({ kind, index }) + '\\n');
const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
async function main() {
    if (command === 'npm') return 0;
    if (command === 'docker') {
        const index = Number((args[0] === 'run' ? args[args.indexOf('--name') + 1] : args.at(-1)).split('-').at(-1));
        record(args[0], index);
        if (args[0] === 'logs') console.log('Stage logs for model ' + index);
        return 0;
    }
    if (command === 'curl') {
        const index = Number(new URL(args.at(-1)).port) - 19392;
        if (index === 1) await delay(200);
        record('ready', index);
        console.log(JSON.stringify({ state: process.env.FAKE_STARTUP_FAILURE === String(index) ? 'unsupported' : 'ready' }));
        return 0;
    }
    if (args[0].endsWith('/prepare.cjs')) {
        fs.mkdirSync(args[3], { recursive: true });
        return 0;
    }
    if (args[0].endsWith('/scenario.cjs')) {
        const index = Number(new URL(process.env.STAGE_LIVE_URL).port) - 19392;
        record('probe-start', index);
        await delay(index === 0 ? 200 : 20);
        record('probe-end', index);
        return index === 0 ? Number(process.env.FAKE_PROBE_CODE || 0) : 0;
    }
    return spawnSync(${JSON.stringify(process.execPath)}, args, { stdio: 'inherit' }).status ?? 2;
}
main().then(code => process.exit(code));
`;

function runHarness(t, options = {}) {
    const root = resolve(__dirname, '../../.ai-work');
    mkdirSync(root, { recursive: true });
    const fixture = mkdtempSync(join(root, 'live-blueprints-run-test-'));
    t.after(() => rmSync(fixture, { recursive: true, force: true }));
    const harness = join(fixture, 'Verification/live-blueprints');
    const bin = join(fixture, 'bin');
    const results = join(fixture, 'results');
    const events = join(fixture, 'events.jsonl');
    for (const directory of [harness, bin, join(fixture, 'tmp'), join(fixture, 'Verification/generated-react-browser/node_modules/playwright')]) mkdirSync(directory, { recursive: true });
    copyFileSync(join(__dirname, 'run.sh'), join(harness, 'run.sh'));
    writeFileSync(join(harness, 'package.json'), '{}');
    writeFileSync(join(harness, 'blueprints.json'), JSON.stringify(models.map(model => ({ model }))));
    for (const name of ['npm', 'docker', 'curl', 'node']) writeFileSync(join(bin, name), tool, { mode: 0o755 });
    const result = spawnSync('bash', [join(harness, 'run.sh'), 'stage:test', results], {
        encoding: 'utf8', timeout: 15000,
        env: {
            ...process.env, PATH: `${bin}:${process.env.PATH}`, TMPDIR: join(fixture, 'tmp'),
            FAKE_EVENTS: events, FAKE_PROBE_CODE: String(options.probeCode ?? 0), FAKE_STARTUP_FAILURE: String(options.startupFailure ?? ''),
            STAGE_LIVE_PORT: '19392', STAGE_LIVE_CONCURRENCY: String(options.concurrency ?? 3),
        },
    });
    assert.ifError(result.error);
    return { ...result, results, events: readFileSync(events, 'utf8').trim().split('\n').map(line => JSON.parse(line)) };
}

function assertIsolatedBatches(events) {
    const active = new Set();
    for (const { kind, index } of events) {
        if (kind === 'run' || kind === 'rm') assert.equal(active.size, 0, `${kind} for model ${index} overlapped a browser probe`);
        if (kind === 'probe-start') active.add(index);
        if (kind === 'probe-end') active.delete(index);
    }
    assert.equal(active.size, 0);
}

for (const concurrency of [1, 2, 3]) {
    test(`keeps startup and teardown outside every browser probe (concurrency ${concurrency})`, t => {
        const result = runHarness(t, { concurrency });
        assert.equal(result.status, 0, result.stdout + result.stderr);
        assertIsolatedBatches(result.events);
        assert.equal(result.events.filter(event => event.kind === 'probe-end').length, models.length);
        for (let start = 0; start < models.length; start += concurrency) {
            const batch = result.events.filter(event => event.index >= start && event.index < start + concurrency);
            assert.ok(batch.findIndex(event => event.kind === 'probe-start') > batch.findLastIndex(event => event.kind === 'ready'));
        }
        for (const [index, model] of models.entries()) assert.equal(readFileSync(join(result.results, model, 'stage.log'), 'utf8'), `Stage logs for model ${index}\n`);
    });
}

for (const probeCode of [1, 2]) {
    test(`preserves probe exit ${probeCode}, runs every sibling and cleans up after all probes`, t => {
        const result = runHarness(t, { probeCode });
        assert.equal(result.status, probeCode, result.stdout + result.stderr);
        assertIsolatedBatches(result.events);
        assert.equal(result.events.filter(event => event.kind === 'probe-end').length, models.length);
        assert.equal(result.events.filter(event => event.kind === 'rm').length, models.length);
    });
}

test('keeps startup failure blocked while probing ready siblings and later batches', t => {
    const result = runHarness(t, { startupFailure: 1, probeCode: 1 });
    assert.equal(result.status, 2, result.stdout + result.stderr);
    assertIsolatedBatches(result.events);
    assert.deepEqual(result.events.filter(event => event.kind === 'probe-end').map(event => event.index).sort(), [0, 2, 3]);
    assert.equal(result.events.filter(event => event.kind === 'rm').length, models.length);
    assert.match(readFileSync(join(result.results, 'hr/scenario.log'), 'utf8'), /could not run: the Stage reported unsupported for hr/);
});
