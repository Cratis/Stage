// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from '@cratis/stage.runtime/primereact';
import 'primeicons/primeicons.css';
import '@cratis/scene.primereact/primeReactTheme.css';

/** Wraps only an application whose ui profile lists PrimeReact, with its theme and build-time license. */
export function withPrimeReact(application: ReactNode) {
    // An absent key keeps PrimeReact's unlicensed presentation, as in a generated application.
    const primeUiLicense = import.meta.env.STAGE_PRIMEUI_LICENSE || '';
    return <PrimeReactProvider license={primeUiLicense} theme={stageTheme}>{application}</PrimeReactProvider>;
}
