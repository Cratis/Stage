// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { primeReactComponents, primeReactPackageManifest } from '@cratis/scene.primereact';
import type { StageLibrary } from './blueprint';

/**
 * The entry a profile that lists `PrimeReact` opts in to (`@cratis/stage.runtime/primereact`): the PrimeReact widgets and what
 * the package declares. The host imports it and registers it with `registerStageLibrary`; the runtime never does.
 */
export const primeReactLibrary: StageLibrary = {
    components: primeReactComponents,
    declared: primeReactPackageManifest.components,
};

export { primeReactComponents, primeReactPackageManifest };
export { stageTheme, stageDarkSelector } from './stageTheme';
