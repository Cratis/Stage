// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_reaction_observer_filters
{
    [Theory]
    [InlineData("Account", false)]
    [InlineData("Account.Notes", true)]
    public void should_compile_the_stored_source_and_optional_stream_filter(string filter, bool withStream)
    {
        var plan = Plan(a_filtered_reaction.FilteredSource(filter));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = Files(plan);
        Assert.Empty(RenderedOutput.Errors(files));
        var type = RenderedOutput.Load(files).GetType("Filters.Work.Processing.FollowUp.Worker")!;
        if (withStream)
        {
            var attribute = Assert.Single(type.GetCustomAttributes(false), attribute => attribute.GetType().IsGenericType);
            Assert.Equal(typeof(FromEventSourceAttribute<>), attribute.GetType().GetGenericTypeDefinition());
            Assert.Equal("stored-notes", attribute.GetType().GetProperty("Stream")!.GetValue(attribute));
        }
        else
        {
            Assert.Equal("stored-account", Assert.Single(type.GetCustomAttributes(typeof(EventSourceTypeAttribute), false).Cast<EventSourceTypeAttribute>()).EventSourceType.Value);
        }
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "GeneratedEventSources/StreamIds.cs");
    }

    [Theory]
    [InlineData("Account")]
    [InlineData("Account.Notes")]
    public void should_include_a_source_referenced_only_by_the_reaction_filter(string filter)
    {
        var model = an_automation.Compile(a_filtered_reaction.FilteredSource(filter));
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single() with { Specifications = [] };
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice] }] }]
        });
        var plan = Plan(model, scoped: true);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "EventSources/AccountEventSource.cs");
        Assert.Empty(RenderedOutput.Errors(Files(plan)));
    }

    [Theory]
    [InlineData("Account", "Notes")]
    [InlineData("Account.Notes", "Notes")]
    [InlineData(null, "Notes")]
    public void should_include_filter_and_append_sources_in_a_slice_scoped_plan(string? filter, string stream)
    {
        var source = a_filtered_reaction.FilteredSource(filter ?? "Account", stream);
        if (filter is null) source = source.Replace("        from Account\n", string.Empty, StringComparison.Ordinal);
        var plan = Plan(source, scoped: true);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "EventSources/AccountEventSource.cs");
    }

    // Screenplay rejects the reserved source name Default before Stage receives an ESM.
    [Theory]
    [InlineData("stored-notes", "All")]
    public void should_refuse_reserved_stored_names_without_artifacts(string stored, string reserved)
    {
        var model = an_automation.Compile(a_filtered_reaction.FilteredSource());
        var sources = model.Application.EventSources.Select(source => source with
        {
            SourceKind = source.SourceKind == stored ? reserved : source.SourceKind,
            Streams = [.. source.Streams.Select(stream => stream with { StreamKind = stream.StreamKind == stored ? reserved : stream.StreamKind })]
        });
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { EventSources = [.. sources] });
        var plan = Plan(model);
        given.rendering_refusals.Rejected(plan, "STAGE-ESM-030");
    }

    static ArtifactRenderPlan Plan(string source, bool scoped = false)
    {
        return Plan(an_automation.Compile(source), scoped);
    }

    static ArtifactRenderPlan Plan(ExecutableSemanticModel model, bool scoped = false)
    {
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        var scope = scoped ? new ArtifactRenderScope(ArtifactRenderScopeKind.Slice, slice.Id) : new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id);

        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, scope, new("Filters", "Filters"));
    }

    static RenderedFile[] Files(ArtifactRenderPlan plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
#endif
