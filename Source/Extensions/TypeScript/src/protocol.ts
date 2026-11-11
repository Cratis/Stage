// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fileURLToPath } from 'node:url';
import * as grpc from '@grpc/grpc-js';
import * as protoLoader from '@grpc/proto-loader';

const root = fileURLToPath(new URL('../../proto', import.meta.url));
const files = ['common', 'backend', 'host'].map(name => `cratis/stage/extensions/v1/${name}.proto`);

const definition = protoLoader.loadSync(files, {
    includeDirs: [root],
    keepCase: false,
    longs: String,
    enums: String,
    defaults: false,
    oneofs: true
});

/** The loaded extension protocol, `cratis.stage.extensions.v1`. */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const protocol = (grpc.loadPackageDefinition(definition) as any).cratis.stage.extensions.v1;

/** The major and minor version of the protocol this SDK speaks. */
export const protocolVersion = { major: 1, minor: 0 };
