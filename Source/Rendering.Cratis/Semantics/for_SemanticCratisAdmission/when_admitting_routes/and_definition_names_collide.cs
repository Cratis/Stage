// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_definition_names_collide : a_routed_command
{
    void Establish()
    {
        ReplaceSlice();
        var second = _source with { Id = SemanticId.Parse($"sem1:{new string('a', 64)}"), Name = "AccountEventSource", SourceKind = "Other", Streams = [] };
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { EventSources = [_source, second] });
    }
    void Because() => _plan = invoice_model.Plan(_model);

    [Fact] void should_refuse_unstable_definition_names() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-012"]);
    [Fact] void should_not_suffix_the_second_definition() => _plan.Artifacts.ShouldBeEmpty();
}
