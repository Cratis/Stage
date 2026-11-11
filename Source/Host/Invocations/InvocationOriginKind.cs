// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The kind of behavior an ability invocation came from. It decides how the outcome returns to the origin.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Invocations.InvocationOriginKind")]
public enum InvocationOriginKind
{
    /// <summary>
    /// A reaction branch: the branch settles (<c lang="text">on returned</c>, <c lang="text">on &lt;Outcome&gt;</c>, <c lang="text">on failed</c>) with the rest of its atomic batch.
    /// </summary>
    ReactionBranch = 0,

    /// <summary>
    /// A workflow action: the workflow takes the outlet the outcome names.
    /// </summary>
    WorkflowAction = 1,

    /// <summary>
    /// A state machine entering a state: the effect completes, and a failure stops the entry for the operator.
    /// </summary>
    StateMachineEntry = 2,

    /// <summary>
    /// A person's interaction with an extension component on a screen. The browser waits for the outcome on its request, so settling takes it nowhere else.
    /// </summary>
    ScreenInteraction = 3
}
