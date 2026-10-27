// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Kits;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_lowering : for_TemplatePreparer.given.a_kit
{
    internal const string Operators = """{"operators":[{"operator":"+","position":"binary","token":"+","precedence":10,"associativity":"left"},{"operator":"*","position":"binary","token":"*","precedence":20,"associativity":"left"}]}""";

    [Fact]
    public void should_preserve_precedence()
    {
        var kit = Kit("{{ expr.lower model.expression }}");
        kit = kit with
        {
            Manifest = kit.Manifest with { Tables = [new("operators", "operators.json")] },
            Files = new KitFiles(kit.Files.Paths.Select(path => new KeyValuePair<string, ImmutableArray<byte>>(path, kit.Files[path])).Append(new KeyValuePair<string, ImmutableArray<byte>>("operators.json", [.. Encoding.UTF8.GetBytes(Operators)])))
        };
        var preparation = TemplateProfile.Prepare(kit, new given.a_language());
        preparation.Diagnostics.ShouldBeEmpty();
        using var plan = preparation.Prepared!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("""{"model":{"expression":{"kind":"binary","operator":"*","left":{"kind":"binary","operator":"+","left":{"kind":"literal","value":1},"right":{"kind":"literal","value":2}},"right":{"kind":"literal","value":3}}}}""");
        var result = plan.Render("main.scriban", json.RootElement);
        result.Refusal.ShouldBeNull();
        ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual("(1 + 2) * 3");
    }

    [Fact]
    public void should_degrade_unsupported_semantics_without_refusing()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ expr.lower model.expression }}"), new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("""{"model":{"expression":{"kind":"call"}}}""");
        var result = plan.Render("main.scriban", json.RootElement);
        result.Refusal.ShouldBeNull();
        result.Output!.Gaps[0].Role.ShouldEqual(GapRole.ThrowingOperationBody);
        ((TemplateTextSegment)result.Output.Segments[0]).Text.ShouldEqual("FAIL[ThrowingOperationBody:STAGE-TPL-EXPR-001]");
    }
}
