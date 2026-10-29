// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection;

public class when_selecting_the_application_scope : given.a_scoped_render_request
{
    [Fact] void should_select_every_slice_in_declaration_order() => Selected(_request).ShouldEqual("Open", "Close", "Save", "Send");
}
