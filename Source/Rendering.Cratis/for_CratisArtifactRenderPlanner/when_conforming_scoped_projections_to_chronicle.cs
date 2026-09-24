// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.for_ProjectionConverter.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Runs the shipped Chronicle definition visitor alongside a compiled generated fluent projection.
/// </summary>
public class when_conforming_scoped_projections_to_chronicle
{
    [Fact]
    public void should_preserve_from_join_and_child_definitions()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string source = when_rendering_scoped_projections.ScopedSource;
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scope-oracle"), "scope-oracle", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success);
        var plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
            .Select(_ => new RenderedFile(_.RelativePath, System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan())));
        var assembly = RenderedOutput.Load(files);
        var projection = assembly.GetTypes().Single(_ => _.Name == "ProjectSummaryProjection");
        var readModel = assembly.GetTypes().Single(_ => _.Name == "ProjectSummary");
        var events = assembly.GetTypes().Where(_ => Attribute.IsDefined(_, typeof(EventTypeAttribute))).ToArray();
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.AllClrTypes.Returns(events.ToImmutableList());
        foreach (var @event in events)
        {
            eventTypes.GetEventTypeFor(@event).Returns(@event.GetEventType());
        }

        var builderType = typeof(ProjectionBuilderFor<>).MakeGenericType(readModel);
        var builder = Activator.CreateInstance(builderType, new ProjectionId("scoped-conformance"), projection, new CamelCaseNamingPolicy(), eventTypes, new JsonSerializerOptions())!;
        projection.GetMethod("Define")!.Invoke(Activator.CreateInstance(projection), [builder]);
        var generated = builderType.GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(builder, null)!;

        var syntax = new ScreenplayCompiler().Compile(source);
        Assert.True(syntax.Success, string.Join(Environment.NewLine, syntax.Diagnostics));
        var expectedSyntax = syntax.Value!.Modules.Single().Features.Single().Slices.Single(_ => _.Name == "ProjectLookup")
            .Projections.Single(_ => _.Name == "ProjectSummaryProjection");
        var expected = ChronicleProjectionOracle.Visit(expectedSyntax);
        var expectedBlocks = ProjectionBlocks(expected);
        var generatedBlocks = ProjectionBlocks(generated);

        // The visitor leaves AutoMap to the engine; Screenplay expands its known matches in ESM. The
        // generated fluent projection disables AutoMap and emits those same explicit mappings instead.
        // Both visitor's unset key and fluent's explicit event-source key resolve to that identity.
        ExpandBoundAutoMaps(expectedBlocks, model);
        foreach (var key in expectedBlocks.Keys.Where(_ => _.EndsWith(".Key", StringComparison.Ordinal) && expectedBlocks[_].Length == 0).ToArray())
        {
            expectedBlocks[key] = "$eventSourceId";
        }

        Assert.True(expectedBlocks.SequenceEqual(generatedBlocks),
            $"Expected: {string.Join("; ", expectedBlocks.Select(_ => $"{_.Key}={_.Value}"))}\nGenerated: {string.Join("; ", generatedBlocks.Select(_ => $"{_.Key}={_.Value}"))}");
    }

    static void ExpandBoundAutoMaps(IDictionary<string, string> shape, ExecutableSemanticModel model)
    {
        var application = model.Application;
        var slices = application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).ToArray();
        var events = slices.SelectMany(_ => _.Events).ToDictionary(_ => _.Id);
        var readModel = slices.SelectMany(_ => _.ReadModels).Single(_ => _.Name == "ProjectSummary");
        var scope = slices.SelectMany(_ => _.Projections).Single(_ => _.Name == "ProjectSummaryProjection").Scope!;
        Expand(scope, readModel.Properties, string.Empty);

        void Expand(SemanticProjectionScope level, IReadOnlyList<SemanticProperty> properties, string prefix)
        {
            foreach (var transition in level.From)
            {
                var @event = events[transition.EventContract];
                var eventProperties = @event.Properties.ToDictionary(_ => _.Id);
                foreach (var mapping in transition.Mappings.Where(_ => _.Operation == SemanticProjectionOperation.Set && _.Target.Length == 1 &&
                    _.Source is SemanticProjectionEventProperty { Path.Length: 1 }))
                {
                    var target = properties.Single(_ => _.Id == mapping.Target[0]).Name;
                    shape[$"{prefix}From.{@event.Name}+1.{target}"] = eventProperties[((SemanticProjectionEventProperty)mapping.Source!).Path[0]].Name;
                }
            }

            foreach (var nested in level.Nested)
            {
                var property = properties.Single(_ => _.Id == nested.Property);
                var type = application.Types.Single(_ => _.Id == property.Type.Target);
                Expand(nested.Scope, type.Properties, $"{prefix}Nested.{property.Name}.");
            }

            foreach (var child in level.Children)
            {
                var property = properties.Single(_ => _.Id == child.Property);
                var type = application.Types.Single(_ => _.Id == property.Type.Target);
                Expand(child.Scope, type.Properties, $"{prefix}Children.{property.Name}.");
            }
        }
    }

    static SortedDictionary<string, string> ProjectionBlocks(object projection)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        ReadScope(projection, "", result);
        return result;
    }

    static void ReadScope(object scope, string prefix, IDictionary<string, string> shape)
    {
        foreach (var kind in new[] { "From", "Join" })
        {
            foreach (DictionaryEntry entry in (IDictionary)Property(scope, kind)!)
            {
                var id = entry.Key.GetType().GetProperty("Id")?.GetValue(entry.Key);
                var generation = entry.Key.GetType().GetProperty("Generation")?.GetValue(entry.Key);
                var eventName = id is string ? $"{id}+{generation}" : entry.Key.ToString()!;
                var definition = entry.Value!;
                var path = $"{prefix}{kind}.{eventName}";
                shape[$"{path}.Key"] = Property(definition, "Key")?.ToString() ?? string.Empty;
                if (kind == "From")
                {
                    shape[$"{path}.ParentKey"] = Property(definition, "ParentKey")?.ToString() ?? string.Empty;
                }
                else
                {
                    shape[$"{path}.On"] = Property(definition, "On")!.ToString()!;
                }

                foreach (DictionaryEntry mapping in (IDictionary)Property(definition, "Properties")!)
                {
                    shape[$"{path}.{mapping.Key}"] = mapping.Value!.ToString()!;
                }
            }
        }

        var every = Property(scope, "All") ?? Property(scope, "FromEvery");
        shape[$"{prefix}Every.IncludeChildren"] = Property(every!, "IncludeChildren")!.ToString()!;
        foreach (DictionaryEntry mapping in (IDictionary)Property(every!, "Properties")!)
        {
            shape[$"{prefix}Every.{mapping.Key}"] = mapping.Value!.ToString()!;
        }

        foreach (var kind in new[] { "RemovedWith", "RemovedWithJoin" })
        {
            foreach (DictionaryEntry entry in (IDictionary)Property(scope, kind)!)
            {
                var id = entry.Key.GetType().GetProperty("Id")?.GetValue(entry.Key);
                var generation = entry.Key.GetType().GetProperty("Generation")?.GetValue(entry.Key);
                var eventName = id is string ? $"{id}+{generation}" : entry.Key.ToString()!;
                var path = $"{prefix}{kind}.{eventName}";
                shape[$"{path}.Key"] = Property(entry.Value!, "Key")?.ToString() ?? string.Empty;
                if (kind == "RemovedWith")
                {
                    shape[$"{path}.ParentKey"] = Property(entry.Value!, "ParentKey")?.ToString() ?? string.Empty;
                }
            }
        }

        foreach (var kind in new[] { "Children", "Nested" })
        {
            if (Property(scope, kind) is not IDictionary definitions)
            {
                continue;
            }

            foreach (DictionaryEntry entry in definitions)
            {
                var path = $"{prefix}{kind}.{entry.Key}.";
                shape[$"{path}IdentifiedBy"] = Property(entry.Value!, "IdentifiedBy")!.ToString()!;
                ReadScope(entry.Value!, path, shape);
            }
        }
    }

    static object? Property(object value, string name) => value.GetType().GetProperty(name)?.GetValue(value);
}
