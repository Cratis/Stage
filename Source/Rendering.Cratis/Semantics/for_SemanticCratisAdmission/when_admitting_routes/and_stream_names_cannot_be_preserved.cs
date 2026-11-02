// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_stream_names_cannot_be_preserved : a_routed_command
{
    [Theory]
    [InlineData("AccountEventSource")]
    [InlineData("Equals")]
    [InlineData("GetType")]
    public void should_refuse_instead_of_renaming_the_constant(string name)
    {
        _source = _source with { Streams = [_source.Streams[0] with { Name = name }] };
        ReplaceSlice();
        var plan = invoice_model.Plan(_model);
        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == _source.Streams[0].Id);
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_refuse_duplicate_member_names()
    {
        var stream = _source.Streams[0];
        var source = _source with { Streams = [stream, stream with { Id = SemanticId.Parse($"sem1:{new string('a', 64)}"), StreamKind = "other" }] };
        var slice = _slice with { Commands = [_command] };
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with
        {
            EventSources = [source],
            Modules = [module with { Features = [feature with { Slices = [slice] }] }]
        }));
    }
}
