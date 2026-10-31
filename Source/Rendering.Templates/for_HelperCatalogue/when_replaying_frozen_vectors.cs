// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_HelperCatalogue;

public class when_replaying_frozen_vectors : for_TemplatePreparer.given.a_kit
{
    public static TheoryData<string, string> Vectors
    {
        get
        {
            var rows = new TheoryData<string, string>();
            foreach (var vector in HelperCatalogue.All.SelectMany(helper => helper.Vectors).DistinctBy(vector => vector.Id))
            {
                rows.Add(vector.Id, "en-US");
                rows.Add(vector.Id, "tr-TR");
            }
            return rows;
        }
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void should_preserve_frozen_bytes(string id, string culture)
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Check(FrozenHelperVectors.All.Single(vector => vector.Id == id));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }

    static void Check(HelperVector vector)
    {
        var kit = Kit(vector.Template);
        if (vector.Operators) kit = for_TemplateProfile.when_lowering.WithOperators(kit);
        var prepared = TemplateProfile.Prepare(kit, new for_TemplateProfile.given.a_language());
        if (vector.PrepareRefusal is not null)
        {
            prepared.Diagnostics.Single().Code.ShouldEqual(vector.PrepareRefusal);
            prepared.Templates.ShouldBeNull();
            return;
        }

        prepared.Diagnostics.ShouldBeEmpty();
        using var plan = (prepared.Templates ?? throw new InvalidOperationException("Missing prepared templates.")).BeginPlan(TemplateBudget.Default);
        var rendered = plan.Render("main.scriban", vector.Scope);
        if (vector.Refusal is not null)
        {
            (rendered.Refusal ?? throw new InvalidOperationException("Missing refusal.")).Code.ShouldEqual(vector.Refusal);
            rendered.Output.ShouldBeNull();
            plan.IsRefused.ShouldBeTrue();
            return;
        }

        rendered.Refusal.ShouldBeNull();
        CheckOutput(rendered.Output ?? throw new InvalidOperationException("Missing output."), vector);
    }

    static void CheckOutput(TemplateOutput output, HelperVector vector)
    {
        if (vector.Text is not null) string.Concat(output.Segments.Cast<TemplateTextSegment>().Select(segment => segment.Text)).ShouldEqual(vector.Text);
        if (vector.Gap is not null) output.Gaps.Single().Code.ShouldEqual(vector.Gap);
        if (!vector.Segments.IsDefault) output.Segments.Select(Describe).ShouldEqualInOrder([.. vector.Segments]);
    }

    static string Describe(TemplateSegment segment) => segment switch
    {
        TemplateTextSegment text => "text:" + text.Text,
        TemplateSymbolSegment symbol => symbol.Kind + ":" + (symbol.Symbol is null ? string.Empty : symbol.Symbol.Namespace + "." + symbol.Symbol.Name),
        TemplateAuthoredSegment authored => "Authored:" + authored.RequirementId,
        _ => throw new InvalidOperationException("Unknown segment.")
    };
}
