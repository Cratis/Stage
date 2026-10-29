// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection;

public class when_selecting_a_nested_feature_scope : given.a_scoped_render_request
{
    [Fact] void should_select_the_descendants_of_the_outer_feature() => Selected(With(ArtifactRenderScopeKind.Feature, _accounts.Id)).ShouldEqual("Open", "Close", "Save");
    [Fact] void should_select_only_the_inner_feature_slices() => Selected(With(ArtifactRenderScopeKind.Feature, _savings.Id)).ShouldEqual("Save");
}
