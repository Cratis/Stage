// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Every deployment target a <see cref="SceneApplication"/> ships, each fully resolved - the result of one
/// build invocation. Cratis/Stage#39.
/// </summary>
/// <remarks>
/// <para>
/// This is the answer to #39's open question about multi-target builds: <strong>one invocation produces one
/// plan per target</strong>, never one build per target. The targets come out of a single compile of a single
/// <c>.play</c> source set, so planning them together is the only way they are guaranteed to describe the same
/// application - a build per target recompiles the same source once per target and can only compare their
/// outcomes after the fact, if at all. It also makes "this template is unplaceable on the phone but fine on the
/// web" a question the build can answer, and lets a build fail once, coherently, with every target's findings
/// in hand.
/// </para>
/// <para>
/// Producing the plans together does not force emitting the artifacts together: <see cref="Targets"/> is a
/// list, and each entry is independently emittable, so a caller wanting a web bundle now and a mobile package
/// later filters this rather than re-running the build. That is why the split is here, at emission, and not
/// back at compilation.
/// </para>
/// </remarks>
/// <param name="Targets">One <see cref="RenderPlan"/> per <c>ui profile</c> per platform, in declaration order.</param>
/// <param name="Findings">
/// What could not be resolved about the application as a whole, independently of any one target. A target's
/// own findings stay on that target rather than being flattened in here, so every finding keeps the target it
/// belongs to.
/// </param>
public record ApplicationRenderPlan(IReadOnlyList<RenderPlan> Targets, IReadOnlyList<RenderFinding> Findings)
{
    /// <summary>
    /// Gets a value indicating whether every target resolved completely and the application itself raised nothing.
    /// </summary>
    public bool IsComplete => Findings.Count == 0 && Targets.All(target => target.IsComplete);
}
