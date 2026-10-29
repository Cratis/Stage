// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePlanSession.when_emitting_authored_bodies;

public class and_the_requirement_id_is_invalid : for_TemplatePreparer.given.a_kit
{
    readonly List<bool> _refused = [];

    void Because()
    {
        foreach (var text in new[] { "{{ impl.body '' }}", "{{ impl.body model.number }}", "{{ impl.body null }}" })
        {
            using var session = TemplatePreparer.Prepare(Kit(text)).Value!.BeginPlan(TemplateBudget.Default);
            var result = session.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { number = 1 } }));
            _refused.Add(result.Refusal is not null && result.Output is null);
        }
    }

    [Fact] void should_refuse_each_one() => _refused.ShouldEachConformTo(refused => refused);
}
