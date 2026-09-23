// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import type { Layout, SceneElement, Screen, ScreenTemplate } from '@cratis/scene.model';
import type { CommandOutcome, InteractionFinding } from '@cratis/scene.engine';
import { InteractionScope, SceneElementView, createBrowserDispatcher } from '@cratis/scene.react';
import { useStageRoutes } from './stageRoutes';
import { stageComponents } from './stageComponents';
import './app.css';

export interface StageSceneApplication {
    layouts: Layout[];
    screenTemplates: ScreenTemplate[];
    screens: Screen[];
}

interface SceneCommandDetail {
    command: string;
}

interface SceneNavigateDetail {
    targetScreen: string;
}

const endpoint = 'stage/scene';

export function App() {
    const [scene, setScene] = useState<StageSceneApplication>();
    const routes = useStageRoutes();
    const [selectedScreen, setSelectedScreen] = useState('');
    const [error, setError] = useState('');
    const [activity, setActivity] = useState('');

    useEffect(() => {
        const abort = new AbortController();
        fetch(endpoint, { signal: abort.signal })
            .then(response => {
                if (!response.ok) throw new Error(`The Stage returned ${response.status}.`);
                return response.json() as Promise<StageSceneApplication>;
            })
            .then(application => {
                setScene(application);
                setSelectedScreen(application.screens[0]?.name ?? '');
            })
            .catch(reason => {
                if (!abort.signal.aborted) setError(reason instanceof Error ? reason.message : String(reason));
            });
        return () => abort.abort();
    }, []);

    useEffect(() => {
        const command = (event: Event) => {
            const detail = (event as CustomEvent<SceneCommandDetail>).detail;
            setActivity(`The modeled command “${detail.command}” is ready for input.`);
        };
        const navigate = (event: Event) => {
            const detail = (event as CustomEvent<SceneNavigateDetail>).detail;
            if (scene?.screens.some(screen => screen.name === detail.targetScreen)) {
                setSelectedScreen(detail.targetScreen);
                setActivity('');
            } else {
                setActivity(`The modeled screen “${detail.targetScreen}” is not available.`);
            }
        };

        globalThis.addEventListener('cratis.scene.command', command);
        globalThis.addEventListener('cratis.scene.navigate', navigate);
        return () => {
            globalThis.removeEventListener('cratis.scene.command', command);
            globalThis.removeEventListener('cratis.scene.navigate', navigate);
        };
    }, [scene]);

    if (error) return <main className='stage-message'><h1>Unable to render this Stage</h1><p>{error}</p></main>;
    if (!scene) return <main className='stage-message'><h1>Preparing the Stage</h1></main>;
    if (scene.screens.length === 0) {
        return (
            <main className='stage-message'>
                <h1>Nothing to show yet</h1>
                <p>This model has no slice carrying a command or a read model, so there is no screen to render.</p>
            </main>
        );
    }

    const screen = scene.screens.find(candidate => candidate.name === selectedScreen) ?? scene.screens[0];

    // Everything a document's interactions can do, pointed at the running application. The engine decides
    // what runs; this only says where a command goes and what a notification looks like.
    const dispatcher = createBrowserDispatcher({
        executeCommand: async (command, args): Promise<CommandOutcome> => {
            const route = routes?.commands[command];
            if (!route) {
                setActivity(`The modeled command “${command}” is not registered by this Stage.`);
                return { isSuccess: false, validationErrors: [] };
            }

            const response = await fetch(route, {
                method: 'POST',
                headers: { 'content-type': 'application/json' },
                body: JSON.stringify(args),
            });

            if (!response.ok) {
                setActivity(`“${command}” failed with ${response.status}.`);
                return { isSuccess: false, validationErrors: [] };
            }

            const result = await response.json() as { isSuccess?: boolean; validationResults?: { message: string; members: string[] }[] };

            // Arc answers a rejected command with 200 and the reasons, so the status alone does not say
            // whether it worked. Reading the body is what makes 'on failure' mean what the document says.
            const validationErrors = (result.validationResults ?? []).map(_ => ({ member: _.members[0] ?? '', message: _.message }));
            if (validationErrors.length > 0) setActivity(validationErrors.map(_ => _.message).join(' '));

            return { isSuccess: result.isSuccess !== false && validationErrors.length === 0, validationErrors };
        },
        navigate: screenName => {
            if (scene.screens.some(candidate => candidate.name === screenName)) {
                setSelectedScreen(screenName);
                setActivity('');
            } else {
                setActivity(`The modeled screen “${screenName}” is not available.`);
            }
        },
        notify: (level, message) => setActivity(`${level}: ${message}`),
        refreshQuery: async query => { globalThis.dispatchEvent(new CustomEvent('cratis.scene.refresh', { detail: { query } })); },
    });

    // A finding is what the engine could not do. Showing it is the whole difference between an interaction
    // that is broken and one that silently does nothing.
    const reportFindings = (findings: InteractionFinding[]) =>
        setActivity(findings.map(finding => finding.detail).join(' '));

    return (
        <InteractionScope dispatcher={dispatcher} context={{ resolve: () => undefined }} attachments={[]} onFindings={reportFindings}>
        <div className='stage-application'>
            <header className='stage-header'>
                <strong>Cratis Stage</strong>
                <nav aria-label='Modeled screens'>
                    {scene.screens.map(candidate => (
                        <button
                            key={candidate.name}
                            type='button'
                            className={candidate.name === screen.name ? 'selected' : ''}
                            onClick={() => { setSelectedScreen(candidate.name); setActivity(''); }}>
                            {candidate.name}
                        </button>
                    ))}
                </nav>
            </header>
            <main className='stage-screen' data-screen={screen.name}>
                {Object.entries(screen.slotContent).map(([slot, elements]) => (
                    <section className='stage-slot' data-slot={slot} key={slot}>
                        {elements.map(element => <SceneContent key={element.id} element={element} />)}
                    </section>
                ))}
                {activity && <p className='stage-activity' role='status'>{activity}</p>}
            </main>
        </div>
        </InteractionScope>
    );
}

function SceneContent({ element }: { element: SceneElement }) {
    return <SceneElementView element={element} registry={stageComponents} resolveBinding={() => undefined} />;
}
