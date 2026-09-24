// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState, type ReactNode } from 'react';
import type { Layout, SceneElement, Screen, ScreenTemplate } from '@cratis/scene.model';
import type { CommandOutcome, InteractionFinding } from '@cratis/scene.engine';
import { evaluateFlowArrangement } from '@cratis/scene.engine';
import { InteractionScope, SceneElementView, createBrowserDispatcher } from '@cratis/scene.react';
import { useStageRoutes } from './stageRoutes';
import { stageComponents } from './stageComponents';
import { useSizeClass } from './useSizeClass';
import { FlowArrangementView } from './FlowArrangementView';
import './app.css';

function isFlowArrangement(arrangement: unknown): arrangement is Parameters<typeof evaluateFlowArrangement>[0] {
    return !!arrangement && typeof arrangement === 'object' && 'root' in arrangement;
}

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
            <StageShell scene={scene} screen={screen} selectedScreen={screen.name} onSelectScreen={name => { setSelectedScreen(name); setActivity(''); }} activity={activity} />
        </InteractionScope>
    );
}

function SceneContent({ element }: { element: SceneElement }) {
    return <SceneElementView element={element} registry={stageComponents} resolveBinding={() => undefined} />;
}

interface StageShellProps {
    scene: StageSceneApplication;
    screen: Screen;
    selectedScreen: string;
    onSelectScreen: (name: string) => void;
    activity: string;
}

/**
 * Renders a screen inside its application `Layout`, and its own content inside its `ScreenTemplate` when
 * it has one - both positioned by the arrangement each one actually declares, via the shared engine, not
 * flat-stacked regardless of what the document said. A layout or template without a `flow` arrangement
 * (none declared, or a `freeform` one - not yet rendered here) falls back to a plain stack, which is what
 * every screen rendered as before this.
 */
function StageShell({ scene, screen, selectedScreen, onSelectScreen, activity }: StageShellProps) {
    const sizeClass = useSizeClass();
    const layout = useMemo(() => scene.layouts.find(candidate => candidate.name === screen.layout), [scene.layouts, screen.layout]);
    const template = useMemo(
        () => (screen.screenTemplate ? scene.screenTemplates.find(candidate => candidate.name === screen.screenTemplate) : undefined),
        [scene.screenTemplates, screen.screenTemplate],
    );

    const contentSlots = useMemo(() => {
        const slots: Record<string, ReactNode> = {};
        for (const [slotName, elements] of Object.entries(screen.slotContent)) {
            slots[slotName] = elements.map(element => <SceneContent key={element.id} element={element} />);
        }
        return slots;
    }, [screen.slotContent]);

    const content = template?.arrangement && isFlowArrangement(template.arrangement)
        ? <FlowArrangementView node={evaluateFlowArrangement(template.arrangement, sizeClass)} slots={contentSlots} />
        : (
            <div className='stage-slots-fallback'>
                {Object.entries(contentSlots).map(([slotName, node]) => (
                    <section className='stage-slot' data-slot={slotName} key={slotName}>{node}</section>
                ))}
            </div>
        );

    const shellSlots: Record<string, ReactNode> = {
        topbar: <StageTopbar />,
        sidebar: <StageSidebar screens={scene.screens} selectedScreen={selectedScreen} onSelectScreen={onSelectScreen} />,
        content: <main className='stage-screen' data-screen={screen.name}>{content}{activity && <p className='stage-activity' role='status'>{activity}</p>}</main>,
        footer: null,
    };

    if (layout?.arrangement && isFlowArrangement(layout.arrangement)) {
        return <div className='stage-application'><FlowArrangementView node={evaluateFlowArrangement(layout.arrangement, sizeClass)} slots={shellSlots} /></div>;
    }

    return (
        <div className='stage-application'>
            <StageTopbar />
            <div className='stage-body'>
                <StageSidebar screens={scene.screens} selectedScreen={selectedScreen} onSelectScreen={onSelectScreen} />
                {shellSlots.content}
            </div>
        </div>
    );
}

function StageTopbar() {
    return (
        <header className='stage-header'>
            <strong>Cratis Stage</strong>
        </header>
    );
}

function StageSidebar({ screens, selectedScreen, onSelectScreen }: { screens: Screen[]; selectedScreen: string; onSelectScreen: (name: string) => void }) {
    return (
        <nav className='stage-sidebar' aria-label='Modeled screens'>
            {screens.map(candidate => (
                <button
                    key={candidate.name}
                    type='button'
                    className={candidate.name === selectedScreen ? 'selected' : ''}
                    onClick={() => onSelectScreen(candidate.name)}>
                    {candidate.name}
                </button>
            ))}
        </nav>
    );
}
