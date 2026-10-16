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
import { StagePackagesFailed, loadStagePackages } from './stagePackages';
import { registerSharedModules } from './sharedModules';
import { stageTheme } from './stageTheme';
import { Browser } from './read-models/Browser';
import { isBrowserPath } from './read-models/path';

// PrimeReact 11 verifies a license key at runtime and otherwise renders a permanent "Invalid PrimeUI
// License" banner over the application. The key itself is the PRIMEUI_LICENSE GitHub secret, the same one
// Direct and Studio already use, remapped to STAGE_PRIMEUI_LICENSE by vite.config.ts's envPrefix - see the
// Dockerfile's frontend stage and publish.yml for where it is threaded through the build. Absent (a local
// build with nothing configured) this is just an empty string, and PrimeReact falls back to its own
// unlicensed presentation rather than failing to render.
const primeUiLicense = import.meta.env.STAGE_PRIMEUI_LICENSE || '';

const root = document.getElementById('root')!;

async function start() {
    // The Stage's own React is what an approved custom package loaded below renders with: register it for the
    // import map's shim modules before any package is imported.
    registerSharedModules();

    // Custom packages the Stage was handed (a package set and the browser build of each package) load before the first
    // render. One that does not load stops the Stage with the reason, rather than rendering a profile without it.
    const approved = await loadStagePackages().catch(reason => reason as Error);
    if (approved instanceof Error) {
        root.setAttribute('role', 'alert');
        root.style.whiteSpace = 'pre-wrap';
        root.textContent = approved instanceof StagePackagesFailed ? approved.message : `Approved packages could not be loaded: ${approved.message}`;
        return;
    }

    createRoot(root).render(
        <StrictMode>
            <PrimeReactProvider license={primeUiLicense} theme={stageTheme}>
                {isBrowserPath(window.location.pathname) ? (
                    <Browser />
                ) : (
                    <CratisComponentsProvider>
                        {/*
                          The runtime entry of the Cratis components package - its components, descriptors and manifest,
                          without designers, property editors or design-time actions - is what a scene naming
                          Cratis.Components renders with, live exactly as in a generated application. Approved custom
                          packages add theirs beside it; the Stage's own never lose a name to them.
                        */}
                        <App components={{ ...approved.components, ...cratisComponentsRuntimePackage.components }} packageCatalog={approved.catalog} screenTemplates={approved.screenTemplates} dialogTemplates={approved.dialogTemplates} />
                    </CratisComponentsProvider>
                )}
            </PrimeReactProvider>
        </StrictMode>,
    );
}

void start();
