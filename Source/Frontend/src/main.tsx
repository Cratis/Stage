// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { PrimeReactProvider } from '@primereact/core/config';
import 'primeicons/primeicons.css';
import '@cratis/scene.primereact/primeReactTheme.css';
import { CratisComponentsProvider } from '@cratis/components';
import { cratisComponentsRuntimePackage } from '@cratis/scene.components/runtime';
import { App } from './App';
import { stageTheme } from './stageTheme';

// PrimeReact 11 verifies a license key at runtime and otherwise renders a permanent "Invalid PrimeUI
// License" banner over the application. The key itself is the PRIMEUI_LICENSE GitHub secret, the same one
// Direct and Studio already use, remapped to STAGE_PRIMEUI_LICENSE by vite.config.ts's envPrefix - see the
// Dockerfile's frontend stage and publish.yml for where it is threaded through the build. Absent (a local
// build with nothing configured) this is just an empty string, and PrimeReact falls back to its own
// unlicensed presentation rather than failing to render.
const primeUiLicense = import.meta.env.STAGE_PRIMEUI_LICENSE || '';

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <PrimeReactProvider license={primeUiLicense} theme={stageTheme}>
            <CratisComponentsProvider>
                {/*
                  The runtime entry of the Cratis components package - its components, descriptors and manifest,
                  without designers, property editors or design-time actions - is what a scene naming
                  Cratis.Components renders with, live exactly as in a generated application.
                */}
                <App components={cratisComponentsRuntimePackage.components} />
            </CratisComponentsProvider>
        </PrimeReactProvider>
    </StrictMode>,
);
