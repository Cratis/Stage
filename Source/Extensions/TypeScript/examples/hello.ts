// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A minimal extension backend: one safe ability that greets, with a public setting for the greeting.
// Stage connects to it at the address its configuration points to (Stage:Extensions:Backends:Hello:Endpoint).
import { Extension, outcome } from '../src/index.js';

const hello = Extension.create('hello', '1.0.0')
    .setting('greeting')
    .ability('Greet', 'safe', async call => {
        const greeting = (await call.host.setting('greeting')) ?? 'Hello';
        const { name } = JSON.parse(call.input) as { name: string };
        return outcome.returned(JSON.stringify({ message: `${greeting}, ${name}!` }));
    });

const { port } = await hello.listen('0.0.0.0:50051');
console.log(`hello extension listening on ${port}`);
