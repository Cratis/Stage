// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import type { Layout, SceneElement, Screen, ScreenTemplate } from '@cratis/scene.model';
import { coreComponents, SceneElementView } from '@cratis/scene.react';
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
    if (scene.screens.length === 0) return <main className='stage-message'><h1>No screens modeled yet</h1><p>Add a screen to the Screenplay to see its frontend.</p></main>;

    const screen = scene.screens.find(candidate => candidate.name === selectedScreen) ?? scene.screens[0];

    return (
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
    );
}

function SceneContent({ element }: { element: SceneElement }) {
    return <SceneElementView element={element} registry={coreComponents} resolveBinding={() => undefined} />;
}
