// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_using_language_helpers : for_TemplatePreparer.given.a_kit
{
    [Theory]
    [InlineData("HTTPServer2_value", "http_server_2_value", "HttpServer2Value", "httpServer2Value")]
    [InlineData(" déjà__Vu ", "déjà_vu", "DéjàVu", "déjàVu")]
    [InlineData("a---B", "a_b", "AB", "aB")]
    public void should_apply_frozen_naming_vectors(string source, string snake, string pascal, string camel)
    {
        TemplateNames.Apply("snake", source).ShouldEqual(snake);
        TemplateNames.Apply("pascal", source).ShouldEqual(pascal);
        TemplateNames.Apply("camel", source).ShouldEqual(camel);
    }

    [Fact]
    public void should_use_the_public_profile_and_language_seam()
    {
        var prepared = TemplateProfile.Prepare(Kit("{{ lit.boolean model.flag }} {{ lit['null'] }} {{ name.constant 'HTTPServer' }}"), new given.a_language());
        prepared.Diagnostics.ShouldBeEmpty();
        using var plan = prepared.Prepared!.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { flag = true } }));
        result.Refusal.ShouldBeNull();
        ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual("yes nil HTTP_SERVER");
    }
}
