// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_numbered_enumeration_guards : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _composition = null!;
    string _implicitDocument = null!;

    void Because()
    {
        var source = given.an_enumeration_model.Source
            .Replace("  draft\n  issued", "  draft = 1\n  issued = 0", StringComparison.Ordinal) + "\n" + """
                slice StateView List
                  readmodel InvoiceRow
                    id InvoiceId
                    status Status
                  query Invoices => InvoiceRow[]
                  projection InvoiceRows => InvoiceRow
                    from InvoiceIssued key $eventSourceId
                      id = $eventSourceId
                      status = status
                  screen Invoices
                    data InvoiceRow[] via query Invoices
                    action "Issue"
                      when item.status == "draft" execute IssueInvoice
                      otherwise hidden
                    on click
                      when item.status == "draft"
                        execute IssueInvoice
                      otherwise
                        notify info "Not draft"
            """;
        (_plan, _composition) = Render(source);
        var (implicitPlan, _) = Render(source.Replace("  draft = 1\n  issued = 0", "  draft\n  issued", StringComparison.Ordinal));
        implicitPlan.Success.ShouldBeTrue();
        _implicitDocument = Encoding.UTF8.GetString(implicitPlan.Artifacts.Single(artifact => artifact.RelativePath == StageSceneDocument.RelativePath).Bytes.AsSpan());
    }

    static (ArtifactRenderPlan Plan, string Composition) Render(string source)
    {
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var scene = new ScreenplaySceneVisitor().Visit(compilation.Value!);
        scene.RuntimeIssues.ShouldBeEmpty();
        var model = given.invoice_model.Compile(source);
        var profile = CratisRendering.CreateProfile(model.Application.Name, new("Invoices", "Invoices"), scene);
        var input = profile.Inputs.Single(input => CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact) && artifact!.RelativePath == SceneCompositionInput.RelativePath);
        CratisArtifactRenderInput.TryCreateArtifact(input, out var composition).ShouldBeTrue();
        var plan = new CratisArtifactRenderPlanner().Plan(new(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        return (plan, Encoding.UTF8.GetString(composition!.Bytes.AsSpan()));
    }

    [Fact] void should_be_publishable() => _plan.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_guard_identity_in_the_composed_scene() => AssertGuards(_composition);
    [Fact] void should_preserve_guard_identity_in_the_planned_runtime_document() => AssertGuards(Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == StageSceneDocument.RelativePath).Bytes.AsSpan()));

    [Fact]
    void should_omit_numbers_for_implicitly_numbered_guards()
    {
        var comparisons = Descendants(JsonNode.Parse(_implicitDocument)!).OfType<JsonObject>()
            .Where(node => node["kind"]?.ToString() == "comparison").ToArray();
        comparisons.Length.ShouldEqual(3);
        foreach (var comparison in comparisons)
        {
            comparison["enumeration"]!.ToJsonString().ShouldEqual("[\"draft\",\"issued\"]");
            comparison.ContainsKey("enumerationNumbers").ShouldBeFalse();
        }
    }

    static void AssertGuards(string json)
    {
        var comparisons = Descendants(JsonNode.Parse(json)!).OfType<JsonObject>()
            .Where(node => node["kind"]?.ToString() == "comparison").ToArray();
        comparisons.Length.ShouldEqual(3);
        foreach (var comparison in comparisons)
        {
            comparison["enumeration"]!.ToJsonString().ShouldEqual("[\"draft\",\"issued\"]");
            comparison["enumerationNumbers"]!.ToJsonString().ShouldEqual("[1,0]");
            SceneGuards.CanEvaluate((object)comparison).ShouldBeTrue();
        }
    }

    static IEnumerable<JsonNode> Descendants(JsonNode node)
    {
        yield return node;
        var children = node switch
        {
            JsonObject value => value.Select(pair => pair.Value),
            JsonArray value => value.AsEnumerable(),
            _ => []
        };
        foreach (var child in children.OfType<JsonNode>())
        {
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
