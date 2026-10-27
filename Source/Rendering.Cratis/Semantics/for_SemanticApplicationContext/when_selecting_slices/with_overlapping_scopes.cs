// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext.when_selecting_slices;

public class with_overlapping_scopes : given.a_scoped_render_request
{
    IReadOnlyList<string> _selection = null!;

    void Because() => _selection = Selected(With(ArtifactRenderScopeKind.Module, _application.Modules[1].Id) with
    {
        AdditionalScopes =
        [
            new(ArtifactRenderScopeKind.Feature, _accounts.Id),
            new(ArtifactRenderScopeKind.Slice, _savings.Slices[0].Id),
            new(ArtifactRenderScopeKind.Module, _application.Modules[1].Id)
        ]
    });

    [Fact] void should_union_scopes_in_model_order_not_request_order() => _selection.ShouldEqual("Open", "Close", "Save", "Send");
    [Fact] void should_not_duplicate_slices() => _selection.Distinct().Count().ShouldEqual(_selection.Count);
}
