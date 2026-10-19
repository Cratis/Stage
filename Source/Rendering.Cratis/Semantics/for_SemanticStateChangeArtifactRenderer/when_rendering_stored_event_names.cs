// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer;

public class when_rendering_stored_event_names : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _code = null!;

    void Because()
    {
        var model = SemanticModelSerializer.Deserialize(StoredEventNamesCorpus.V16.EsmBytes.AsSpan());
        _plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("StoredEventNames", "StoredEventNames"));
        _code = string.Join('\n', _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_admit_the_mixed_model() => _plan.Success.ShouldBeTrue();
    [Fact] void should_pin_the_renamed_event() => _code.ShouldContain("[global::Cratis.Chronicle.Events.EventTypeAttribute(\"Original\")]\npublic record Renamed(");
    [Fact] void should_keep_the_unpinned_event_argument_free() => _code.ShouldContain("[global::Cratis.Chronicle.Events.EventTypeAttribute]\npublic record Recorded(");
}
