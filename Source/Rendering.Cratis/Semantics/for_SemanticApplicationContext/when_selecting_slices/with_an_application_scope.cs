// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext.when_selecting_slices;

public class with_an_application_scope : given.a_scoped_render_request
{
    IReadOnlyList<string> _selection = null!;

    void Because() => _selection = Selected(_request);

    [Fact] void should_select_all_slices_in_model_order() => _selection.ShouldEqual("Open", "Close", "Save", "Lend", "Send");
}
