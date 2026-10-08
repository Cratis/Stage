// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_authoring_documentation_with_a_described_reaction : Specification
{
    SemanticCompilation _compilation = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _compilation = when_rendering_authoring_documentation.Compile(
        when_rendering_authoring_documentation.Source.Replace(
            "    slice StateView ProjectLookup",
            """
                  reaction RegistrationNotification
                    description "Notifies about registration"
                    when ProjectRegistered
                      description "Handles a registered project"
                slice StateView ProjectLookup
            """,
            StringComparison.Ordinal));

    void Because() => _plan = when_rendering_authoring_documentation.Plan(_compilation);

    [Fact] void should_admit_the_metadata_it_creates() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_still_plan_rendered_declarations() => _plan.Artifacts.ShouldNotBeEmpty();
}
