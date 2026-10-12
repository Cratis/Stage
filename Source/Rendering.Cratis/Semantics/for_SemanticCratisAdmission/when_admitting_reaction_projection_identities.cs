// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_reaction_projection_identities : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    const string ProjectionSource = """
        concept WorkId : String
        module Work
          feature Processing
            slice StateChange Start
              command StartWork
                id WorkId identifier
                otherId WorkId
                produces event Started
                  for id
                  id WorkId = id
                  otherId WorkId = otherId
            slice Automation FollowUp
              command CompleteWork
                id WorkId identifier
                produces event Completed
                  for id
                  id WorkId = id
              reaction CompleteFromEvent
                when Started
                  produces Completed
                    id = id
            slice StateView Lookup
              readmodel Summary
                id WorkId
              query ById => Summary?
                by id WorkId
              projection SummaryProjection => Summary
                from Completed key id
                  id = id
        """;

    [Theory]
    [InlineData("")]
    [InlineData(" for otherId")]
    [InlineData(" for \"another\"")]
    public void should_refuse_a_reaction_destination_not_proven_equal_to_the_projection_key(string destination)
    {
        SetContext(Compile(WithDestination(destination)));
        Plan();
        _plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-009"]);
        _plan.Artifacts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_admit_and_compile_a_matching_destination_for_every_producer(bool reactionOnly)
    {
        SetContext(Compile(WithDestination(" for id")));
        if (reactionOnly) ReplaceAutomation(_located.Slice with { Commands = [] });
        Plan();
        _plan.Diagnostics.ShouldBeEmpty();
        _plan.Artifacts.ShouldNotBeEmpty();
        RenderedOutput.Errors(Files()).ShouldBeEmpty();
    }

    [Fact]
    public void should_admit_a_literal_destination_matching_the_payload_key()
    {
        SetContext(Compile(WithDestination(" for \"same\"").Replace("            id = id", "            id = \"same\"", StringComparison.Ordinal)));
        Plan();
        _plan.Diagnostics.ShouldBeEmpty();
        RenderedOutput.Errors(Files()).ShouldBeEmpty();
    }

    [Fact]
    public void should_check_reaction_producers_outside_the_selected_projection_slice()
    {
        SetContext(Compile(ProjectionSource));
        var lookup = _context.SelectedSlices().Single(slice => slice.Slice.Name == "Lookup");
        _plan = CratisRendering.Plan(_model, _context.Request.ExecutionPlan, new(ArtifactRenderScopeKind.Slice, lookup.Slice.Id), new("Reactions", "Reactions"));
        _plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-009"]);
        _plan.Artifacts.ShouldBeEmpty();
    }

    static string WithDestination(string destination)
    {
        if (destination.Length == 0) return ProjectionSource;

        return ProjectionSource.Replace("          produces Completed\n", $"          produces Completed\n           {destination}\n", StringComparison.Ordinal);
    }

    RenderedFile[] Files() => [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
