// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copies the extension protocol next to the package, so the SDK loads the same files every other SDK builds from.
import { cpSync, rmSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const source = fileURLToPath(new URL('../../Protocol/cratis', import.meta.url));
const target = fileURLToPath(new URL('../proto/cratis', import.meta.url));
rmSync(target, { recursive: true, force: true });
cpSync(source, target, { recursive: true });
