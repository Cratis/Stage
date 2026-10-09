// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policies;

public class with_a_scoped_use_site : Specification
{
    ArtifactRenderPlan _application = null!;
    ArtifactRenderPlan _scoped = null!;
    ExecutableSemanticModel _model = null!;
    Assembly _assembly = null!;
    IReadOnlyList<string> _warnings = [];

    void Establish()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source("return context.Identity.IsAuthenticated && context.Occurred.Year == 2027;"));
        _model = loaded.Model;
        _application = opaque_policy_model.Plan(loaded);
        var slice = _model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Kind == SemanticSliceKind.StateChange);
        _scoped = CratisRendering.Plan(
            loaded.Model,
            loaded.Plan,
            new(ArtifactRenderScopeKind.Slice, slice.Id),
            new("InvoiceApp", "Invoices"),
            loaded.ImplementationRequirements,
            loaded.ImplementationContents,
            loaded.AttachmentDiagnostics,
            loaded.TypedContextDescriptors);
    }

    void Because()
    {
        Assert.True(_scoped.Success, opaque_policy_model.Errors(_scoped));
        var registration = opaque_policy_model.Files(_application).Single(file => file.RelativePath == "GeneratedPolicyRegistration.cs");
        var files = opaque_policy_model.Files(_scoped).Append(registration).ToArray();
        _assembly = RenderedOutput.Load(files);
        _warnings = RenderedOutput.Warnings(files);
    }

    [Fact] void should_keep_every_policy_artifacts_bytes_and_sources() => _scoped.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/", StringComparison.Ordinal) || artifact.RelativePath.StartsWith("TypedContexts/", StringComparison.Ordinal)).All(artifact => _application.Artifacts.Any(expected => expected.RelativePath == artifact.RelativePath && expected.Bytes.SequenceEqual(artifact.Bytes) && expected.Sources.SequenceEqual(artifact.Sources))).ShouldBeTrue();
    [Fact] void should_emit_only_the_selected_use_sites_body() => _scoped.Artifacts.Count(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/PolicyBodies_", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_emit_only_the_selected_use_sites_wrapper() => _scoped.Artifacts.Count(artifact => artifact.RelativePath.StartsWith("TypedContexts/TypedContext_", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_keep_the_legacy_shared_bodies_path() => _scoped.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("GeneratedPolicies/PolicyBodies.cs");
    [Fact] void should_keep_the_legacy_shared_context_path() => _scoped.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("TypedContexts/PolicyContext.cs");
    [Fact] void should_compile_without_warnings() => _warnings.ShouldBeEmpty();
    [Fact] void should_allow_the_selected_command_in_the_ruled_year() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Caller(), opaque_policy_model.Receipt).ShouldBeTrue();
    [Fact] void should_deny_the_selected_command_outside_the_ruled_year() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Caller(), opaque_policy_model.Receipt.AddYears(1)).ShouldBeFalse();
}
#endif
