// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { UiProfile } from '@cratis/scene.model';
import { listsPrimeReact, registerStageLibrary } from '@cratis/stage.runtime';

/** Loads the live host's PrimeReact library, provider, theme and license only when the scene opts in, and registers the library with the runtime. */
export async function loadProfileProvider(profiles: UiProfile[] | undefined) {
    if (!listsPrimeReact(profiles)) return undefined;
    const [{ primeReactLibrary }, provider] = await Promise.all([import('@cratis/stage.runtime/primereact'), import('./primeReactProvider')]);
    registerStageLibrary('PrimeReact', primeReactLibrary);
    return provider;
}
