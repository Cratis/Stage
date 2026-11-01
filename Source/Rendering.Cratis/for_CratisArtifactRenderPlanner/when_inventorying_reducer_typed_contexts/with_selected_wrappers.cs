// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_inventorying_reducer_typed_contexts;

public class with_selected_wrappers
{
    [Fact]
    public async Task should_plan_the_unchanged_min_a3_model_with_its_emitted_wrapper_inventoried()
    {
        var loaded = await reducer_context_model.LoadMinA3();
        var request = reducer_context_model.Request(loaded);
        var context = new SemanticApplicationContext(request, reducer_context_model.Options);
        AssertWrapperDeclarations(context);
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.CanPublishArtifacts, string.Join(Environment.NewLine, plan.Diagnostics));
        var descriptor = Assert.Single(SemanticReducerTypedContexts.Selected(context, context.SelectedSlices()));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith($"/{SemanticTypedContextRenderer.WrapperName(descriptor)}.cs", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_inventory_domain_relative_wrappers_for_nested_features_and_cross_slice_events()
    {
        var source = string.Join('\n', when_rendering_a_pure_reducer.Source.Split('\n').Select((line, index) => index < 2 ? line : "  " + line))
            .Replace("  feature Ordering\n", "  feature Commerce\n    feature Ordering\n", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var request = reducer_context_model.Request(loaded, domain: "Finance/Checkout");
        var context = new SemanticApplicationContext(request, reducer_context_model.Options);
        AssertWrapperDeclarations(context);
        Assert.All(SemanticReducerTypedContexts.Declarations(context, context.SelectedSlices()), declaration => Assert.Equal("Finance.Checkout.TypedContexts", declaration.Namespace));
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.CanPublishArtifacts, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(plan)));
    }

    [Fact]
    public async Task should_not_inventory_or_publish_unselected_reducer_descriptors()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source);
        var slice = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Commands.Length > 0);
        var request = reducer_context_model.Request(loaded, new(ArtifactRenderScopeKind.Slice, slice.Id));
        var context = new SemanticApplicationContext(request, reducer_context_model.Options);
        Assert.NotEmpty(request.TypedContextDescriptors);
        Assert.Empty(SemanticReducerTypedContexts.Declarations(context, context.SelectedSlices()));
        Assert.DoesNotContain(context.GeneratedTypes, type => type.Name.StartsWith("TypedContext_", StringComparison.Ordinal));
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.CanPublishArtifacts, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath.Contains("/TypedContext_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_deduplicate_the_same_reducer_pair_without_inventing_state_or_event_shapes()
    {
        var loaded = await reducer_context_model.LoadMinA3();
        var request = reducer_context_model.Request(loaded);
        var descriptor = Assert.Single(request.TypedContextDescriptors, descriptor => descriptor.Role == SemanticImplementationRole.ReducerTransition);
        var context = new SemanticApplicationContext(request with { TypedContextDescriptors = [descriptor, descriptor] }, reducer_context_model.Options);
        var declarations = SemanticReducerTypedContexts.Declarations(context, context.SelectedSlices()).ToArray();
        var declaration = Assert.Single(declarations);
        Assert.Equal(SemanticTypedContextRenderer.WrapperName(descriptor), declaration.Name);
        Assert.DoesNotContain(declarations, declaration => declaration.Name.EndsWith("Shape", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_retain_genuine_generated_declaration_collisions()
    {
        var context = new SemanticApplicationContext(reducer_context_model.Request(await reducer_context_model.LoadMinA3()), reducer_context_model.Options);
        var declarations = SemanticCratisAdmission.GeneratedDeclarations(context, context.SelectedSlices()).ToArray();
        var descriptor = Assert.Single(SemanticReducerTypedContexts.Selected(context, context.SelectedSlices()));
        var wrapper = Assert.Single(declarations, declaration => declaration.Name == SemanticTypedContextRenderer.WrapperName(descriptor));
        Assert.Contains(GeneratedTypeNames.Collisions([], declarations.Append(wrapper)), collision => collision.Name == wrapper.Name);
    }

    [Fact]
    public async Task should_keep_source_namespace_collision_refusals()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source.Replace("module Orders", "module TypedContexts", StringComparison.Ordinal));
        var plan = new CratisArtifactRenderPlanner().Plan(reducer_context_model.Request(loaded));
        Assert.False(plan.CanPublishArtifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("reserved reducer", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_still_refuse_unknown_typed_context_names()
    {
        var context = new SemanticApplicationContext(reducer_context_model.Request(await reducer_context_model.LoadMinA3()), reducer_context_model.Options);
        var builder = new CSharpCodeBuilder(GeneratedProjects.Application, context.GeneratedTypes, hasScaffoldGlobalUsings: false)
            .Namespace("MinA3.Other")
            .Line("public class Probe : global::MinA3.TypedContexts.TypedContext_NotInventoried {}");
        Assert.Throws<CSharpImportRejected>(builder.ToString);
    }

    static void AssertWrapperDeclarations(SemanticApplicationContext context)
    {
        var descriptors = SemanticReducerTypedContexts.Selected(context, context.SelectedSlices()).ToArray();
        Assert.NotEmpty(descriptors);
        var inventory = SemanticCratisAdmission.GeneratedDeclarations(context, context.SelectedSlices()).ToArray();
        foreach (var descriptor in descriptors)
        {
            var name = SemanticTypedContextRenderer.WrapperName(descriptor);
            Assert.Contains(inventory, declaration => declaration.Namespace == string.Join('.', context.Domain.Append("TypedContexts")) && declaration.Name == name);
            Assert.Contains(context.GeneratedTypes, type => type.Namespace == context.TypedContextsNamespace && type.Name == name);
            var wrapper = SemanticTypedContextRenderer.Render(descriptor, context);
            Assert.Contains($"public record {name}(", wrapper.Content, StringComparison.Ordinal);
        }
    }
}
#endif
