// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ApplicationStructure, FeatureNode, Screen } from '@cratis/scene.model';
import { fetchModelReferences, modelReferenceMap } from './modelReferences';
import { useEffect, useState } from 'react';
import { useStageSource } from './stageSource';

/** Where a slice sits in the application, the way Arc's `<Slice>` and the Notes context name it. */
export interface StageSliceLocation {
    module: string;
    feature: string;
    slice: string;
}

/** What the host says about feedback: whether Notes is served, and the identifiers of the model's elements by qualified name. */
export interface StageNotesState {
    enabled: boolean;
    modelReferences: Record<string, string>;
}

const disabled: StageNotesState = { enabled: false, modelReferences: {} };

/** Reads the host's feedback state from the model references document; a host without one has Notes off. */
export async function readStageNotes(signal: AbortSignal, fetcher: typeof fetch = fetch): Promise<StageNotesState> {
    const references = await fetchModelReferences(signal, fetcher).catch(() => undefined);
    return references ? { enabled: references.notesEnabled === true, modelReferences: modelReferenceMap(references) } : disabled;
}

/** Reads what the host says about Notes once; a host that says nothing (a sandbox) has Notes off. */
export function useStageNotes(): StageNotesState {
    const source = useStageSource();
    const [state, setState] = useState<StageNotesState>(disabled);
    useEffect(() => {
        if (!source.notes) return undefined;
        const abort = new AbortController();
        source.notes(abort.signal).then(setState).catch(() => undefined);
        return () => abort.abort();
    }, [source]);

    return state;
}

function* slicesOf(module: string, features: FeatureNode[], parents: string[]): Generator<StageSliceLocation & { command?: string; qualified: string }> {
    for (const feature of features) {
        const path = [...parents, feature.name];
        for (const slice of feature.slices) {
            yield { module, feature: path.join('.'), slice: slice.name, command: slice.command, qualified: [module, ...path, slice.name].join('.') };
        }

        yield* slicesOf(module, feature.features, path);
    }
}

function everySlice(structure: ApplicationStructure | undefined) {
    return (structure?.modules ?? []).flatMap(module => [
        ...(module.slices ?? []).map(slice => ({ module: module.name, feature: '', slice: slice.name, command: slice.command, qualified: `${module.name}.${slice.name}` })),
        ...slicesOf(module.name, module.features, []),
    ]);
}

/** Every slice the application knows, for the slice search of the feedback dialog. */
export function knownSlicesOf(structure: ApplicationStructure | undefined): StageSliceLocation[] {
    return everySlice(structure).map(({ module, feature, slice }) => ({ module, feature, slice }));
}

/**
 * Where the named slice is: by its qualified name (`Module.Feature.Slice`), or by the command it carries. Undefined when the structure does not
 * contain it.
 */
export function locateSlice(structure: ApplicationStructure | undefined, nameOrCommand: string | undefined): StageSliceLocation | undefined {
    if (!nameOrCommand) return undefined;
    const found = everySlice(structure).find(slice => slice.qualified === nameOrCommand) ?? everySlice(structure).find(slice => slice.command === nameOrCommand);
    return found ? { module: found.module, feature: found.feature, slice: found.slice } : undefined;
}

/**
 * Where a screen is: its module and feature from the ancestry the model recorded, and the slice it shows when the structure has a slice of the
 * screen's name there. A screen without ancestry is somewhere in the application without a module or feature.
 */
export function locateScreen(structure: ApplicationStructure | undefined, screen: Screen | undefined): StageSliceLocation | undefined {
    if (!screen) return undefined;
    const [module = '', ...feature] = screen.path ?? [];
    const featureName = feature.join('.');
    const slice = everySlice(structure).find(candidate => candidate.module === module && candidate.feature === featureName && candidate.slice === screen.name);
    return { module, feature: featureName, slice: slice?.slice ?? screen.name };
}

/** What Arc's application context is told about the screen being shown: its identity and the module and feature ancestry it sits in. */
export function screenContribution(screen: Screen | undefined): { id: string; name: string; path: string } | undefined {
    return screen ? { id: screen.name, name: screen.name, path: (screen.path ?? []).join('/') } : undefined;
}
