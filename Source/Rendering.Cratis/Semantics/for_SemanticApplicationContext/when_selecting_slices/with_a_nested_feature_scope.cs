// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext.when_selecting_slices;

public class with_a_nested_feature_scope : given.a_scoped_render_request
{
    IReadOnlyList<string> _selection = null!;

    void Because() => _selection = Selected(With(ArtifactRenderScopeKind.Feature, _savings.Id));

    [Fact] void should_select_only_the_nested_feature_slices() => _selection.ShouldEqual("Save");
}
