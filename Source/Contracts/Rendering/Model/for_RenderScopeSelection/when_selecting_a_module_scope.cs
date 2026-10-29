// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection;

public class when_selecting_a_module_scope : given.a_scoped_render_request
{
    [Fact] void should_select_only_the_slices_of_that_module() => Selected(With(ArtifactRenderScopeKind.Module, _banking.Id)).ShouldEqual("Open", "Close", "Save");
    [Fact] void should_select_the_other_module() => Selected(With(ArtifactRenderScopeKind.Module, Shipping.Id)).ShouldEqual("Send");
}
