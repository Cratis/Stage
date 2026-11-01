// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext;

public class when_indexing_duplicate_identities : a_register_project_render_request
{
    Exception? _error;
    SemanticId _duplicateId;

    void Establish()
    {
        // Fault injection: duplicate a compiled event without changing the valid source fixture.
        var @event = _registerProject.Events.Single();
        _duplicateId = @event.Id;
        typeof(SemanticSlice).GetField("<Events>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_registerProject, ImmutableArray.Create(@event, @event));
    }

    void Because() => _error = Catch.Exception(() => _ = new SemanticApplicationContext(_request, _options));

    [Fact] void should_throw_the_typed_refusal() => _error.ShouldBeOfExactType<UnsupportedSemanticRendering>();
    [Fact] void should_use_the_existing_semantic_rendering_code() => ((UnsupportedSemanticRendering)_error!).Code.ShouldEqual("STAGE-ESM-012");
    [Fact] void should_name_the_declaration_kind_and_duplicate_id() => _error!.Message.ShouldContain($"duplicate event identity '{_duplicateId}'");
}
