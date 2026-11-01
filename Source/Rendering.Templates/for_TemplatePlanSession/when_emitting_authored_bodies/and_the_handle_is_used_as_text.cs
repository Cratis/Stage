// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePlanSession.when_emitting_authored_bodies;

public class and_the_handle_is_used_as_text : for_TemplatePreparer.given.a_kit
{
    readonly List<string> _codes = [];

    void Because()
    {
        foreach (var text in new[] { "{{ (impl.body 'r1') + 'text' }}", "{{ capture x; impl.body 'r1'; end }}", "{{ x = impl.body 'r1'; x.name }}", "{{ (impl.body 'r1') | string.upcase }}", "{{ [impl.body 'r1'] }}" })
        {
            using var session = TemplatePreparer.Prepare(Kit(text)).Value!.BeginPlan(TemplateBudget.Default);
            _codes.Add(session.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { } })).Refusal?.Code ?? "none");
        }
    }

    [Fact] void should_refuse_every_coercion_as_a_symbol_misuse() => _codes.ShouldEachConformTo(code => code == TemplateCodes.Symbol);
}
