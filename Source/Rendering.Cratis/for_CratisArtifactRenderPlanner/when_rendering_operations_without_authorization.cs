// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_operations_without_authorization : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;
    string[] _sources = null!;

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(AnonymousPoliciesCorpus.V31.EsmBytes.AsSpan());
        foreach (var slice in _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices))
        {
            foreach (var command in slice.Commands) typeof(SemanticCommand).GetProperty(nameof(SemanticCommand.Authorization)).SetValue(command, null);
            foreach (var query in slice.Queries) typeof(SemanticKeyedQuery).GetProperty(nameof(SemanticKeyedQuery.Authorization)).SetValue(query, null);
        }
    }

    void Because()
    {
        _plan = invoice_model.Plan(_model);
        _sources = [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))];
    }

    [Fact] void should_admit_the_operations() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_commands() => _sources.ShouldContain(source => source.Contains("[Command]", StringComparison.Ordinal));
    [Fact] void should_render_queries() => _sources.ShouldContain(source => source.Contains("[ReadModel]", StringComparison.Ordinal));
    [Fact] void should_not_emit_anonymous_attributes() => _sources.ShouldEachConformTo(source => !source.Contains("[AllowAnonymous", StringComparison.Ordinal) && !source.Contains("[global::Cratis.Arc.Authorization.AllowAnonymous", StringComparison.Ordinal));
    [Fact] void should_not_emit_authorize_attributes() => _sources.ShouldEachConformTo(source => !source.Contains("[Authorize", StringComparison.Ordinal) && !source.Contains("[global::Cratis.Arc.Authorization.Authorize", StringComparison.Ordinal));
    [Fact] void should_not_emit_empty_attributes() => _sources.ShouldEachConformTo(source => !source.Contains("[]\n", StringComparison.Ordinal));
}
