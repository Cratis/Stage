// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// Only the slice's first projection is rendered, so the dropped one's query used to be part of the union
/// guarding the read model that survived — and an unguarded query anywhere in the slice published it to
/// everyone. A query guards the read model its return type names and no other.
/// </summary>
public class when_another_read_model_has_an_unguarded_query : a_slice_with_two_read_models
{
    RenderedFile _file = null!;
    StateViewSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateViewSliceRenderer();

    void Because() => _file = _renderer.Render(_dashboardSlice, _applicationSet, "CratisApp");

    [Fact] void should_render_the_first_projections_read_model() => _file.Content.ShouldContain("public record InvoiceSummary(");
    [Fact] void should_keep_it_guarded_by_its_own_querys_policy() => _file.Content.ShouldContain("[Roles(\"Accountant\")]");
    [Fact] void should_not_publish_it_to_everyone() => _file.Content.Contains("[AllowAnonymous]").ShouldBeFalse();
}
