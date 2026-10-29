// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext.when_selecting_slices;

public class with_a_feature_scope : given.a_scoped_render_request
{
    IReadOnlyList<string> _selection = null!;

    void Because() => _selection = Selected(With(ArtifactRenderScopeKind.Feature, _accounts.Id));

    [Fact] void should_select_the_feature_and_its_descendants_in_model_order() => _selection.ShouldEqual("Open", "Close", "Save");
}
