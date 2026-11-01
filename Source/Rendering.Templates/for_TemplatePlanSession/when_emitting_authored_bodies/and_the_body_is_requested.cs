// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePlanSession.when_emitting_authored_bodies;

public class and_the_body_is_requested : for_TemplatePreparer.given.a_kit
{
    TemplateRenderResult _result = null!;

    void Because()
    {
        using var session = TemplatePreparer.Prepare(Kit("A{{ impl.body model.id }}B{{ x = impl.body 'r2'; if x != null; x; end }}")).Value!.BeginPlan(TemplateBudget.Default);
        _result = session.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { id = "r1" } }));
    }

    [Fact] void should_not_refuse() => _result.Refusal.ShouldBeNull();
    [Fact] void should_emit_structured_handles_in_order() => _result.Output!.Segments.AsEnumerable().ShouldEqual([new TemplateTextSegment("A"), new TemplateAuthoredSegment("r1"), new TemplateTextSegment("B"), new TemplateAuthoredSegment("r2")]);
}
