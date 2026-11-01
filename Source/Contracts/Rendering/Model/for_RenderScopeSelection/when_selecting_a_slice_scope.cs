// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection;

public class when_selecting_a_slice_scope : given.a_scoped_render_request
{
    [Fact] void should_select_exactly_that_slice() => Selected(With(ArtifactRenderScopeKind.Slice, _accounts.Slices[1].Id)).ShouldEqual("Close");
}
