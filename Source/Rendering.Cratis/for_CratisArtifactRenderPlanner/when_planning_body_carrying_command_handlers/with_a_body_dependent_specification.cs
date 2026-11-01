// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

public class with_a_body_dependent_specification : Specification
{
    internal const string Source = handler_model.Source + """

              specification NormalizingTheName
                when PlaceOrder
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "alice"
                then OrderPlaced
                  name = "ALICE"
        """;

    ArtifactRenderPlan _plan = null!;
    string _specification = null!;

    void Because()
    {
        _plan = handler_model.Plan(handler_model.Request(source: Source));
        _specification = string.Join('\n', _plan.Artifacts.Where(artifact => artifact.RelativePath.Contains("when_normalizing_the_name", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_admit_the_specification() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_skip_the_body_dependent_outcome() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011").ShouldBeFalse();
    [Fact] void should_execute_the_command() => _specification.ShouldContain("_scenario.Execute(new PlaceOrder(");
    [Fact] void should_assert_the_then_payload_instead_of_the_declarative_mapping() => _specification.ShouldContain("@event.Name == \"ALICE\"");
}
