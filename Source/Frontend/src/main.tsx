// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { CratisComponentsProvider } from '@cratis/components';
import { Arc } from '@cratis/arc.react';
import { notesDialogHeaderActions } from '@cratis/showrunner.notes.react';
import { cratisComponentsRuntimePackage } from '@cratis/scene.components/runtime';
import { App, StageSourceProvider, hostedBasePath, type StageRoutes } from '@cratis/stage.runtime';
import { StagePackagesFailed, loadStagePackages } from './stagePackages';
import { registerSharedModules } from './sharedModules';
import { loadProfileProvider } from './loadProfileProvider';
import { Browser } from './read-models/Browser';
import { isBrowserPath } from './read-models/path';
import { httpStageSource } from './httpStageSource';
import { registerStageCommands } from './host/commandRegistration';
import { registerStageQueries } from './host/queryRegistration';

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

    const abort = new AbortController();
    let routes: StageRoutes | undefined;
    try {
        if (!isBrowserPath(window.location.pathname)) {
            routes = await httpStageSource.routes(abort.signal);
            if (!routes) throw new Error('Stage routes could not be loaded.');
            registerStageCommands(routes);
            registerStageQueries(routes);
        }
    } catch (reason) {
        console.warn('Stage command routes could not be loaded; rendering without registered commands.', reason);
    }
    // Read once before rendering so the provider is ready before any opted-in widget mounts. Keep a failed
    // request for App to report through its existing scene-loading error UI.
    const sceneRequest = isBrowserPath(window.location.pathname) ? undefined : httpStageSource.scene(abort.signal);
    const scene = await sceneRequest?.catch(() => undefined);
    const source = { ...httpStageSource, scene: async (signal: AbortSignal) => sceneRequest ?? httpStageSource.scene(signal), routes: async () => routes };
    const primeReact = await loadProfileProvider(scene?.uiProfiles);

    const application = (<>
        {isBrowserPath(window.location.pathname) ? (
            <CratisComponentsProvider><Browser /></CratisComponentsProvider>
        ) : (
            <CratisComponentsProvider dialogHeaderActions={notesDialogHeaderActions}>
                {/*
                  The runtime entry of the Cratis components package - its components, descriptors and manifest,
                  without designers, property editors or design-time actions - is what a scene naming
                  Cratis.Components renders with, live exactly as in a generated application. Approved custom
                  packages add theirs beside it; the Stage's own never lose a name to them.
                */}
                {/*
                  Arc's own requests - its identity at /.cratis/me among them - use its API base path. Under a
                  reverse proxy prefix (Studio's Play at /api/play/{id}/) an empty one would reach the proxy's root.
                */}
                <Arc queryDirectMode sliceMarkers apiBasePath={hostedBasePath()}>
                    <StageSourceProvider source={source}>
                        <App components={{ ...approved.components, ...cratisComponentsRuntimePackage.components }} packageCatalog={approved.catalog} screenTemplates={approved.screenTemplates} dialogTemplates={approved.dialogTemplates} routing={hostedBasePath() === '' ? 'path' : 'hash'} />
                    </StageSourceProvider>
                </Arc>
            </CratisComponentsProvider>
        )}
    </>);
    createRoot(root).render(
        <StrictMode>
            {primeReact ? primeReact.withPrimeReact(application) : application}
        </StrictMode>,
    );
}

void start();
