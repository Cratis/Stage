// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// Only the slice's first projection is rendered. Its query keeps its authorization on its generated method;
/// the dropped read model's unguarded query neither lands here nor changes that method's policy.
/// </summary>
public class when_another_read_model_has_an_unguarded_query : a_slice_with_two_read_models
{
    RenderedFile _file = null!;
    StateViewSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateViewSliceRenderer();

    void Because() => _file = _renderer.Render(_dashboardSlice, _applicationSet, "CratisApp");

    [Fact] void should_render_the_first_projections_read_model() => _file.Content.ShouldContain("public record InvoiceSummary(");
    [Fact] void should_guard_the_generated_method_with_its_own_querys_policy() =>
        _file.Content.ShouldContain("[Roles(\"Accountant\")]\n    public static async Task<InvoiceSummary?> GetInvoiceSummary");
    [Fact] void should_not_publish_it_to_everyone() => _file.Content.Contains("[AllowAnonymous]").ShouldBeFalse();
}
