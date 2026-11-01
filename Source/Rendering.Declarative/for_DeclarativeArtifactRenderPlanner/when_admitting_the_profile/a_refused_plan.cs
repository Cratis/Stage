// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_admitting_the_profile;

/// <summary>
/// Base for admission refusals: zero artifacts and one error.
/// </summary>
public abstract class a_refused_plan : given.a_declarative_planner
{
    protected abstract string ExpectedCode { get; }

    private protected Contracts.Rendering.ArtifactRenderPlan _plan = null!;
}
