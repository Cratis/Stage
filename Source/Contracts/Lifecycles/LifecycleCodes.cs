// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Lifecycles;

/// <summary>
/// The diagnostic codes for state machines and workflows Stage cannot execute or render.
/// </summary>
public static class LifecycleCodes
{
    /// <summary>A workflow declares more than one trigger, or none.</summary>
    public const string SingleTrigger = "STAGE-LIFECYCLE-001";

    /// <summary>A node kind the engine does not know.</summary>
    public const string UnknownNode = "STAGE-LIFECYCLE-002";

    /// <summary>A foreach inside the branches of another foreach.</summary>
    public const string NestedForEach = "STAGE-LIFECYCLE-003";

    /// <summary>A foreach that is not gathered by exactly one join.</summary>
    public const string ForEachJoin = "STAGE-LIFECYCLE-004";

    /// <summary>A join that does not gather the branches of a foreach.</summary>
    public const string UnpairedJoin = "STAGE-LIFECYCLE-005";

    /// <summary>An edge leaves by an outlet the engine node does not have.</summary>
    public const string UnknownOutlet = "STAGE-LIFECYCLE-006";

    /// <summary>A scheduled trigger the runtime does not drive.</summary>
    public const string ScheduledTrigger = "STAGE-LIFECYCLE-007";

    /// <summary>A transition guard the renderer cannot express over the followed read model.</summary>
    public const string UnrenderedGuard = "STAGE-LIFECYCLE-008";

    /// <summary>A state machine whose state after a restart cannot be derived from its read model alone.</summary>
    public const string RestartState = "STAGE-LIFECYCLE-009";

    /// <summary>A workflow action that invokes an ability, which the engine's graph does not run yet (Cratis/Stage#1118).</summary>
    public const string AbilityNode = "STAGE-LIFECYCLE-010";
}
