// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_rendered_specifications_are_routed : Specification
{
    Contracts.Rendering.ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(when_planning_event_source_routes.Source + """

              specification Depositing
                when Deposit
                  accountId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  month = 10
                  amount = 5
                then Deposited
                  amount = 5
        """));

    [Fact] void should_admit_without_an_explicit_route_assertion() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_the_command_specification() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("when_depositing.cs", StringComparison.Ordinal)).ShouldBeTrue();
}
