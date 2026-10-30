// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_checking_root_join_property_keys : Specification
{
    const string SourceTemplate = """
        module Work
          feature Work
            slice StateChange Recording
              command Open
                sourceId String identifier
                value {{primitive}}
                label String
                produces Opened
                  for sourceId
                  value = value
                  label = label
              command Rename
                sourceId String identifier
                correlationId {{primitive}}
                label String
                produces Renamed
                  for sourceId
                  correlationId = correlationId
                  label = label
              event Opened
                value {{primitive}}
                label String
              event Renamed
                correlationId {{primitive}}
                label String
              specification RootFirst
                given Opened
                  for "root"
                  value = {{literal}}
                  label = "before"
                when append Renamed
                  for "{{key}}"
                  correlationId = {{literal}}
                  label = "after"
                then readmodel Root
                  id = "root"
                  label = "after"
            slice StateView Listing
              readmodel Root
                id String key
                foreignKey {{primitive}}
                label String
              projection Build => Root
                no automap
                from Opened
                  foreignKey = value
                  label = label
                join label on foreignKey
                  with Renamed
                    label = label
        """;

    [Theory]
    [InlineData("String", "plain")]
    [InlineData("Uuid", "plain")]
    [InlineData("String", "implicit")]
    public void should_admit_compatible_root_keys(string primitive, string shape)
    {
        var plan = Compile(primitive, shape);
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
    }

    [Theory]
    [InlineData("String", "literal")]
    [InlineData("String", "source")]
    [InlineData("Int", "plain")]
    public void should_refuse_keys_outside_the_scalar_property_contract(string primitive, string shape)
    {
        var plan = Compile(primitive, shape);
        Assert.NotNull(SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()));
    }

    [Theory]
    [InlineData("optional")]
    [InlineData("collection")]
    [InlineData("mismatch")]
    [InlineData("composite")]
    public void should_reject_incompatible_property_types_at_the_semantic_boundary(string shape) =>
        Assert.Throws<InvalidSemanticContract>(() => Compile("String", shape));

    static SemanticExecutionPlan Compile(string primitive, string shape)
    {
        var key = primitive == "Uuid" ? "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01" : "one";
        var literal = primitive == "Int" ? "1" : $"\"{key}\"";
        var source = SourceTemplate.Replace("{{primitive}}", primitive, StringComparison.Ordinal)
            .Replace("{{literal}}", literal, StringComparison.Ordinal).Replace("{{key}}", key, StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RootJoin"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Join.play", source);
        var compilation = new SemanticModelCompiler().Compile("RootJoin", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var slices = model.Application.Modules.Single().Features.Single().Slices;
        var property = slices.SelectMany(slice => slice.Events).Single(@event => @event.Name == "Renamed").Properties.Single(property => property.Name == "correlationId");
        var joinKey = KeyFor(property, shape, key);
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Events = [.. slice.Events.Select(@event => @event with { Properties = [.. @event.Properties.Select(item => item.Id == property.Id ? ChangeType(item, shape) : item)] })],
                        Projections = [.. slice.Projections.Select(projection => projection.Scope is { Joins.Length: > 0 } scope
                            ? projection with { Scope = scope with { Joins = [.. scope.Joins.Select(join => join with { Key = joinKey })] } }
                            : projection)]
                    })]
                })]
            })]
        };
        var execution = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
        Assert.NotNull(execution.Plan);
        return execution.Plan;
    }

    static SemanticProjectionKey? KeyFor(SemanticProperty property, string shape, string key) => shape switch
    {
        "implicit" => null,
        "context" => new SemanticProjectionValueKey(new SemanticProjectionEventContextValue("correlationId")),
        "source" => new SemanticProjectionValueKey(SemanticProjectionValue.EventSourceIdentity),
        "literal" => new SemanticProjectionValueKey(SemanticProjectionValue.Literal(SemanticValue.Text(key))),
        "composite" => new SemanticProjectionCompositeKey(property.Id, [new(property.Id, new SemanticProjectionEventProperty([property.Id]))]),
        _ => new SemanticProjectionValueKey(new SemanticProjectionEventProperty([property.Id]))
    };

    static SemanticProperty ChangeType(SemanticProperty property, string shape) => property with
    {
        Type = shape switch
        {
            "optional" => property.Type with { IsOptional = true },
            "collection" => property.Type with { IsCollection = true },
            "mismatch" => property.Type with { Primitive = SemanticPrimitiveType.WholeNumber },
            _ => property.Type
        }
    };
}
