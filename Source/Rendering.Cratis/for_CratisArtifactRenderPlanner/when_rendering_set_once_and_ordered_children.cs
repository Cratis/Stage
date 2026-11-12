// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

// Screenplay ESM v43 renders as Chronicle projection behavior instead of being refused: a set-once property keeps the first value, and a children
// collection says where a new child is placed.
public class when_rendering_set_once_and_ordered_children : Specification
{
    const string Source = """
        type Entry
          code String
        type Tag
          name String
        module Library
          feature Records
            slice StateView Records
              event RecordOpened
                owner String
              event EntryAdded
                code String
              event TagAdded
                name String
              command OpenRecord
                record String identifier
                owner String
                produces RecordOpened
                  for record
                  owner = owner
              command AddEntry
                record String identifier
                code String
                produces EntryAdded
                  for record
                  code = code
              command AddTag
                record String identifier
                name String
                produces TagAdded
                  for record
                  name = name
              readmodel Record
                id String key
                owner String
                entries Entry[]
                tags Tag[]
              projection Build => Record
                no automap
                from RecordOpened
                  set once owner = owner
                children entries identified by code
                  insert first
                  from EntryAdded key code
                    parent $eventSourceId
                    code = code
                children tags identified by name
                  sort by name descending
                  from TagAdded key name
                    parent $eventSourceId
                    name = name
        """;

    ArtifactRenderPlan _plan = null!;
    string _projection = string.Empty;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("records"), "records", "Records.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var execution = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        _plan = CratisRendering.Plan(compilation.Value.Model, execution.Plan, new(ArtifactRenderScopeKind.Application, compilation.Value.Model.Application.Id), new("Library", "Library"));
        _projection = string.Join('\n', _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).Where(text => text.Contains("IProjectionFor", StringComparison.Ordinal)));
    }

    [Fact] void should_render_the_model() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_set_the_property_once() => _projection.ShouldContain("SetOnce(model => model.Owner)");
    [Fact] void should_place_a_new_child_first() => _projection.ShouldContain("children.Prepend();");
    [Fact] void should_sort_a_collection_descending() => _projection.ShouldContain("children.OrderByDescending(item => item.Name);");
}
#endif
