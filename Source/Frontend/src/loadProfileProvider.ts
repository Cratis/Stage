// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { UiProfile } from '@cratis/scene.model';
import { listsPrimeReact } from '@cratis/stage.runtime';

/** Loads the live host's PrimeReact provider, theme and license only when the scene opts in. */
export async function loadProfileProvider(profiles: UiProfile[] | undefined) {
    return listsPrimeReact(profiles) ? import('./primeReactProvider') : undefined;
}
