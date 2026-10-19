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
        { when_rendering_scoped_projections.ScopedSource, "e5f1bb30f72bca24c534f617464c455709377733626682767915fe590c99333f" },
        { QuerySource, "cc622d2c2b7a6030342c3c823adba47c96f08f2504a545e1f035cbd0c15b7ce8" },
        { DeniedQuerySource(), "422877b8623baaed91b83953a94ecaf9974349a9cf75be9a23a1552ad2729b42" },
        { a_reaction_specification.ProductionSource, "c99cb46e0b410e2648760d34d223d49acd9019b16bcca4001bfa7c3cbb9338bd" },
        { a_redelivery_specification.RedeliverySource, "7f140178f427dd6ee77eafa6d36878ea0d5915eade1951810c94604b2f7e4e72" },
        { a_specification_case_table.Source, "8a601707cf7d2d1398524cce07a5eb2c4fa43675cd986afe71a001f836a9a8d0" }
    };

    public static TheoryData<string> Sources
    {
        get
        {
            var sources = new TheoryData<string>();
            foreach (var row in Models)
            {
                sources.Add((string)row[0]);
            }

            return sources;
        }
    }

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
