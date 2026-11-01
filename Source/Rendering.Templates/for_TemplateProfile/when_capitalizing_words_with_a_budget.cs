// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_capitalizing_words_with_a_budget : for_TemplatePreparer.given.a_kit
{
    [Fact]
    public void should_meter_character_iteration_and_drop_partial_output()
    {
        using var plan = TemplateProfile.Prepare(Kit("before{{ words = string.capitalizewords model.text }}after"), new given.a_language()).Templates.BeginPlan(TemplateBudget.Default with { MaxSteps = 100 });
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { text = new string('a', 100) } }));
        result.Refusal.ShouldNotBeNull();
        result.Refusal.Code.ShouldEqual(TemplateCodes.Steps);
        result.Output.ShouldBeNull();
    }
}
