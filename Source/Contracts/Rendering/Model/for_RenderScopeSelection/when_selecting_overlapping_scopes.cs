// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection;

public class when_selecting_overlapping_scopes : given.a_scoped_render_request
{
    IReadOnlyList<string> _selection = null!;
    void Because() => _selection = Selected(With(ArtifactRenderScopeKind.Module, Shipping.Id) with
    {
        AdditionalScopes =
        [
            new(ArtifactRenderScopeKind.Feature, _savings.Id),
            new(ArtifactRenderScopeKind.Slice, _savings.Slices[0].Id),
            new(ArtifactRenderScopeKind.Module, Shipping.Id)
        ]
    });
    [Fact] void should_union_the_scopes_in_declaration_order() => _selection.ShouldEqual("Save", "Send");
    [Fact] void should_not_duplicate_slices() => _selection.Distinct().Count().ShouldEqual(_selection.Count);
}
