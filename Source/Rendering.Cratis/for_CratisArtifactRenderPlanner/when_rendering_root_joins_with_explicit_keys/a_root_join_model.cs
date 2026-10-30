// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_root_joins_with_explicit_keys;

static class a_root_join_model
{
    public const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        type ProjectNote
          noteId ProjectId
          name ProjectName
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
              event ProjectNamed
                correlationId ProjectId
                name ProjectName
              event ProjectNoted
                noteId ProjectId
                projectId ProjectId
                name ProjectName
              event ProjectNoteNamed
                name ProjectName
            slice StateView ProjectLookup
              readmodel ProjectSummary
                id ProjectId key
                projectId ProjectId
                name ProjectName
                notes ProjectNote[]
              projection ProjectSummaryProjection => ProjectSummary
                no automap
                from ProjectRegistered
                  projectId = projectId
                  name = name
                join project on projectId
                  with ProjectNamed
                    name = name
                children notes identified by noteId
                  from ProjectNoted key noteId
                    parent projectId
                    name = name
                  join note on noteId
                    with ProjectNoteNamed
                      name = name
        """;

    public static ExecutableSemanticModel Create(Func<SemanticProjectionKey, SemanticProjectionKey>? change = null, bool child = false)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("root-join"), "root-join", "Join.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var @event = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Events).Single(@event => @event.Name == "ProjectNamed");
        var key = new SemanticProjectionValueKey(new SemanticProjectionEventProperty([@event.Properties.Single(property => property.Name == "correlationId").Id]));
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Projections = [.. slice.Projections.Select(projection => projection.Scope is { Joins.Length: > 0 } scope
                            ? projection with { Scope = child
                                ? scope with { Children = [.. scope.Children.Select(collection => collection with { Scope = collection.Scope with { Joins = [.. collection.Scope.Joins.Select(join => join with { Key = SemanticProjectionKey.EventSourceIdentity })] } })] }
                                : scope with { Joins = [.. scope.Joins.Select(join => join with { Key = change?.Invoke(key) ?? key })] } }
                            : projection)]
                    })]
                })]
            })]
        };
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
    }
}
