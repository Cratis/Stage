// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class a_source_line_mapped_plan
{
    internal const string Root = "Model";
    internal const string Document = "Billing.play";

    internal static ArtifactRenderPlan Plan(LoadedSemanticModel loaded, bool mapped = true, string application = "InvoiceApp", string project = "Invoices")
    {
        var model = loaded.Model;
        var profile = CratisRendering.CreateProfile(loaded.Model.Application.Name, new(application, project));
        if (mapped)
        {
            var documents = loaded.ImplementationRequirements.Select(requirement => requirement.Source.Span.Document).Distinct();
            profile = CratisRendering.WithSourceLineMapping(profile, Root, documents.Select(document => KeyValuePair.Create(document, Document)));
        }

        return PlanWith(loaded, profile);
    }

    internal static ArtifactRenderPlan PlanWith(LoadedSemanticModel loaded, ArtifactRenderProfile profile)
    {
        var model = loaded.Model;
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }

    internal static IEnumerable<SemanticImplementationRequirement> InlineRequirements(LoadedSemanticModel loaded) =>
        loaded.ImplementationRequirements.Where(requirement => requirement.File is null && !requirement.BodyLines.IsDefaultOrEmpty);

    internal static string[] Lines(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Split('\n');

    internal static string Directive(PlannedArtifact artifact, SemanticImplementationRequirement requirement) =>
        $"#line {requirement.BodyLines[0].Line} \"{string.Concat(Enumerable.Repeat("../", artifact.RelativePath.Count(character => character == '/')))}{Root}/{Document}\"";

    // Finds the artifact and line index of a requirement's directive.
    internal static (PlannedArtifact Artifact, string[] Lines, int Index) Find(ArtifactRenderPlan plan, SemanticImplementationRequirement requirement)
    {
        foreach (var artifact in plan.Artifacts.Where(candidate => candidate.RelativePath.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var lines = Lines(artifact);
            var index = Array.IndexOf(lines, Directive(artifact, requirement));
            if (index >= 0) return (artifact, lines, index);
        }

        throw new Xunit.Sdk.XunitException($"No #line directive for requirement {requirement.RequirementId}: {string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => diagnostic.Message))}");
    }

    internal static void AssertMapped(ArtifactRenderPlan plan, LoadedSemanticModel loaded)
    {
        var inline = InlineRequirements(loaded).ToArray();
        Assert.NotEmpty(inline);
        foreach (var requirement in inline)
        {
            var (_, lines, index) = Find(plan, requirement);
            Assert.Equal("#line default", lines[index + 1 + requirement.BodyLines.Length]);
            Assert.DoesNotContain(lines.Skip(index + 1).Take(requirement.BodyLines.Length), line => line.StartsWith("#line", StringComparison.Ordinal));
        }
    }

    internal static string PerformerSource(string body) => $"""
        module People
          feature Directory
            slice StateView People
              readmodel PersonSummary
                personId Uuid
                name String
              query FindPeople => PersonSummary[]
                filter name String
                performer
                  ```csharp
                  {body}
                  ```
        """;
}
#endif
