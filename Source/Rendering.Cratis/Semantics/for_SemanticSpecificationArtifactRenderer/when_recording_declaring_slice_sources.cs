// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer;

public class when_recording_declaring_slice_sources
{
    const string QuerySource = """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId ProjectId identifier
                name String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name String
            slice StateView Lookup
              readmodel ProjectSummary
                projectId ProjectId
                name String
              query ById => ProjectSummary?
                by projectId ProjectId
              projection Summary => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
              specification LookingUp
                given ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Stage"
                then query ById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Stage"
        """;

    public static TheoryData<string, string> Models => new()
    {
        { when_rendering_scoped_projections.ScopedSource, "526f862d0de7d59386ad6883ec31b8d85e4310db7ea9df373f1e61cc74ac16f6" },
        { QuerySource, "97fa5b90bc77390ecf540ba65874c98123adbe826a4a5b0e4f6d3da30dce1ae1" },
        { DeniedQuerySource(), "6b699c9814d8c5a3b9a4ecae23981a886408cf9cb116e0b6af194c3b8b0d4ec3" },
        { a_reaction_specification.ProductionSource, "c865e969c777d53bfe134ef4be802d4aa0f312f186d4b7d2d8eadfca85ee5e19" },
        { a_redelivery_specification.RedeliverySource, "185e704ba171ebc3b8c9dd58816684bd055d6a540506d3c61a97096a7ad8be9b" },
        { a_specification_case_table.Source, "b95e27b77aba9efbb451ad8ed303e48633ce0497586aa51a5d3746e5815df7ef" }
    };

    public static IEnumerable<object[]> Sources => Models.Select(row => new[] { row[0] });

    [Theory]
    [MemberData(nameof(Models))]
    public void should_preserve_the_artifact_content_hashes(string source, string digest)
    {
        var context = Context(source);
        var files = context.Specifications.Values.SelectMany(specification => SemanticSpecificationArtifactRenderer.Render(specification, context));
        var hashes = files.OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .Select(file => $"{file.RelativePath} {Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content)))}");
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', hashes)))).ShouldEqual(digest);
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void should_name_only_the_specification_and_its_declaring_slice(string source)
    {
        var context = Context(source);
        foreach (var specification in context.Specifications.Values)
        {
            var files = SemanticSpecificationArtifactRenderer.Render(specification, context);
            files.ShouldNotBeEmpty();
            foreach (var file in files)
            {
                file.Sources.ShouldEqual(new[] { specification.Id, context.DeclaringSlice(specification.Id).Slice.Id });
            }
        }
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void should_plan_identical_sources_and_digest_repeatedly(string source)
    {
        var context = Context(source);
        var first = new CratisArtifactRenderPlanner().Plan(context.Request);
        var second = new CratisArtifactRenderPlanner().Plan(context.Request);
        Assert.True(first.Success, string.Join(Environment.NewLine, first.Diagnostics));
        second.Digest.ShouldEqual(first.Digest);
        second.Artifacts.SelectMany(artifact => artifact.Sources).ShouldEqual(first.Artifacts.SelectMany(artifact => artifact.Sources));
    }

    static SemanticApplicationContext Context(string source)
    {
        var model = invoice_model.Compile(source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile(model.Application.Name, options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));

        return new(request, options);
    }

    static string DeniedQuerySource() => "policy Clerks\n  require authenticated\n" + QuerySource
        .Replace("query ById => ProjectSummary?\n", "query ById => ProjectSummary?\n        authorize Clerks\n", StringComparison.Ordinal)
        .Replace("      specification LookingUp\n", "      specification LookingUp\n        given caller\n", StringComparison.Ordinal)
        .Replace("        given ProjectRegistered\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Stage\"\n", string.Empty, StringComparison.Ordinal)
        .Replace("          result\n            projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n            name = \"Stage\"", "        then denied", StringComparison.Ordinal);
}
