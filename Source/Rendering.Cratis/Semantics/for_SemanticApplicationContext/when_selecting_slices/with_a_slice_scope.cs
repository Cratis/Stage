// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext.when_selecting_slices;

public class with_a_slice_scope : given.a_scoped_render_request
{
    IReadOnlyList<string> _selection = null!;

    void Because() => _selection = Selected(With(ArtifactRenderScopeKind.Slice, _accounts.Slices[1].Id));

    [Fact] void should_select_only_the_requested_slice() => _selection.ShouldEqual("Close");
}
