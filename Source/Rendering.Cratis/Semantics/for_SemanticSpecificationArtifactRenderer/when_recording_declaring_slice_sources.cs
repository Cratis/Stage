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
        { when_rendering_scoped_projections.ScopedSource, "1afbe1461d5cad81bbcecb298f7ef6ef58799ed7398355280bcac48ee91833e9" },
        { QuerySource, "593e95a70877d953aa788243bb021925a7ac07762a4fdaaabc89593f12a5d442" },
        { DeniedQuerySource(), "eba39398570adb22d2f94070518b196d8310234ea49f1e8020d0c8b85a3c07cb" },
        { a_reaction_specification.ProductionSource, "a126793aa88e2d18154a56d2f9b639bccbf39ad32c3c4ef68deb0d95ac22a116" },
        { a_redelivery_specification.RedeliverySource, "874c5afa44cd2e0597b983df8fca086b43797312d68967b39ae6651451f0e177" },
        { a_specification_case_table.Source, "eecd08b06586899a7a5f8abc44bcfbdc58471fd61140898e5b98072befdad986" }
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
            .Select(file => $"{file.RelativePath} {Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(LegacyLicenseHeader.Restore(file.RelativePath, file.Content))))}");
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
