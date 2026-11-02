// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Lifecycles;

namespace Cratis.Stage.Semantics;

internal sealed partial class SemanticRuntime : ISemanticWorldFacts
{
    /// <inheritdoc/>
    public async Task<ImmutableArray<SemanticFact>> Facts()
    {
        await _gate.WaitAsync();
        try
        {
            InitializeWorld();
            return FaultReason is null ? _world.Facts : [];
        }
        finally
        {
            _gate.Release();
        }
    }
}
