// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_a_response_field_named_like_its_record : Specification
{
    const string Source = """
        concept ProjectId : Uuid
        eventsource Project
          identifier ProjectId
        type Address
          street String
          city String
        module Projects
          feature Moving
            slice StateChange Move
              command Move
                projectId ProjectId identifier
                address Address
                produces event Moved
                  for projectId
                  address Address = address
                returns
                  projectId = projectId
                  moveResponse = address
        """;

    ArtifactRenderPlan _plan = null!;
    string Slice => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Move/Move.cs", StringComparison.Ordinal)).Bytes.AsSpan());

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(Source));

    [Fact] void should_render_the_collision() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_escape_the_response_record() => Slice.ShouldContain("record MoveResponse_(");
    [Fact] void should_keep_the_field_name() => Assert.Matches(@"MoveResponse_\([^)]*\bMoveResponse\)", Slice);
    [Fact] void should_report_the_escaped_name() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-050" && diagnostic.Message.Contains("'MoveResponse_'", StringComparison.Ordinal));
}
