// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_a_stream_is_named_All : a_routed_command
{
    void Establish() => _source = _source with { Streams = [_source.Streams[0] with { StreamKind = "All" }] };
    void Because()
    {
        ReplaceSlice();
        _plan = invoice_model.Plan(_model);
    }

    [Fact] void should_refuse_the_chronicle_sentinel() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-030"]);
    [Fact] void should_not_publish_a_partial_application() => _plan.Artifacts.ShouldBeEmpty();
}
