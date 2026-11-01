// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_rendering_query_performers_with_compiler_contexts : given.a_source_plan
{
    const string InlineBody = "return readModels.OrderBy(request => request.RequestedAt);";
    const string FileBody = "return readModels.Where(request => request.RequestedAt >= since);";
    LoadedSemanticModel _performers = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_root, "performers.play"), $$"""
            concept Reason : String
              validate
                rule ValidReason
                  ```csharp
                  return true;
                  ```
                rule AcceptedReason
                  ```csharp
                  return true;
                  ```
            module Projects
              feature Registration
                slice StateView ChangeRequests
                  readmodel ChangeRequest
                    talkId Uuid key
                    reason Reason
                    requestedAt DateTime
                  query AllChangeRequests => ChangeRequest[]
                    performer
                      ```csharp
                      {{InlineBody}}
                      ```
                  query ChangeRequestsSince => ChangeRequest[]
                    filter since DateTime
                    performer
                      file callforpapers/changes/ChangeRequestsSince.cs
            """);
        var directory = Path.Combine(_root, "callforpapers", "changes");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "ChangeRequestsSince.cs"), FileBody);
    }

    async Task Because()
    {
        _performers = await LoadPerformers();
        _result = CratisRendering.PlanFrom(_performers, _featureSelection, _planOptions);
    }

    [Fact]
    void should_render_both_performers_with_their_bodies()
    {
        _performers.TypedContextDescriptors.ShouldNotBeEmpty();
        Assert.Contains(_performers.TypedContextDescriptors, descriptor => descriptor.Role == SemanticImplementationRole.RulePredicate);
        Assert.NotNull(_result.Plan);
        Assert.True(_result.Plan.Outcome == ArtifactRenderOutcome.Complete,
            $"Outcome: {_result.Plan.Outcome}; {string.Join("; ", _result.Plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"))}");
        var content = Content(_result);
        content.ShouldContain(InlineBody);
        content.ShouldContain(FileBody);
    }

    [Fact]
    void should_use_query_shapes_without_requiring_performer_descriptors()
    {
        // Screenplay Documentation/screenplay/context.md: "Query performers, reactions and constraints have no descriptors yet".
        var requirements = _performers.ImplementationRequirements.Where(requirement => requirement.Role == SemanticImplementationRole.QueryPerformer).ToArray();
        requirements.Length.ShouldEqual(2);
        foreach (var requirement in requirements)
        {
            requirement.QueryShape.ShouldNotBeNull();
            _performers.TypedContextDescriptors.Where(descriptor => descriptor.RequirementId == requirement.RequirementId).ShouldBeEmpty();
        }
    }

    [Fact]
    void should_still_refuse_a_missing_descriptor_for_a_descriptor_backed_role()
    {
        var descriptor = _performers.TypedContextDescriptors.First(candidate => candidate.Role == SemanticImplementationRole.RulePredicate);
        var missing = _performers with
        {
            TypedContextDescriptors = [.. _performers.TypedContextDescriptors.Where(candidate => candidate.RequirementId != descriptor.RequirementId)]
        };
        missing.TypedContextDescriptors.ShouldNotBeEmpty();
        var result = CratisRendering.PlanFrom(missing, _featureSelection, _planOptions);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains(descriptor.RequirementId, StringComparison.Ordinal));
    }

    [Fact]
    async Task should_preserve_observable_performer_stubs_with_diagnostics()
    {
        var path = Path.Combine(_root, "performers.play");
        var source = await File.ReadAllTextAsync(path);
        await File.WriteAllTextAsync(path, source.Replace("=> ChangeRequest[]", "=> observable ChangeRequest[]", StringComparison.Ordinal));
        var loaded = await LoadPerformers();
        var result = CratisRendering.PlanFrom(loaded, _featureSelection, _planOptions);
        Assert.NotNull(result.Plan);
        result.Plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
        Assert.Contains(result.Plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006" && diagnostic.Message.Contains("cannot be observed", StringComparison.Ordinal));
        Assert.All(result.Plan.ImplementationGaps, gap => gap.ResultVersion.ShouldEqual(2u));
        var content = Content(result);
        content.ShouldContain("throw new NotImplementedException(");
        content.ShouldNotContain(InlineBody);
        content.ShouldNotContain(FileBody);
    }

    async Task<LoadedSemanticModel> LoadPerformers()
    {
        var loaded = await SemanticModelLoader.LoadAsync(_root, [], null, _planOptions.ApplicationName);
        Assert.True(loaded.Success, string.Join("; ", loaded.Diagnostics));
        return loaded.Loaded!;
    }

    static string Content(CratisPlanResult result) =>
        string.Join(Environment.NewLine, result.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
