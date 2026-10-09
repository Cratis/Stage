// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_rendering_scoped_projection_keys;

public class with_nested_and_join_removed_children : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _source = string.Empty;
    PropertyInfo _identifier = null!;

    void Establish()
    {
        // Use exactly the scoped projection model exercised by the live MongoDB probe, including
        // the optional LastSeen of the same concept type, nested clearing and child join removal.
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("mongo"), "mongo", "Scopes.play", every_literal_projection.Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        execution.Success.ShouldBeTrue();
        _plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        _plan.Success.ShouldBeTrue();
    }

    void Because()
    {
        var sources = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        _source = sources.Single(source => source.RelativePath == "Projects/Registration/ProjectLookup/ProjectLookup.cs").Content;
        var assembly = RenderedOutput.Load(sources);
        _identifier = assembly.GetType("Projects.Projects.Registration.ProjectLookup.ProjectSummary")!.GetProperty("ProjectId")!;
    }

    [Fact] void should_preserve_the_typed_event_source_identifier() => typeof(EventSourceId<Guid>).IsAssignableFrom(_identifier.PropertyType).ShouldBeTrue();
    [Fact] void should_not_add_a_key_attribute_to_an_event_source_identifier() => _identifier.GetCustomAttribute<KeyAttribute>().ShouldBeNull();
    [Fact] void should_use_the_identifier_for_explicit_root_keys() => _source.ShouldContain("from.UsingKey(evt => evt.ProjectId);");
    [Fact] void should_materialize_the_identifier_from_the_event_source_for_implicit_root_keys() => _source.ShouldContain("from.Set(model => model.ProjectId).ToEventSourceId();");
    [Fact] void should_use_the_child_identity_separately_from_the_parent_key() => _source.ShouldContain("children.IdentifiedBy(item => item.NoteId);");
    [Fact] void should_target_the_parent_identifier_for_children() => _source.ShouldContain("from.UsingParentKey(evt => evt.ProjectId);");
}
