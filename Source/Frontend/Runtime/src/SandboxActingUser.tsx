// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { useStageSource } from './stageSource';
import { changeSandboxActingUser, type SandboxActingUserState } from './sandboxActingUser';

/** A real caller selection for a live development sandbox, not a design-time persona preview. */
export function SandboxActingUser() {
    const source = useStageSource();
    const [state, setState] = useState<SandboxActingUserState>();
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    useEffect(() => {
        const controller = new AbortController();
        if (source.actingUser && source.selectActingUser) {
            void source.actingUser(controller.signal).then(value => {
                if (!controller.signal.aborted) setState(value);
            }).catch(() => { /* A host without the sandbox surface offers no switcher. */ });
        }
        return () => controller.abort();
    }, [source]);

    if (!state?.enabled || state.users.length === 0) return null;
    const select = async (name: string) => {
        setBusy(true);
        setError('');
        try {
            await changeSandboxActingUser(source, name || null, () => window.location.reload());
        } catch {
            setError('The acting user could not be changed. Try again.');
            setBusy(false);
        }
    };
    return <label className="stage-acting-user">
        <span>Acting as </span>
        <select aria-label="Acting as" value={state.current ?? ''} disabled={busy} onChange={event => { void select(event.target.value); }}>
            <option value="">No acting user</option>
            {state.users.map(user => <option key={user.name} value={user.name}>{user.displayName || user.name}{user.personas.length ? ` (${user.personas.join(', ')})` : ''}</option>)}
        </select>
        {error && <span role="alert">{error}</span>}
    </label>;
}
