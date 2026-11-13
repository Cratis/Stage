// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Lifecycles;

/// <summary>
/// An ability a state machine invokes on entering a state.
/// </summary>
/// <param name="Machine">The state machine.</param>
/// <param name="State">The state it entered.</param>
/// <param name="Entry">The 1-based position of the entry among what entering the state does.</param>
/// <param name="Effect">The state entry that invokes the ability.</param>
/// <param name="Key">The key of the instance that entered the state.</param>
/// <param name="Causation">Identifies the entering, so delivering it again invokes the ability once.</param>
public sealed record StateEntryInvocation(SemanticStateMachine Machine, string State, int Entry, SemanticStateEntry Effect, SemanticValue Key, string Causation)
{
    /// <summary>
    /// Gets the address of the entry within the model, as the invocation's origin names it.
    /// </summary>
    public string Address => $"{Machine.Name}/{State}/entering {Entry}";
}
