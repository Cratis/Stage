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
    public void should_freeze_the_complete_api_helpers_builtins_and_defaults()
    {
        using var reader = new StreamReader(typeof(TemplateProfile).Assembly.GetManifestResourceStream("Cratis.Stage.Rendering.Templates.v1.surface.sha256")!);
        TemplateSurface.Digest.ShouldEqual(reader.ReadToEnd().Trim());
    }

    [Fact]
    public void should_keep_the_reference_identical_to_the_catalogue()
    {
        using var reader = new StreamReader(typeof(TemplateProfile).Assembly.GetManifestResourceStream("template-profile-v1.md")!);
        reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal).ShouldEqual(HelperCatalogue.Reference);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void should_obey_versioned_vectors_in_any_host_culture(string culture)
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            using var stream = typeof(TemplateProfile).Assembly.GetManifestResourceStream("Cratis.Stage.Rendering.Templates.v1.vectors.json")!;
            using var vectors = JsonDocument.Parse(stream);
            foreach (var vector in vectors.RootElement.EnumerateArray()) Check(vector);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }

    static void Check(JsonElement vector)
    {
        var kit = Kit(vector.GetProperty("template").GetString()!);
        if (vector.TryGetProperty("operators", out _)) kit = when_lowering.WithOperators(kit);
        var prepared = TemplateProfile.Prepare(kit, new given.a_language());
        if (vector.TryGetProperty("prepareRefusal", out var prepareCode))
        {
            prepared.Diagnostics[0].Code.ShouldEqual(prepareCode.GetString());
            return;
        }

        prepared.Diagnostics.ShouldBeEmpty();
        using var plan = prepared.Templates!.BeginPlan(TemplateBudget.Default);
        var rendered = plan.Render("main.scriban", vector.GetProperty("scope"));
        if (vector.TryGetProperty("refusal", out var refusal))
        {
            rendered.Refusal!.Code.ShouldEqual(refusal.GetString());
            rendered.Output.ShouldBeNull();
            plan.IsRefused.ShouldBeTrue();
            return;
        }

        rendered.Refusal.ShouldBeNull();
        if (vector.TryGetProperty("text", out var expectedText)) string.Concat(rendered.Output!.Segments.Cast<TemplateTextSegment>().Select(segment => segment.Text)).ShouldEqual(expectedText.GetString());
        if (vector.TryGetProperty("gap", out var gap)) rendered.Output!.Gaps[0].Code.ShouldEqual(gap.GetString());
        if (vector.TryGetProperty("segments", out var expectedSegments))
        {
            var actual = rendered.Output!.Segments.Select(segment => segment switch
            {
                TemplateTextSegment text => "text:" + text.Text,
                TemplateSymbolSegment symbol => symbol.Kind + ":" + (symbol.Symbol is null ? string.Empty : symbol.Symbol.Namespace + "." + symbol.Symbol.Name),
                _ => throw new InvalidOperationException("Unknown segment.")
            });
            actual.ShouldEqual(expectedSegments.EnumerateArray().Select(value => value.GetString()));
        }
    }
}
