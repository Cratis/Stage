// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class optional_validated_composite_concept_cases
{
    internal static ExecutableSemanticModel Compile()
    {
        const string declarations = """
            concept Count : Int
              validate
                > 5 severity error message "Count minimum"
                < 10 severity warning message "Count maximum"
                != 2 severity information message "Count excluded"
            concept Amount : Decimal
              validate
                > 5 severity warning message "Amount minimum"
                < 10 severity error message "Amount maximum"
                != 2 severity information message "Amount excluded"
            concept Label : String
              validate
                length == 3 severity error message "Label length"
                matches "^X" severity warning message "Label pattern"
                != "bad" severity information message "Label excluded"
            type Details
              count Count optional
              amount Amount optional
              label Label optional
            type Node
              count Count optional
              next Node optional
            module Inputs
              feature Optional
            """;
        const string valid = "{\"count\":7,\"amount\":7.5,\"label\":\"Xxx\"}";
        const string invalid = "{\"count\":2,\"amount\":2,\"label\":\"bad\"}";
        var shapes = new[]
        {
            (Name: "RequiredDetails", Type: "Details", Valid: valid, Invalid: invalid, Absent: "{}"),
            (Name: "OptionalDetails", Type: "Details optional", Valid: valid, Invalid: invalid, Absent: "{}"),
            (Name: "Collection", Type: "Details[] optional", Valid: $"[{valid},{valid}]", Invalid: $"[{invalid},{invalid}]", Absent: "[{},{}]"),
            (Name: "Recursive", Type: "Node optional", Valid: "{\"count\":7,\"next\":{\"count\":7}}", Invalid: "{\"count\":2,\"next\":{\"count\":2}}", Absent: "{\"next\":{}}")
        };
        var slices = shapes.Select(shape =>
        {
            var cases = new List<(string Name, string Value)>
            {
                ("NestedAbsent", shape.Absent), ("Valid", shape.Valid), ("Invalid", shape.Invalid)
            };
            if (shape.Type.Contains("optional", StringComparison.Ordinal)) cases.Add(("ParentAbsent", shape.Absent));
            if (shape.Name == "Collection") cases.Add(("Empty", "[]"));
            if (string.Equals(shape.Name, "RequiredDetails", StringComparison.Ordinal) || string.Equals(shape.Name, "OptionalDetails", StringComparison.Ordinal)) cases.Add(("NestedNull", "{}"));
            return $$"""
                    slice StateChange {{shape.Name}}Input
                      command Check{{shape.Name}}
                        id String
                        value {{shape.Type}}
                        returns id
                """ + "\n" + string.Join('\n', cases.Select(vector => $$"""
                      specification {{shape.Name}}{{vector.Name}}
                        when Check{{shape.Name}}
                          id = "one"
                          value = {{vector.Value}}
                        {{(vector.Name == "Invalid" ? "then error \"Count minimum\"" : "then returns \"one\"")}}
                """));
        });
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("OptionalComposites"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("optional-composites"), "optional-composites", "OptionalComposites.play", declarations + "\n" + string.Join('\n', slices));
        var compilation = new SemanticModelCompiler().Compile("OptionalComposites", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;

        // Authored command fixtures reject null literals. Exercise the optional typed ESM contract too.
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification with
                        {
                            When = specification.When! with
                            {
                                Values = [.. specification.When.Values.Select(value => slice.Commands.Single().Properties.Single(property => property.Id == value.TargetProperty).Name == "value"
                                    ? value with { Value = OptionalValue(model, specification.Name, value.Value) }
                                    : value)]
                            }
                        })]
                    })]
                })]
            })]
        });
    }

    static SemanticValue OptionalValue(ExecutableSemanticModel model, string name, SemanticValue value)
    {
        if (name.EndsWith("ParentAbsent", StringComparison.Ordinal)) return SemanticValue.Null;
        if (name.EndsWith("NestedNull", StringComparison.Ordinal))
        {
            return SemanticValue.Composite([.. model.Application.Types.Single(type => type.Name == "Details").Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Null))]);
        }

        return value;
    }
}
#endif
