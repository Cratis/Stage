// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_freezing_v1 : for_TemplatePreparer.given.a_kit
{
    [Fact]
    public void should_lock_the_surface_digest()
    {
        using var stream = typeof(TemplateProfile).Assembly.GetManifestResourceStream("Cratis.Stage.Rendering.Templates.v1.surface.sha256")!;
        using var reader = new StreamReader(stream);
        TemplateSurface.Digest.ShouldEqual(reader.ReadToEnd().Trim());
    }

    [Fact]
    public void should_keep_frozen_naming_vectors()
    {
        using var stream = typeof(TemplateProfile).Assembly.GetManifestResourceStream("Cratis.Stage.Rendering.Templates.v1.vectors.json")!;
        using var document = JsonDocument.Parse(stream);
        foreach (var row in document.RootElement.EnumerateArray())
        {
            foreach (var style in new[] { "pascal", "camel", "snake", "kebab", "constant" }) TemplateNames.Apply(style, row.GetProperty("input").GetString()!).ShouldEqual(row.GetProperty(style).GetString()!);
        }
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void should_be_culture_invariant(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            using var plan = TemplateProfile.Prepare(Kit("{{ string.upcase 'i' }}|{{ string.index_of 'FILE' 'i' }}|{{ [ 'ı', 'I', 'i', 'İ' ] | array.sort | array.join ',' }}|{{ lit.decimal model.number }}"), new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default);
            var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { number = 1.25m } }));
            result.Refusal.ShouldBeNull();
            ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual("I|-1|I,i,İ,ı|1.25");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void should_share_preparation_but_not_execution_state()
    {
        var prepared = TemplateProfile.Prepare(Kit("{{ x = model.name; x }}"), new given.a_language()).Prepared!;
        Parallel.For(0, 16, index =>
        {
            using var plan = prepared.BeginPlan(TemplateBudget.Default);
            var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { name = index } }));
            result.Refusal.ShouldBeNull();
            ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual(index.ToString(CultureInfo.InvariantCulture));
        });
    }
}
