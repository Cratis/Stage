// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useRef, useState } from 'react';

/**
 * The query results published to the binding scope, by scope. A publisher passes an identity token with what it registers; a withdrawal that
 * carries a token only removes the registration that token still owns, so an older publisher's cleanup never deletes a newer publisher's result
 * on the same scope. A withdrawal without a token removes the registration unconditionally.
 */
export function useQueryRegistry<TState>() {
    const [queries, setQueries] = useState<Record<string, TState>>({});
    const owners = useRef(new Map<string, symbol>());

    const registerQueryResult = useCallback((scope: string, state: TState | undefined, owner?: symbol) => {
        if (state) {
            if (owner) owners.current.set(scope, owner);
            else owners.current.delete(scope);
            setQueries(current => ({ ...current, [scope]: state }));
            return;
        }

        if (owner && owners.current.get(scope) !== owner) return;
        owners.current.delete(scope);
        setQueries(current => {
            if (!(scope in current)) return current;
            const { [scope]: _, ...remaining } = current;
            return remaining;
        });
    }, []);

    return { queries, registerQueryResult };
}
